"""T16 backend evidence gate: T05-T15 matrices and session access query. Web/browser checks are separate."""
import argparse,json,os,sys
from pathlib import Path
import xml.etree.ElementTree as ET
from verify_t05 import main,NS

ACCESS_CASES = {'Access_ServerPermissionsMatchAuthenticatedRole_WithoutPrivateData': 4, 'Access_AnonymousCannotGetPermissions': 1}

def verify_access_matrix(path):
    rows=ET.parse(path).findall('.//t:UnitTestResult',NS);counts={}
    for case,minimum in ACCESS_CASES.items():
        matches=[r for r in rows if ('.'+case) in r.get('testName','')]
        if len(matches)<minimum or any(r.get('outcome')!='Passed' for r in matches):
            raise ValueError(f'Missing, skipped or failing T16 case: {case}')
        counts[case]=len(matches)
    return counts

def run():
    if not os.environ.get('HRM_T05_SQLSERVER','').strip():
        parser=argparse.ArgumentParser(add_help=False);parser.add_argument('--output');args,_=parser.parse_known_args()
        if args.output:
            p=Path(args.output).resolve();p.mkdir(parents=True,exist_ok=False)
            (p/'result.json').write_text(json.dumps({'status':'NOT_RUN','task':'T16','executed':0,'reason':'Real SQL Server is required; no skip or InMemory substitute.'},indent=2),encoding='utf-8')
        print('NOT_RUN: configure HRM_T05_SQLSERVER; T16 gate blocked.',file=sys.stderr);return 2
    return main(task='T16')

if __name__=='__main__':raise SystemExit(run())
