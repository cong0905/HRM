"""Run real T19 browser acceptance on a fresh owned SQL fixture, then clean up."""
import json, os, shutil, socket, subprocess, time
from pathlib import Path
ROOT=Path(__file__).resolve().parents[1]
OUT=ROOT/'docs/verification/t19'
META=OUT/'browser-host.json'
DOTNET=ROOT/'.dotnet/dotnet.exe'
NPM=shutil.which('npm.cmd') or 'npm.cmd'
NPX=shutil.which('npx.cmd') or 'npx.cmd'
def wait_until(predicate,seconds,process):
    end=time.monotonic()+seconds
    while time.monotonic()<end:
        if process.poll() is not None:raise RuntimeError('Background host exited; inspect logs')
        if predicate():return
        time.sleep(.25)
    raise TimeoutError('Host readiness timed out')
def listening():
    try:
        with socket.create_connection(('localhost',3001),timeout=1):return True
    except OSError:return False
OUT.mkdir(parents=True,exist_ok=True)
if listening():raise RuntimeError('Port3001 already in use; stop the owned test Vite first')
if META.exists():META.unlink()
env=os.environ.copy();env['DOTNET_ROOT']=str(DOTNET.parent)
env['HRM_T05_SQLSERVER']='Server=.;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
status={'status':'RUNNING'};host=None;web=None;metadata=None
try:
    with (OUT/'browser-host.log').open('w',encoding='utf-8') as hlog, (OUT/'vite.log').open('w',encoding='utf-8') as vlog:
        host=subprocess.Popen([str(DOTNET),str(ROOT/'backend/tests/HRM.T19BrowserReview/bin/Release/net10.0/HRM.T19BrowserReview.dll')],cwd=ROOT,env=env,stdout=hlog,stderr=subprocess.STDOUT)
        wait_until(META.exists,60,host)
        metadata=json.loads(META.read_text());fixture=json.loads(Path(metadata['fixtureFile']).read_text())
        env.update(HRM_API_ORIGIN=fixture['apiOrigin'],HRM_WEB_CERT=fixture['certPath'],HRM_WEB_KEY=fixture['keyPath'],HRM_T19_FIXTURE_FILE=metadata['fixtureFile'],E2E_BASE_URL='https://localhost:3001')
        web=subprocess.Popen([NPM,'run','dev','--','--host','localhost','--port','3001','--strictPort'],cwd=ROOT/'web',env=env,stdout=vlog,stderr=subprocess.STDOUT,creationflags=subprocess.CREATE_NEW_PROCESS_GROUP)
        wait_until(listening,40,web)
        print('READY: fresh SQL + API + Vite on3001; running T19 browser tests.',flush=True)
        result=subprocess.run([NPX,'playwright','test','--config','playwright.t19.config.ts'],cwd=ROOT/'web',env=env)
        status.update(status='PASS' if result.returncode==0 else 'FAIL',exitCode=result.returncode)
finally:
    if web is not None and web.poll() is None:
        subprocess.run(['taskkill','/PID',str(web.pid),'/T','/F'],capture_output=True)
    if host is not None:
        if metadata is not None:Path(metadata['stopFile']).touch()
        host.wait(timeout=40)
    ledger=OUT/'sql-browser.json'
    status['cleanupCompleted']=json.loads(ledger.read_text()).get('cleanupCompleted',False) if ledger.exists() else False
    (OUT/'browser-run.json').write_text(json.dumps(status,indent=2))
    print('Cleanup:',status['cleanupCompleted'],flush=True)
if status.get('status')!='PASS' or not status['cleanupCompleted']:raise SystemExit(1)
