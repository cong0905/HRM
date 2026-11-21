"""Owned Android-emulator review helpers; never print fixture credentials."""
import argparse
import http.cookiejar
import json
import os
import re
from pathlib import Path
import subprocess
import ssl
import sys
import time
import xml.etree.ElementTree as ET
import urllib.request

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'docs/verification/t20'
ADB = ROOT / '.cache/android-t20/sdk/platform-tools/adb.exe'
SERIAL = 'emulator-5580'

def fixture():
    host = json.loads((OUT / 'mobile-host.json').read_text())
    return json.loads(Path(host['fixtureFile']).read_text())

def adb(*args, check=True):
    result = subprocess.run([str(ADB), '-s', SERIAL, *map(str, args)], capture_output=True, timeout=90)
    if check and result.returncode:
        raise RuntimeError('Owned emulator adb operation failed: ' + result.stderr.decode(errors='replace')[:250])
    return result.stdout

def snapshot(name):
    image = OUT / (name + '.png')
    for attempt in range(4):
        adb('shell', 'rm', '-f', '/sdcard/t20-window.xml')
        adb('shell', 'uiautomator', 'dump', '/sdcard/t20-window.xml')
        xml = adb('shell', 'cat', '/sdcard/t20-window.xml', check=False).decode('utf-8')
        try: document = ET.fromstring(xml); break
        except ET.ParseError:
            if attempt == 3: raise RuntimeError('Native screenshot UI tree was not ready')
            time.sleep(.5)
    # Avoid persisting entered secret text even when Android masks password nodes.
    for node in document.iter('node'):
        if node.get('password') == 'true':
            node.set('text', '[REDACTED]')
    ET.ElementTree(document).write(OUT / (name + '.xml'), encoding='utf-8')
    time.sleep(.7)
    image.write_bytes(adb('exec-out', 'screencap', '-p'))
    print('Saved', name, flush=True)

def tap_label(label, widget=None):
    deadline = time.monotonic() + 30
    while time.monotonic() < deadline:
        adb('shell', 'rm', '-f', '/sdcard/t20-current.xml')
        adb('shell', 'uiautomator', 'dump', '/sdcard/t20-current.xml')
        try:
            tree = ET.fromstring(adb('shell', 'cat', '/sdcard/t20-current.xml', check=False))
        except ET.ParseError:
            time.sleep(.5)
            continue
        node = next((n for n in tree.iter('node') if (n.get('content-desc', '').casefold() == label.casefold() or n.get('text', '').casefold() == label.casefold()) and (not widget or n.get('class') == widget)), None)
        if node is not None:
            x1, y1, x2, y2 = map(int, re.findall(r'\d+', node.get('bounds')))
            adb('shell', 'input', 'tap', (x1+x2)//2, (y1+y2)//2)
            return
        time.sleep(.5)
    raise TimeoutError('Native control not found in a fresh UI tree: ' + label)

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('action', choices=['prepare', 'metro', 'open', 'snapshot', 'login', 'tap', 'press', 'background', 'resume', 'fill-user', 'fill-password', 'submit', 'revoke-web', 'offline', 'online', 'storage-audit', 'logs-audit'])
    parser.add_argument('value', nargs='?', default='device')
    parser.add_argument('--package', default='host.exp.exponent', choices=['host.exp.exponent', 'com.hrmnext.mobile'])
    args = parser.parse_args()
    if args.action == 'prepare':
        data = fixture()
        adb('root'); time.sleep(1); adb('remount')
        target = '/system/etc/security/cacerts/' + data['caHash'] + '.0'
        adb('push', data['caPath'], target)
        adb('shell', 'chmod', '644', target)
        adb('reverse', 'tcp:8081', 'tcp:8081')
        adb('reverse', f"tcp:{data['port']}", f"tcp:{data['port']}")
        print('Trusted disposable fixture CA inside owned emulator; API/Metro reverse ports ready.')
    elif args.action == 'metro':
        env = os.environ.copy()
        env.update(CI='1', EXPO_PUBLIC_API_URL=fixture()['apiOrigin'], EXPO_NO_TELEMETRY='1')
        raise SystemExit(subprocess.call(['node', str(ROOT / 'mobile/node_modules/expo/bin/cli'),
                                         'start', '--localhost', '--port', '8081', '--clear'],
                                        cwd=ROOT / 'mobile', env=env))
    elif args.action in ('open', 'resume'):
        if args.package == 'host.exp.exponent':
            adb('shell', 'am', 'start', '-a', 'android.intent.action.VIEW', '-d', 'exp://127.0.0.1:8081', args.package)
        else:
            adb('shell', 'am', 'start', '-n', args.package + '/.MainActivity')
        print('Opened', args.package, 'on owned emulator.')
    elif args.action == 'snapshot':
        snapshot(args.value)
    elif args.action == 'login':
        # Coordinates were observed from the current Android screenshot/XML.
        # value format: actor,xUser,yUser,xPassword,yPassword,xButton,yButton
        actor, *coordinates = args.value.split(',')
        xu, yu, xp, yp, xb, yb = coordinates
        data = fixture()
        adb('shell', 'input', 'tap', xu, yu)
        time.sleep(.3)
        adb('shell', 'input', 'keyevent', '123')
        adb('shell', 'input', 'keyevent', *(['67'] * 80))
        adb('shell', 'input', 'text', data['actors'][actor])
        adb('shell', 'input', 'tap', xp, yp)
        time.sleep(.3)
        adb('shell', 'input', 'keyevent', '123')
        adb('shell', 'input', 'keyevent', *(['67'] * 80))
        adb('shell', 'input', 'text', data['password'])
        adb('shell', 'input', 'keyevent', '4')
        adb('shell', 'input', 'tap', xb, yb)
        print('Submitted owned fixture login (credentials not logged).')
    elif args.action == 'tap':
        x, y = args.value.split(',')
        adb('shell', 'input', 'tap', x, y)
    elif args.action == 'press':
        tap_label(args.value)
    elif args.action in ('fill-user', 'fill-password'):
        label = 'Tên đăng nhập' if args.action == 'fill-user' else 'Mật khẩu'
        tap_label(label, 'android.widget.EditText')
        time.sleep(.4)
        adb('shell', 'input', 'keyevent', '123')
        adb('shell', 'input', 'keyevent', *(['67'] * 80))
        data = fixture()
        value = data['actors'].get(args.value, data['actors']['Employee']) if args.action == 'fill-user' else data['password']
        adb('shell', 'input', 'text', value)
        print('Filled fixture field; value not logged.')
    elif args.action == 'submit':
        keyboard = adb('shell', 'dumpsys', 'input_method').decode(errors='replace')
        if 'mInputShown=true' in keyboard:
            adb('shell', 'input', 'keyevent', '4')
            time.sleep(.5)
        tap_label('Đăng nhập', 'android.widget.Button')
    elif args.action == 'background':
        adb('shell', 'input', 'keyevent', '3')
    elif args.action == 'revoke-web':
        data = fixture()
        cookies = http.cookiejar.CookieJar()
        context = ssl.create_default_context(cafile=data['caPath'])
        client = urllib.request.build_opener(urllib.request.HTTPCookieProcessor(cookies), urllib.request.HTTPSHandler(context=context))
        def request(path, body=None, csrf=None):
            headers = {'Content-Type': 'application/json'}
            if csrf: headers['X-XSRF-TOKEN'] = csrf
            payload = None if body is None else json.dumps(body).encode()
            with client.open(urllib.request.Request(data['apiOrigin'] + path, data=payload, headers=headers), timeout=30) as response:
                content = response.read()
                return response.status, json.loads(content) if content else None
        _, csrf = request('/api/v1/auth/csrf')
        login_status, _ = request('/api/v1/auth/web/login', {'username': data['actors']['Employee'], 'password': data['password']}, csrf['requestToken'])
        _, csrf = request('/api/v1/auth/csrf')
        revoke_status, _ = request('/api/v1/auth/logout-all', {}, csrf['requestToken'])
        result = {'webCookieLoginStatus': login_status, 'webCsrfLogoutAllStatus': revoke_status,
                  'transport': 'real HTTPS with disposable CA validation', 'nativeRevocationStillNeedsUiCheck': True}
        (OUT / 'web-revocation.json').write_text(json.dumps(result, indent=2))
        print('Real web-cookie login and CSRF-protected logout-all:', login_status, revoke_status)
    elif args.action in ('offline', 'online'):
        data = fixture()
        if args.action == 'offline': adb('reverse', '--remove', f"tcp:{data['port']}")
        else: adb('reverse', f"tcp:{data['port']}", f"tcp:{data['port']}")
        print('Owned fixture API connectivity:', args.action)
    elif args.action == 'storage-audit':
        preferences = '/data/data/' + args.package + '/shared_prefs/'
        names = adb('shell', 'ls', preferences).decode().splitlines()
        name = next(n.strip() for n in names if n.strip().endswith('SecureStore.xml'))
        raw = adb('shell', 'cat', preferences + name).decode()
        tree = ET.fromstring(raw)
        entries = [n.text or '' for n in tree if 'hrm_secure_session_v1' in n.get('name', '')]
        if args.value != 'cleared' and not entries: raise RuntimeError('No native SecureStore session entry found')
        if args.value == 'cleared' and entries: raise RuntimeError('Session remains in native SecureStore')
        encrypted = all(isinstance(json.loads(value), dict) and 'ct' in json.loads(value) for value in entries)
        cleartext = any('accessToken' in value or 'refreshToken' in value for value in entries)
        prefix = 'apk-' if args.package == 'com.hrmnext.mobile' else ''
        (OUT / (prefix + 'native-storage-' + args.value + '.json')).write_text(json.dumps({'package': args.package, 'secureStoreEntries': len(entries), 'encryptedCiphertextRecords': encrypted, 'plaintextTokenFieldsFound': cleartext}, indent=2))
        print('Native SecureStore inspected without printing ciphertext or tokens:', encrypted, cleartext)
    elif args.action == 'logs-audit':
        log = adb('logcat', '-d').decode(errors='replace')
        data = fixture()
        sensitive = data['password'] in log or bool(re.search(r'(?:accessToken|refreshToken)\s*["\']?\s*[:=]\s*["\'][A-Za-z0-9_-]{20,}', log))
        (OUT / 'device-log-audit.json').write_text(json.dumps({'fixturePasswordFound': data['password'] in log, 'credentialPatternFound': sensitive, 'rawLogsPersisted': False}, indent=2))
        print('Device logs scanned in memory; credential pattern found:', sensitive)

if __name__ == '__main__':
    main()
