"""One bounded native APK acceptance run against the current owned SQL fixture."""
import hashlib
import json
from pathlib import Path
import subprocess
import sys
import time
import xml.etree.ElementTree as ET
import zipfile
from t20_device_review import ROOT, OUT, ADB, SERIAL, adb, fixture, snapshot, tap_label

PACKAGE = 'com.hrmnext.mobile'
APK = ROOT / 'mobile/android/app/build/outputs/apk/release/app-release.apk'
CHECKS = []
record = None

def check(name, condition):
    if not condition: raise AssertionError(name)
    CHECKS.append(name)
    print('PASS:', name, flush=True)

def helper(action, value='device'):
    result = subprocess.run([sys.executable, '-B', str(ROOT / 'scripts/t20_device_review.py'),
                             action, value, '--package', PACKAGE], cwd=ROOT, capture_output=True, text=True, timeout=100)
    if result.returncode: raise RuntimeError('Native helper failed: ' + result.stderr[-300:])

def ui():
    adb('shell', 'rm', '-f', '/sdcard/t20-acceptance.xml')
    adb('shell', 'uiautomator', 'dump', '/sdcard/t20-acceptance.xml')
    raw = adb('shell', 'cat', '/sdcard/t20-acceptance.xml', check=False)
    try: return ET.fromstring(raw)
    except ET.ParseError: return ET.Element('waiting-for-native-idle')

def labels(tree):
    return '\n'.join((n.get('text', '') + '\n' + n.get('content-desc', '')) for n in tree.iter('node') if n.get('password') != 'true')

def wait_text(expected, timeout=45):
    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        tree = ui()
        if expected in labels(tree):
            time.sleep(1)
            return tree
        time.sleep(1)
    raise TimeoutError('Expected native screen did not appear: ' + expected)

def login(actor):
    wait_text('Đăng nhập hệ thống')
    helper('fill-user', actor)
    helper('fill-password')
    helper('submit')
    return wait_text('Xin chào, ' + fixture()['actors'][actor])

def restart():
    adb('shell', 'am', 'force-stop', PACKAGE)
    helper('open')

def start_record(name):
    global record
    record = (name, subprocess.Popen([str(ADB), '-s', SERIAL, 'shell', 'screenrecord', '--time-limit', '180', '/sdcard/' + name + '.mp4'], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL))

def stop_record():
    global record
    if not record: return
    name, process = record
    ids = adb('shell', 'pidof', 'screenrecord', check=False).decode().strip().split()
    for pid in ids:
        if pid.isdigit(): adb('shell', 'kill', '-2', pid, check=False)
    process.wait(timeout=15)
    adb('pull', '/sdcard/' + name + '.mp4', OUT / (name + '.mp4'))
    record = None

result = {'status': 'RUNNING', 'package': PACKAGE, 'serial': SERIAL,
          'os': 'Android 9/API28 x86_64', 'runtime': 'installed APK, release bundle, test signing',
          'checks': CHECKS}
try:
    if not APK.is_file(): raise FileNotFoundError('Build the owned T20 APK first')
    result['apkSha256'] = hashlib.file_digest(APK.open('rb'), 'sha256').hexdigest()
    with zipfile.ZipFile(APK) as archive:
        bundle = archive.read('assets/index.android.bundle')
    generated_bundle = ROOT / 'mobile/android/app/build/generated/assets/createBundleReleaseJsAndAssets/index.android.bundle'
    if bundle != generated_bundle.read_bytes() or b'localCleared' not in bundle:
        raise AssertionError('APK does not contain the final generated session bundle')
    result['bundleSha256'] = hashlib.sha256(bundle).hexdigest()
    adb('install', '-r', APK)
    adb('shell', 'pm', 'clear', PACKAGE)
    helper('online')
    helper('open')
    wait_text('Đăng nhập hệ thống')
    start_record('apk-login-restore')
    login('Employee')
    snapshot('apk-authenticated')
    helper('storage-audit', 'encrypted')
    storage = json.loads((OUT / 'apk-native-storage-encrypted.json').read_text())
    check('real HTTPS login and encrypted native SecureStore', storage['encryptedCiphertextRecords'] and not storage['plaintextTokenFieldsFound'])
    restart()
    wait_text('Xin chào, ' + fixture()['actors']['Employee'])
    snapshot('apk-restored')
    check('force-stop and cold launch restores a server-validated session', True)
    stop_record()
    start_record('apk-revoke-account-isolation')
    helper('background')
    helper('revoke-web')
    helper('resume')
    tree = wait_text('Đăng nhập hệ thống')
    check('web-cookie CSRF logout-all blocks the resumed native app', fixture()['actors']['Employee'] not in labels(tree))
    helper('storage-audit', 'cleared')
    snapshot('apk-revoked')
    login('EmployeeOther')
    tree = wait_text('Xin chào, ' + fixture()['actors']['EmployeeOther'])
    check('second account does not inherit first-account identity', fixture()['actors']['Employee'] not in labels(tree))
    snapshot('apk-other-account')
    stop_record()
    start_record('apk-offline-logout')
    helper('offline')
    restart()
    tree = wait_text('Tạm dừng kết nối máy chủ')
    check('offline cold launch hides identity and preserves encrypted credentials', all(name not in labels(tree) for name in fixture()['actors'].values()))
    helper('storage-audit', 'encrypted')
    snapshot('apk-offline')
    helper('online')
    tap_label('Thử lại kết nối')
    wait_text('Xin chào, ' + fixture()['actors']['EmployeeOther'])
    check('manual retry after API recovery restores only the current account', True)
    snapshot('apk-online-restored')
    # Removing adb reverse does not terminate existing HTTPS keep-alive sockets.
    # A cold offline launch closes them and leaves a genuine offline session.
    helper('offline')
    restart()
    wait_text('Tạm dừng kết nối máy chủ')
    tap_label('Đăng xuất khỏi thiết bị này')
    tree = wait_text('Máy chủ chưa thể xác nhận', timeout=30)
    check('offline logout hides data and reports unconfirmed server revocation', all(name not in labels(tree) for name in fixture()['actors'].values()))
    helper('storage-audit', 'cleared')
    snapshot('apk-logout-offline')
    helper('online')
    helper('logs-audit')
    log = json.loads((OUT / 'device-log-audit.json').read_text())
    check('device logs contain no fixture password or token credential pattern', not log['credentialPatternFound'])
    result['status'] = 'PASS'
except Exception as error:
    result.update(status='FAIL', error=str(error))
    try: snapshot('apk-failure')
    except Exception: pass
    raise
finally:
    try: stop_record()
    finally:
        try: helper('online')
        finally: (OUT / 'apk-run.json').write_text(json.dumps(result, ensure_ascii=False, indent=2), encoding='utf-8')
