"""Full T05 gate. Requires an explicitly configured SQL Server; never substitutes InMemory."""
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

from verify_t04 import inspect_packages, snapshot_inputs as legacy_inputs

ROOT = Path(__file__).resolve().parents[1]
SOLUTION = 'backend/HRM.Next.slnx'
TESTS = 'backend/tests/HRM.Tests.Integration/HRM.Tests.Integration.csproj'
NS = {'t': 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}


def inputs():
    result = legacy_inputs()
    paths = [ROOT / 'scripts/verify_t05.py', ROOT / 'scripts/contracts.py', ROOT / SOLUTION]
    if (ROOT / 'scripts/verify_t06.py').exists():
        paths.append(ROOT / 'scripts/verify_t06.py')
    if (ROOT / 'scripts/verify_t07.py').exists():
        paths.append(ROOT / 'scripts/verify_t07.py')
    if (ROOT / 'scripts/verify_t08.py').exists():
        paths.append(ROOT / 'scripts/verify_t08.py')
    if (ROOT / 'scripts/verify_t09.py').exists():
        paths.append(ROOT / 'scripts/verify_t09.py')
    if (ROOT / 'scripts/verify_t10.py').exists():
        paths.append(ROOT / 'scripts/verify_t10.py')
    if (ROOT / 'scripts/verify_t11.py').exists():
        paths.append(ROOT / 'scripts/verify_t11.py')
    if (ROOT / 'scripts/verify_t12.py').exists():
        paths.append(ROOT / 'scripts/verify_t12.py')
    if (ROOT / 'scripts/verify_t13.py').exists():
        paths.append(ROOT / 'scripts/verify_t13.py')
    if (ROOT / 'scripts/verify_t14.py').exists():
        paths.append(ROOT / 'scripts/verify_t14.py')
    if (ROOT / 'scripts/verify_t15.py').exists():
        paths.append(ROOT / 'scripts/verify_t15.py')
    if (ROOT / 'scripts/verify_t16.py').exists():
        paths.append(ROOT / 'scripts/verify_t16.py')
    paths.extend(ROOT / name for name in ('docs/business-rules.md', 'docs/fixtures/work-calendars.demo.json', 'docs/fixtures/payroll-golden.demo.json', 'docs/decisions/T13-calculation.md', 'docs/decisions/T14-workflow.md', 'docs/decisions/T15-payslips.md', 'docs/decisions/T16-web.md', 'docs/verification/invariants.md', 'docs/decisions/T12-invariants.md') if (ROOT / name).exists())
    paths.extend(p for p in (ROOT / 'contracts').glob('*.json') if p.is_file())
    for folder in ('backend/src', 'contracts/src', 'contracts/scripts', 'contracts/tests', 'contracts/type-tests'):
        paths.extend(p for p in (ROOT / folder).rglob('*') if p.is_file()
                     and not any(part in ('bin', 'obj', 'node_modules', 'dist') for part in p.parts)
                     and (p.suffix in ('.cs', '.csproj', '.ts', '.mjs') or p.name == 'packages.lock.json'))
    for p in sorted(set(paths)):
        result[p.relative_to(ROOT).as_posix()] = hashlib.sha256(p.read_bytes()).hexdigest()
    return dict(sorted(result.items()))


def main(task='T05'):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--dotnet', default=shutil.which('dotnet'))
    parser.add_argument('--output', help='A new evidence directory')
    parser.add_argument('--ci-backend-only', action='store_true',
                        help='Linux CI backend job only; Windows legacy job must pass separately')
    args = parser.parse_args()
    if not args.dotnet:
        parser.error('Supply --dotnet .dotnet/dotnet.exe or install the pinned SDK on PATH')
    if not os.environ.get('HRM_T05_SQLSERVER', '').strip():
        parser.error('Set HRM_T05_SQLSERVER explicitly. A disposable GUID database will be created and cleaned up.')
    npm = shutil.which('npm.cmd' if os.name == 'nt' else 'npm')
    if not npm:
        parser.error('Node.js 24/npm is required')
    dotnet = str(Path(args.dotnet).resolve())
    env = os.environ.copy()
    env['DOTNET_ROOT'] = str(Path(dotnet).parent)
    env['PATH'] = str(Path(dotnet).parent) + os.pathsep + env.get('PATH', '')
    env['DOTNET_CLI_TELEMETRY_OPTOUT'] = '1'
    stamp = datetime.datetime.now(datetime.timezone.utc).strftime('%Y%m%dT%H%M%SZ')
    run_dir = Path(args.output).resolve() if args.output else ROOT / 'docs/verification/runs' / f'{task}-{stamp}-{uuid.uuid4().hex[:8]}'
    run_dir.mkdir(parents=True, exist_ok=False)
    env['HRM_T05_SQL_LEDGER'] = str(run_dir / 'sql-evidence.json')
    evidence = {'status': 'RUNNING', 'task': task, 'steps': [], 'run_dir': str(run_dir), 'sql_server_explicitly_configured': True,
                'scope': 'backend-only; separate Windows legacy gate required' if args.ci_backend_only else ('backend/contract/legacy only; web/browser reviewed separately' if task == 'T16' else f'full {task} and legacy')}

    def save():
        (run_dir / 'result.json').write_text(json.dumps(evidence, indent=2, ensure_ascii=False), encoding='utf-8')

    def redact(text):
        text = text.replace(env['HRM_T05_SQLSERVER'], '[REDACTED SQL CONNECTION]')
        for match in re.finditer(r'(?i)(?:password|pwd)\s*=\s*([^;]+)', env['HRM_T05_SQLSERVER']):
            secret = match.group(1).strip(' "\'')
            if secret:
                text = text.replace(secret, '[REDACTED PASSWORD]')
        return text

    def run(name, command, cwd=ROOT, as_json=False):
        print(f'Running {name}', flush=True)
        step = {'name': name, 'command': command}
        evidence['steps'].append(step)
        save()
        completed = subprocess.run(command, cwd=cwd, env=env, capture_output=True,
                                   text=True, encoding='utf-8', errors='replace', timeout=900)
        stdout, stderr = redact(completed.stdout), redact(completed.stderr)
        (run_dir / f'{name}.stdout.log').write_text(stdout, encoding='utf-8')
        (run_dir / f'{name}.stderr.log').write_text(stderr, encoding='utf-8')
        step['exit_code'] = completed.returncode
        save()
        if completed.returncode:
            raise RuntimeError(f'{name} exited {completed.returncode}; see {run_dir}')
        if as_json:
            data = json.loads(stdout)
            (run_dir / f'{name}.json').write_text(json.dumps(data, indent=2), encoding='utf-8')
            return data
        return stdout

    try:
        evidence['input_sha256'] = inputs()
        version = run('sdk', [dotnet, '--version']).strip()
        if version != json.loads((ROOT / 'global.json').read_text())['sdk']['version']:
            raise ValueError('SDK differs from global.json')
        evidence['sdk_version'] = version
        run('restore', [dotnet, 'restore', SOLUTION, '--locked-mode', '--force'])
        build = run('build', [dotnet, 'build', SOLUTION, '-c', 'Release', '--no-restore', '--no-incremental'])
        if '0 Warning(s)' not in build or '0 Error(s)' not in build:
            raise ValueError('Backend full build must have zero warnings/errors')
        resolved = run('resolved', [dotnet, 'list', SOLUTION, 'package', '--include-transitive', '--no-restore', '--format', 'json'], as_json=True)
        if inspect_packages(resolved, require_resolved=False) != 5:
            raise ValueError('Expected four product projects and one integration suite')
        audit = run('vulnerable', [dotnet, 'list', SOLUTION, 'package', '--include-transitive', '--vulnerable', '--no-restore', '--format', 'json'], as_json=True)
        inspect_packages(audit)
        run('sql-integration', [dotnet, 'test', TESTS, '-c', 'Release', '--no-build', '--no-restore',
            '--logger', 'trx;LogFileName=integration.trx', '--results-directory', str(run_dir / 'integration')])
        counters = ET.parse(run_dir / 'integration/integration.trx').find('.//t:Counters', NS).attrib
        min_passed = 388 if task == 'T16' else 383 if task == 'T15' else 347 if task == 'T14' else 312 if task == 'T13' else 255 if task == 'T12' else 239 if task == 'T11' else 204 if task == 'T10' else 174 if task == 'T09' else 147 if task == 'T08' else (67 if task == 'T07' else (57 if task == 'T06' else 34))
        if int(counters['passed']) < min_passed or counters['total'] != counters['passed'] or counters['executed'] != counters['total']:
            raise ValueError(f'Incomplete integration run: {counters}')
        evidence['integration'] = counters
        if task in ('T06', 'T07', 'T08', 'T09', 'T10', 'T11', 'T12', 'T13', 'T14', 'T15', 'T16'):
            from verify_t06 import verify_auth_matrix
            evidence['auth_matrix'] = verify_auth_matrix(run_dir / 'integration/integration.trx')
        if task in ('T07', 'T08', 'T09', 'T10', 'T11', 'T12', 'T13', 'T14', 'T15', 'T16'):
            from verify_t07 import verify_authorization_matrix
            evidence['authorization_matrix'] = verify_authorization_matrix(run_dir / 'integration/integration.trx')
        if task in ('T08', 'T09', 'T10', 'T11', 'T12', 'T13', 'T14', 'T15', 'T16'):
            from verify_t08 import verify_t08_matrix
            evidence['t08_matrix'] = verify_t08_matrix(run_dir / 'integration/integration.trx')
        if task in ('T09', 'T10', 'T11', 'T12', 'T13', 'T14', 'T15', 'T16'):
            from verify_t09 import verify_calendar_matrix
            evidence['calendar_matrix'] = verify_calendar_matrix(run_dir / 'integration/integration.trx')
        if task in ('T10', 'T11', 'T12', 'T13', 'T14', 'T15', 'T16'):
            from verify_t10 import verify_attendance_matrix
            evidence['attendance_matrix'] = verify_attendance_matrix(run_dir / 'integration/integration.trx')
        if task in ('T11', 'T12', 'T13', 'T14', 'T15', 'T16'):
            from verify_t11 import verify_leave_matrix
            evidence['leave_matrix'] = verify_leave_matrix(run_dir / 'integration/integration.trx')
        if task in ('T12', 'T13', 'T14', 'T15', 'T16'):
            from verify_t12 import verify_invariant_matrix
            evidence['invariant_matrix'] = verify_invariant_matrix(run_dir / 'integration/integration.trx')
        if task in ('T13', 'T14', 'T15', 'T16'):
            from verify_t13 import verify_payroll_matrix
            evidence['payroll_matrix'] = verify_payroll_matrix(run_dir / 'integration/integration.trx')
        if task in ('T14', 'T15', 'T16'):
            from verify_t14 import verify_workflow_matrix
            evidence['workflow_matrix'] = verify_workflow_matrix(run_dir / 'integration/integration.trx')
        if task in ('T15', 'T16'):
            from verify_t15 import verify_payslip_matrix
            evidence['payslip_matrix'] = verify_payslip_matrix(run_dir / 'integration/integration.trx')
        if task == 'T16':
            from verify_t16 import verify_access_matrix
            evidence['access_matrix'] = verify_access_matrix(run_dir / 'integration/integration.trx')
        sql = json.loads((run_dir / 'sql-evidence.json').read_text())
        if (not sql.get('createdByFixture') or not sql.get('cleanupCompleted') or not sql.get('appliedMigrations')
            or not re.fullmatch(r'HRM_Next_Test_T05_[a-f0-9]{32}', sql.get('databaseName', ''))
            or not sql.get('serverVersion')):
            raise ValueError('Missing real SQL Server/owned database/cleanup evidence')
        evidence['sql'] = sql
        run('npm-ci', [npm, 'ci', '--ignore-scripts', '--no-fund', '--no-audit'], cwd=ROOT / 'contracts')
        npm_audit = run('npm-audit', [npm, 'audit', '--json'], cwd=ROOT / 'contracts', as_json=True)
        if npm_audit.get('metadata', {}).get('vulnerabilities', {}).get('total') != 0:
            raise ValueError('Missing clean npm audit evidence')
        run('contract-drift-and-client', [sys.executable, '-B', 'scripts/contracts.py', 'check', '--dotnet', dotnet,
            '--evidence', str(run_dir / 'contracts')])
        if args.ci_backend_only:
            run('python-verifiers', [sys.executable, '-B', '-m', 'unittest', 'discover', '-s', 'scripts/tests', '-v'])
        else:
            run('legacy-gate', [sys.executable, '-B', 'scripts/verify_t04.py', '--dotnet', dotnet,
                '--output', str(run_dir / 'legacy')])
        if inputs() != evidence['input_sha256']:
            raise ValueError('Inputs changed during verification; run again against final source')
        evidence['status'] = 'PASS_BACKEND_ONLY' if args.ci_backend_only else 'PASS'
        limits_msg = {
            'T16': 'This is backend/contract/legacy evidence only: server permissions endpoint and prior matrices; web unit/build/audit/browser acceptance recorded separately; no remote CI/UAT',
            'T15': 'Snapshot payslip details/reconciliation and bounded UTF-8 formula-safe CSV implemented; no PDF, bank transfer, real email, remote CI or human UAT',
            'T14': 'Versioned payroll workflow, durable command receipts and lock implemented; legacy metadata-only compatibility retained; no statutory tax/insurance, UI, remote CI or production UAT',
            'T13': 'Demo salary history, pure payroll calculator and immutable snapshots implemented; no statutory PIT/insurance, UI or complete lifecycle T14; metadata-only compatibility exists without salary sources',
            'T12': 'Cross-module SQL invariants and shared employee guards verified; no UI, payroll formulas T13/T14, production UAT or remote CI execution',
            'T11': 'Leave transaction, overdraft check, balance recheck and idempotency implemented; no cancel of approved leave in MVP; no payroll formulas T13 or UI',
            'T10': 'Attendance API and durable idempotency implemented; no location proof, UI or actual payroll formulas T13; MissingCheckout is a time-dependent read projection',
            'T09': 'Calendar and pinned snapshots implemented; attendance commands and actual payroll formulas remain T10/T13; no production UAT',
            'T08': 'Employee profile and lifecycle implemented; attendance write port and full email delivery remain T10/T22',
            'T07': 'Core Employee lifecycle/FK and full business logic remain T08-T15; no SMTP delivery or production UAT',
            'T06': 'Business permission/scope remains T07; no SMTP delivery or production auth UAT'
        }.get(task, 'Identity and business permissions remain T06/T07; production read route fails closed')
        evidence['limits'] = [limits_msg,
                              'Legacy has 8 known regression failures matched by its manifest',
                              'No legacy database mutation, production deployment or remote GitHub CI run performed']
    except Exception as exc:
        evidence['status'] = 'FAIL'
        evidence['error'] = redact(str(exc))
        print(evidence['error'], file=sys.stderr)
    finally:
        save()
    print(f'{evidence["status"]}: {run_dir}', flush=True)
    return 0 if evidence['status'] in ('PASS', 'PASS_BACKEND_ONLY') else 1


if __name__ == '__main__':
    raise SystemExit(main())
