"""Reuse existing real personnel browser scenarios with fresh evidence and SQL fixture."""
import json
import os
from pathlib import Path
import shutil
import socket
import subprocess
import time

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'docs/verification/personnel-20261116-dispatch'
META = ROOT / 'docs/verification/t17/browser-host.json'
SPEC = ROOT / 'web/e2e/personnel-20261116-review.spec.ts'
CONFIG = ROOT / 'web/playwright.personnel-20261116-review.config.ts'
PORT = 3002


def listening():
    try:
        with socket.create_connection(('localhost', PORT), timeout=1):
            return True
    except OSError:
        return False


def ready(predicate, process, timeout):
    end = time.monotonic() + timeout
    while time.monotonic() < end:
        if process.poll() is not None:
            raise RuntimeError('Owned test host exited; inspect evidence logs.')
        if predicate():
            return
        time.sleep(.25)
    raise TimeoutError('Owned test host readiness timed out.')


def main():
    if listening() or SPEC.exists() or CONFIG.exists():
        raise RuntimeError('Test port or temporary review files already occupied.')
    OUT.mkdir(exist_ok=True)
    source = (ROOT / 'web/e2e/personnel.spec.ts').read_text(encoding='utf-8')
    source = source.replace('../docs/verification/t17', '../docs/verification/personnel-20261116-dispatch')
    source = source.replace('await expect(page).toHaveURL("/");', 'await expect(page).toHaveURL("/", { timeout: 30000 });')
    source = source.replace('async ({ page, browser }) => {\n  await login(page, "HR");', 'async ({ page, browser }) => {\n  test.setTimeout(120000);\n  await login(page, "HR");', 1)
    source = source.replace('async ({ page, browser }) => {\n  const context = await browser.newContext', 'async ({ page, browser }) => {\n  test.setTimeout(120000);\n  const context = await browser.newContext')
    source = source.replace('  const administrator = await browser.newContext', '''  expect((await page.request.get(`/api/v1/employees/${created.profile.id}`)).status()).toBe(403);
  const administrator = await browser.newContext''', 1)
    scope_test = r'''
test("Personnel scope revocation hides loaded PII and denies an unassigned HR", async ({page, browser}) => {
  await login(page, "HR"); await page.goto("/personnel");
  const employees = await (await page.request.get("/api/v1/employees?page=1&pageSize=20")).json();
  const target = employees.items.find((item: {status: string}) => item.status === "DangLamViec");
  expect(target).toBeTruthy();
  await page.getByRole("button", {name: target.fullName, exact: true}).click();
  await expect(page.getByLabel("Số điện thoại", {exact: true})).toBeVisible();
  await page.getByLabel("Số điện thoại", {exact: true}).fill("0999999999");
  await page.getByLabel("Lý do thay đổi", {exact: true}).fill("Owned scope revocation review");
  const context = await browser.newContext({ignoreHTTPSErrors: true, baseURL: "https://localhost:3002"});
  try {
    const admin = await context.newPage(); await login(admin, "SystemAdmin");
    const accounts = await (await admin.request.get(`/api/v1/accounts?page=1&pageSize=20&username=${encodeURIComponent(fixture.actors.HR!)}`)).json();
    const hr = accounts.items.find((item: {username: string}) => item.username === fixture.actors.HR);
    expect(hr).toBeTruthy();
    const csrf = await (await admin.request.get("/api/v1/auth/csrf")).json();
    const scopes = await (await admin.request.get(`/api/v1/accounts/${hr.id}/scopes`)).json();
    for (const scope of scopes) {
      const revoked = await admin.request.delete(`/api/v1/accounts/${hr.id}/scopes/${scope.employeeId}`, {
        headers: {"X-XSRF-TOKEN": csrf.requestToken}, data: {reason: "Owned fixture scope revocation review"}
      });
      expect(revoked.status()).toBe(204);
    }
    const denied = page.waitForResponse(r => r.request().method() === "PATCH" && new URL(r.url()).pathname === `/api/v1/employees/${target.id}`);
    await page.getByRole("button", {name: "Lưu thay đổi", exact: true}).click();
    expect((await denied).status()).toBe(403);
    await expect(page.getByLabel("Số điện thoại", {exact: true})).toHaveCount(0);
    await expect(page.getByText(target.fullName, {exact: true})).toHaveCount(0);
    await page.screenshot({path: path.join(evidence, "scope-revoked.png"), fullPage: true});
    expect((await page.request.get(`/api/v1/employees/${target.id}`)).status()).toBe(403);
    const empty = await page.request.get("/api/v1/employees?page=1&pageSize=20");
    expect(empty.status()).toBe(200);
    expect((await empty.json()).items).toEqual([]);
    const restored = await admin.request.post(`/api/v1/accounts/${hr.id}/scopes`, {
      headers: {"X-XSRF-TOKEN": csrf.requestToken},
      data: {employeeIds: scopes.map((scope: {employeeId: string}) => scope.employeeId), reason: "Restore owned fixture scope for independent offboarding scenario"}
    });
    expect(restored.status()).toBe(204);
  } finally { await context.close(); }
});
'''
    offboard_index = source.index('test("Offboarding from HR')
    source = source[:offboard_index] + scope_test + source[offboard_index:]
    SPEC.write_text(source, encoding='utf-8')
    CONFIG.write_text('''import { defineConfig } from "@playwright/test";
export default defineConfig({
  testDir: "./e2e", testMatch: "personnel-20261116-review.spec.ts",
  workers: 1, fullyParallel: false, retries: 0, timeout: 60000,
  expect: { timeout: 10000 },
  reporter: [["list"], ["json", { outputFile: "../docs/verification/personnel-20261116-dispatch/playwright.json" }]],
  use: { channel: "msedge", baseURL: "https://localhost:3002", ignoreHTTPSErrors: true, trace: "retain-on-failure", screenshot: "only-on-failure" },
  outputDir: "../docs/verification/personnel-20261116-dispatch/browser-artifacts"
});
''', encoding='utf-8')
    env = os.environ.copy()
    env['DOTNET_ROOT'] = str(ROOT / '.dotnet')
    env['HRM_T05_SQLSERVER'] = 'Server=.;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
    old_meta = META.stat().st_mtime_ns if META.exists() else None
    host = web = metadata = None
    result = {'status': 'RUNNING'}
    try:
        with (OUT / 'browser-host.log').open('w', encoding='utf-8') as hlog, (OUT / 'vite.log').open('w', encoding='utf-8') as vlog:
            host = subprocess.Popen([str(ROOT / '.dotnet/dotnet.exe'), str(ROOT / 'backend/tests/HRM.T17BrowserReview/bin/Release/net10.0/HRM.T17BrowserReview.dll')], cwd=ROOT, env=env, stdout=hlog, stderr=subprocess.STDOUT)
            ready(lambda: META.exists() and META.stat().st_mtime_ns != old_meta, host, 90)
            metadata = json.loads(META.read_text())
            fixture = json.loads(Path(metadata['fixtureFile']).read_text())
            (OUT / 'browser-host.json').write_text(json.dumps(metadata, indent=2), encoding='utf-8')
            env.update(HRM_API_ORIGIN=fixture['apiOrigin'], HRM_WEB_CERT=fixture['certPath'], HRM_WEB_KEY=fixture['keyPath'], HRM_T17_FIXTURE_FILE=metadata['fixtureFile'], E2E_BASE_URL=f'https://localhost:{PORT}')
            web = subprocess.Popen([shutil.which('npm.cmd') or 'npm.cmd', 'run', 'dev', '--', '--host', 'localhost', '--port', str(PORT), '--strictPort'], cwd=ROOT / 'web', env=env, stdout=vlog, stderr=subprocess.STDOUT)
            ready(listening, web, 50)
            print('READY: owned SQL fixture, real HTTPS API, personnel browser review.', flush=True)
            test = subprocess.run([shutil.which('npx.cmd') or 'npx.cmd', 'playwright', 'test', '--config', CONFIG.name, '--grep', 'HR creates employee|Employee sees own profile|HR personnel fits|Offboarding from HR|Personnel scope revocation'], cwd=ROOT / 'web', env=env)
            result.update(status='PASS' if test.returncode == 0 else 'FAIL', exitCode=test.returncode)
    except Exception as error:
        result.update(status='ERROR', error=str(error))
    finally:
        if web is not None and web.poll() is None:
            subprocess.run(['taskkill', '/PID', str(web.pid), '/T', '/F'], capture_output=True)
        if host is not None:
            if metadata:
                stop = Path(metadata['stopFile']).resolve()
                if stop.parent.name.startswith('HRM-T17-browser-') and stop.name == 'stop':
                    stop.touch()
            try:
                host.wait(timeout=45)
            except subprocess.TimeoutExpired:
                result['cleanupError'] = 'Owned harness did not exit within 45 seconds.'
        ledger = ROOT / 'docs/verification/t17/sql-browser.json'
        if metadata and ledger.exists():
            shutil.copyfile(ledger, OUT / 'sql-browser.json')
            result['cleanupCompleted'] = json.loads(ledger.read_text()).get('cleanupCompleted', False)
        SPEC.unlink(missing_ok=True)
        CONFIG.unlink(missing_ok=True)
        (OUT / 'browser-run.json').write_text(json.dumps(result, indent=2), encoding='utf-8')
        print(json.dumps(result), flush=True)
    return 0 if result['status'] == 'PASS' and result.get('cleanupCompleted') else 1


if __name__ == '__main__':
    raise SystemExit(main())
