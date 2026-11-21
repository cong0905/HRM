"""Export live OpenAPI and generate/check the shared TypeScript client.

The short-lived Kestrel instance uses an explicitly fake SQL server and never
queries a database. All real database behavior is covered by integration tests.
"""
import argparse
from contextlib import contextmanager
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
import time
import urllib.request

ROOT = Path(__file__).resolve().parents[1]
CONTRACTS = ROOT / 'contracts'


def canonical(document):
    return json.dumps(document, ensure_ascii=False, sort_keys=True, indent=2) + '\n'


def check_contract(expected, actual):
    if expected != actual:
        raise RuntimeError('OpenAPI drift: runtime contract differs from contracts/openapi.v1.json')


@contextmanager
def running_api(dotnet, evidence):
    evidence.mkdir(parents=True, exist_ok=True)
    env = os.environ.copy()
    env['DOTNET_ROOT'] = str(Path(dotnet).resolve().parent)
    env['ASPNETCORE_ENVIRONMENT'] = 'Development'
    env['ConnectionStrings__Hrm'] = 'Server=invalid.example;Database=HRM_Next_Test_T05_Contracts;Integrated Security=True;Encrypt=True'
    env['Api__ExposeOpenApi'] = 'true'
    log = evidence / 'contract-host.log'
    command = [dotnet, str(ROOT / 'backend/src/HRM.Api/bin/Release/net10.0/HRM.Next.Api.dll'), '--urls', 'http://127.0.0.1:0']
    with log.open('w', encoding='utf-8') as output:
        process = subprocess.Popen(command, cwd=ROOT, env=env, stdout=output, stderr=subprocess.STDOUT)
        try:
            deadline = time.monotonic() + 40
            url = None
            while time.monotonic() < deadline:
                if process.poll() is not None:
                    raise RuntimeError(f'Contract host exited {process.returncode}; see {log}')
                match = re.search(r'Now listening on:\s+(http://127\.0\.0\.1:\d+)', log.read_text(encoding='utf-8', errors='replace'))
                if match:
                    url = match.group(1)
                    break
                time.sleep(0.2)
            if not url:
                raise TimeoutError('Contract host did not become ready')
            yield url
        finally:
            if process.poll() is None:
                process.terminate()
                try:
                    process.wait(timeout=10)
                except subprocess.TimeoutExpired:
                    process.kill()
                    process.wait(timeout=10)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('action', choices=('update', 'check'))
    parser.add_argument('--dotnet', default=shutil.which('dotnet'))
    parser.add_argument('--evidence', required=True)
    args = parser.parse_args()
    if not args.dotnet:
        parser.error('dotnet must be on PATH or supplied explicitly')
    dotnet = str(Path(args.dotnet).resolve())
    evidence = Path(args.evidence).resolve()
    npm = shutil.which('npm.cmd' if os.name == 'nt' else 'npm')
    if not npm:
        parser.error('Node.js/npm is required')
    def run(command):
        subprocess.run(command, cwd=CONTRACTS, check=True)
    with running_api(dotnet, evidence) as url:
        with urllib.request.urlopen(url + '/openapi/v1.json', timeout=15) as response:
            document = json.load(response)
        if any('_tests' in path for path in document['paths']):
            raise RuntimeError('Test-only routes appeared in product OpenAPI')
        actual = canonical(document)
        (evidence / 'openapi.actual.json').write_text(actual, encoding='utf-8')
        target = CONTRACTS / 'openapi.v1.json'
        if args.action == 'update':
            target.write_text(actual, encoding='utf-8')
            run([npm, 'run', 'generate'])
        else:
            check_contract(target.read_text(encoding='utf-8'), actual)
            run([npm, 'run', 'check:generated'])
        run([npm, 'test'])
        run(['node', 'scripts/smoke.mjs', url])
    print(f'Contract {args.action} PASS; live Kestrel host stopped.', flush=True)
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
