"""Local-only Android SDK setup/build for the T20 verification fixture."""
import argparse
import json
import os
from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[1]
CACHE = ROOT / '.cache/android-t20'
OUT = ROOT / 'docs/verification/t20'

args = argparse.ArgumentParser()
args.add_argument('action', choices=['sdk', 'build'])
options = args.parse_args()
env = os.environ.copy()
env.update(JAVA_HOME='C:/Program Files/Java/jdk-21', ANDROID_HOME=str(CACHE / 'sdk'),
           ANDROID_USER_HOME=str(CACHE / 'user'), GRADLE_USER_HOME=str(CACHE / 'gradle'),
           EXPO_NO_TELEMETRY='1', CI='1')
if options.action == 'sdk':
    commands = [[str(CACHE / 'sdk/cmdline-tools/latest/bin/android.exe'), '--no-metrics',
                 '--sdk=' + str(CACHE / 'sdk'), 'sdk', 'install', package]
                for package in ('platforms/android-35', 'build-tools/35.0.0', 'ndk/26.1.10909125', 'cmake/3.22.1')]
    log = OUT / 'sdk-install.log'
    stdin = 'y\n' * 100
else:
    host = json.loads((OUT / 'mobile-host.json').read_text())
    fixture = json.loads(Path(host['fixtureFile']).read_text())
    env['EXPO_PUBLIC_API_URL'] = fixture['apiOrigin']
    command = [str(ROOT / 'mobile/android/gradlew.bat'), ':app:assembleRelease', '--no-daemon',
               '--max-workers=2', '-PreactNativeArchitectures=x86_64']
    commands = [command]
    log = OUT / 'android-build.log'
    stdin = ''
print('Starting T20', options.action, '(log retained, no fixture credentials printed)', flush=True)
with log.open('w', encoding='utf-8') as stream:
    for command in commands:
        result = subprocess.run(command, cwd=ROOT / 'mobile/android', env=env, input=stdin,
                                text=True, stdout=stream, stderr=subprocess.STDOUT, timeout=2400)
        if result.returncode: break
print('T20', options.action, 'exit code', result.returncode, flush=True)
if result.returncode:
    print('\n'.join(log.read_text(encoding='utf-8', errors='replace').splitlines()[-25:]))
raise SystemExit(result.returncode)
