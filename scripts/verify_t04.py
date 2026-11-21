"""Reproducible T04 gate: locked restore, full builds, regression and NuGet audit.

Uses the same entry point locally and on CI. Never connects to a database.
"""
import argparse
import datetime
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
import uuid
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
SOLUTION = 'db/src/HRM.sln'
TESTS = 'backend/tests/HRM.Tests.Regression/HRM.Tests.Regression.csproj'
NS = {'t': 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}


def inspect_packages(report, require_resolved=False):
    """Fail closed on missing frameworks, error logs or any vulnerable package."""
    if report.get('logs') and any(x.get('level', '').lower() == 'error' for x in report['logs']):
        raise ValueError('NuGet report contains errors')
    projects = report.get('projects', [])
    if not projects:
        raise ValueError('NuGet report has no projects')
    vulnerabilities = []
    for project in projects:
        frameworks = project.get('frameworks', [])
        if not frameworks:
            raise ValueError(f'Missing framework report: {project.get("path")}')
        for framework in frameworks:
            if not framework.get('framework', '').startswith('net10.0'):
                raise ValueError(f'Unexpected target framework: {framework.get("framework")}')
            if require_resolved and not framework.get('topLevelPackages') and not framework.get('transitivePackages'):
                raise ValueError('Resolved package report is empty')
            for kind in ('topLevelPackages', 'transitivePackages'):
                for package in framework.get(kind, []):
                    if not package.get('id') or not package.get('resolvedVersion'):
                        raise ValueError('Package identity or resolved version missing')
                    for advisory in package.get('vulnerabilities', []):
                        vulnerabilities.append({'project': project['path'], 'package': package['id'],
                                                'version': package.get('resolvedVersion'), **advisory})
    if vulnerabilities:
        raise ValueError('Vulnerabilities found: ' + json.dumps(vulnerabilities))
    return len(projects)


def snapshot_inputs():
    paths = [ROOT / name for name in ('global.json', 'Directory.Build.props',
             'Directory.Packages.props', 'NuGet.Config', '.config/dotnet-tools.json',
             'docs/verification/baseline-allowed-failures.json', 'scripts/verify_t04.py',
             'scripts/run_and_verify_baseline.py', 'scripts/verify_baseline_trx.py',
             '.github/workflows/ci.yml', 'tools/Install-Dotnet.ps1', 'tools/dotnet.cmd')]
    paths.extend((ROOT / 'scripts/tests').glob('test_*.py'))
    for folder in ('db/src', 'backend/tests'):
        paths.extend(p for p in (ROOT / folder).rglob('*') if p.is_file()
                     and not any(x in ('bin', 'obj') for x in p.parts)
                     and (p.suffix in ('.cs', '.csproj') or p.name == 'packages.lock.json'))
    return {p.relative_to(ROOT).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest()
            for p in sorted(paths)}


def warning_owner(path):
    if any(x in path for x in ('UngVien', 'PhongVan', 'TuyenDung')):
        return 'P01'
    if any(x in path for x in ('/Auth/', 'DoiMatKhau')):
        return 'T06'
    if 'ChamCong' in path:
        return 'T10'
    if any(x in path for x in ('BangLuong', 'HieuSuat')):
        return 'T13'
    if 'Chat' in path or 'Gemini' in path:
        return 'P07'
    return 'T08'


def collect_warnings(run_dir):
    warnings = {}
    pattern = r'^(.*?)\((\d+),(\d+)\): warning (\w+): (.*?) \[.*\]$'
    for log in run_dir.glob('build-*.stdout.log'):
        for line in log.read_text(encoding='utf-8').splitlines():
            match = re.match(pattern, line)
            if not match:
                continue
            path, row, col, code, message = match.groups()
            path = Path(path).relative_to(ROOT).as_posix()
            key = (path, row, col, code, message)
            warnings[key] = {'file': path, 'line': int(row), 'column': int(col),
                             'code': code, 'message': message, 'owner_task': warning_owner(path)}
    result = sorted(warnings.values(), key=lambda x: (x['file'], x['line'], x['column']))
    (run_dir / 'warning-register.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--dotnet', help='Exact SDK executable; default uses PATH/global.json')
    parser.add_argument('--output', help='New evidence directory, must not already exist')
    args = parser.parse_args()
    stamp = datetime.datetime.now(datetime.timezone.utc).strftime('%Y%m%dT%H%M%SZ')
    run_dir = Path(args.output).resolve() if args.output else ROOT / 'docs/verification/runs' / f'T04-{stamp}-{uuid.uuid4().hex[:8]}'
    run_dir.mkdir(parents=True, exist_ok=False)
    evidence = {'status': 'RUNNING', 'steps': [], 'run_dir': str(run_dir)}
    env = os.environ.copy()
    dotnet = str(Path(args.dotnet).resolve()) if args.dotnet else shutil.which('dotnet')
    if not dotnet:
        raise SystemExit('dotnet not found. Run tools/Install-Dotnet.ps1 first.')
    env['PATH'] = str(Path(dotnet).parent) + os.pathsep + env.get('PATH', '')
    env['DOTNET_ROOT'] = str(Path(dotnet).parent)
    env['DOTNET_CLI_TELEMETRY_OPTOUT'] = '1'

    def save():
        (run_dir / 'result.json').write_text(json.dumps(evidence, ensure_ascii=False, indent=2), encoding='utf-8')

    def run(name, command, json_output=False):
        print(f'Running {name}', flush=True)
        step = {'name': name, 'command': command}
        evidence['steps'].append(step)
        save()
        result = subprocess.run(command, cwd=ROOT, env=env, capture_output=True,
                                text=True, encoding='utf-8', errors='replace', timeout=600)
        (run_dir / f'{name}.stdout.log').write_text(result.stdout, encoding='utf-8')
        (run_dir / f'{name}.stderr.log').write_text(result.stderr, encoding='utf-8')
        step['exit_code'] = result.returncode
        save()
        if result.returncode:
            raise RuntimeError(f'{name} exited {result.returncode}; see {run_dir}')
        if json_output:
            data = json.loads(result.stdout)
            (run_dir / f'{name}.json').write_text(json.dumps(data, indent=2), encoding='utf-8')
            return data
        return result.stdout

    try:
        evidence['input_sha256'] = snapshot_inputs()
        version = run('sdk-version', [dotnet, '--version']).strip()
        required = json.loads((ROOT / 'global.json').read_text())['sdk']['version']
        if version != required:
            raise ValueError(f'Expected SDK {required}, got {version}')
        evidence['sdk_version'] = version
        run('restore-solution-locked', [dotnet, 'restore', SOLUTION, '--locked-mode'])
        run('restore-tests-locked', [dotnet, 'restore', TESTS, '--locked-mode'])
        run('restore-ef-tool', [dotnet, 'tool', 'restore'])
        run('ef-version', [dotnet, 'ef', '--version'])
        run('build-solution', [dotnet, 'build', SOLUTION, '--no-restore', '-c', 'Release', '--no-incremental'])
        run('build-tests', [dotnet, 'build', TESTS, '--no-restore', '-c', 'Release', '--no-incremental'])
        evidence['warnings'] = {'count': len(collect_warnings(run_dir)), 'register': 'warning-register.json'}
        for name, project, expected in (('solution', SOLUTION, 5), ('tests', TESTS, 1)):
            resolved = run(f'{name}-resolved', [dotnet, 'list', project, 'package', '--no-restore',
                           '--include-transitive', '--format', 'json'], True)
            if inspect_packages(resolved, require_resolved=True) != expected:
                raise ValueError(f'Unexpected project count in {name} dependency report')
            audit = run(f'{name}-vulnerable', [dotnet, 'list', project, 'package', '--no-restore',
                        '--include-transitive', '--vulnerable', '--format', 'json'], True)
            inspect_packages(audit)
        for name, category, minimum in (('characterization', 'Characterization', 8),
                                        ('runtime-compatibility', 'RuntimeCompatibility', 3)):
            trx_dir = run_dir / name
            run(name, [dotnet, 'test', TESTS, '--no-build', '--no-restore', '-c', 'Release',
                '--filter', f'Category={category}', '--logger', 'trx', '--results-directory', str(trx_dir)])
            trx = list(trx_dir.glob('*.trx'))
            if len(trx) != 1:
                raise ValueError(f'Expected one {name} TRX')
            counters = ET.parse(trx[0]).find('.//t:Counters', NS).attrib
            if int(counters['passed']) < minimum or counters['total'] != counters['passed']:
                raise ValueError(f'Incomplete {name} run: {counters}')
            evidence[name] = counters
        run('baseline', [sys.executable, '-B', 'scripts/run_and_verify_baseline.py',
            '--configuration', 'Release', '--results-dir', str(run_dir / 'baseline')])
        run('python-verifiers', [sys.executable, '-B', '-m', 'unittest', 'discover', '-s', 'scripts/tests', '-v'])
        if snapshot_inputs() != evidence['input_sha256']:
            raise ValueError('Source/package inputs changed during verification; re-run against final inputs')
        evidence['status'] = 'PASS'
        evidence['limits'] = ['8 baseline regression failures are known legacy defects, not fixed by T04',
                              'No SQL Server migration, UI UAT or deployed API acceptance performed']
    except Exception as exc:
        evidence['status'] = 'FAIL'
        evidence['error'] = str(exc)
        print(str(exc), file=sys.stderr)
    finally:
        save()
    print(f'{evidence["status"]}: {run_dir}', flush=True)
    return 0 if evidence['status'] == 'PASS' else 1


if __name__ == '__main__':
    raise SystemExit(main())
