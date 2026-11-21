"""Collect actual T20 test evidence and source fingerprints; never grant acceptance."""
import hashlib
import json
from pathlib import Path
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'docs/verification/t20'

def read(name):
    return json.loads((OUT / name).read_text(encoding='utf-8-sig'))

unit = read('unit-final.json')
trx = ET.parse(OUT / 't20-auth.trx')
counters = trx.find('.//{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}Counters').attrib
native = read('apk-run.json')
auth_sql = read('sql-auth-ledger.json')
device_sql = read('sql-mobile-ledger.json')
files = [p for p in (ROOT / 'mobile/src').rglob('*') if p.suffix in {'.ts', '.tsx'}]
files += list((ROOT / 'mobile/plugins').glob('*.cjs'))
files += [ROOT / ('mobile/' + name) for name in ('App.tsx', 'index.ts', 'app.json', 'package.json', 'package-lock.json', 'tsconfig.json')]
files += [ROOT / name for name in ('contracts/src/generated/api.ts', 'contracts/openapi.v1.json',
                                   'backend/tests/HRM.T20MobileReview/Program.cs', 'backend/tests/HRM.T20MobileReview/HRM.T20MobileReview.csproj')]
hashes = {p.relative_to(ROOT).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(files)}
gates = {
    'unit': unit['success'] and unit['numFailedTests'] == 0 and unit['numPendingTests'] == 0,
    'backendAuth': int(counters['failed']) == 0 and int(counters['passed']) == 8,
    'installedApk': native['status'] == 'PASS' and len(native['checks']) == 8,
    'ownedSqlCleanup': auth_sql['cleanupCompleted'] and device_sql['cleanupCompleted'],
}
result = {
    'task': 'T20', 'evidenceStatus': 'PASS' if all(gates.values()) else 'INCOMPLETE',
    'technicalAcceptance': 'owner-delegated decision recorded separately', 'gates': gates,
    'unitTests': {'passed': unit['numPassedTests'], 'failed': unit['numFailedTests'], 'pending': unit['numPendingTests']},
    'backendAuthTests': {k: int(counters[k]) for k in ('passed', 'failed', 'total', 'notExecuted')},
    'native': native, 'sourceSha256': hashes,
    'limits': ['No human UAT, remote CI, production deployment or iOS run',
               'Android9/API28 x86_64 emulator; physical ARM64 devices not tested',
               'APK uses test signing and a disposable local HTTPS fixture URL',
               'Expo52/React Native0.76 is the pinned project baseline, not a claim of current framework support'],
}
(OUT / 'result.json').write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
print('T20 evidence:', result['evidenceStatus'], '| unit', unit['numPassedTests'], '| auth SQL', counters['passed'], '| APK checks', len(native['checks']))
if not all(gates.values()): raise SystemExit(1)
