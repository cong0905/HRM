"""Download verified official Android archives into this workspace only."""
import argparse
import hashlib
import json
from pathlib import Path
import urllib.request
import xml.etree.ElementTree as ET
import zipfile

ROOT = Path(__file__).resolve().parents[1]
SDK = ROOT / '.cache/android-t20/sdk'
DOWNLOADS = ROOT / '.cache/android-t20/downloads'
OUT = ROOT / 'docs/verification/t20'
REPOSITORIES = {
    'main': 'https://dl.google.com/android/repository/repository2-3.xml',
    'image': 'https://dl.google.com/android/repository/sys-img/android/sys-img2-3.xml',
}

def repository(url):
    with urllib.request.urlopen(url, timeout=60) as response:
        return ET.fromstring(response.read())

def metadata(tree, package, base):
    candidates = [p for p in tree.findall('remotePackage') if p.attrib['path'] == package
                  and p.find('channelRef').attrib.get('ref') == 'channel-0']
    node = candidates[0]
    archive = next(a for a in node.findall('./archives/archive')
                   if a.findtext('host-os') in ('windows', None)
                   and a.findtext('host-arch') in (None, 'x86_64', 'x64'))
    complete = archive.find('complete')
    checksum = complete.find('checksum')
    return {
        'package': package, 'url': base + complete.findtext('url'),
        'size': int(complete.findtext('size')),
        'checksumType': checksum.attrib.get('type', 'sha1'), 'checksum': checksum.text,
    }

def install(item):
    DOWNLOADS.mkdir(parents=True, exist_ok=True)
    SDK.mkdir(parents=True, exist_ok=True)
    archive = DOWNLOADS / item['url'].rsplit('/', 1)[1]
    def valid():
        if not archive.is_file() or archive.stat().st_size != item['size']:
            return False
        digest = hashlib.new(item['checksumType'])
        with archive.open('rb') as stream:
            for chunk in iter(lambda: stream.read(1024 * 1024), b''):
                digest.update(chunk)
        return digest.hexdigest().lower() == item['checksum'].lower()
    if not valid():
        print(f"Downloading {item['package']} ({item['size'] // 1024 // 1024} MiB)", flush=True)
        with urllib.request.urlopen(item['url'], timeout=120) as source, archive.open('wb') as target:
            while chunk := source.read(1024 * 1024):
                target.write(chunk)
        if not valid():
            raise RuntimeError('Archive checksum mismatch; refusing extraction')
    destination = SDK
    if item['package'].startswith('system-images;'):
        # Android archive already contains the x86_64 directory.
        _, version, flavor, _ = item['package'].split(';')
        destination = SDK / 'system-images' / version / flavor
    if item['package'] == 'cmdline-tools;latest':
        destination = SDK / 'cmdline-tools/latest'
    destination.mkdir(parents=True, exist_ok=True)
    with zipfile.ZipFile(archive) as bundle:
        for entry in bundle.infolist():
            name = entry.filename
            if item['package'] == 'cmdline-tools;latest':
                name = name.removeprefix('cmdline-tools/')
            if not name: continue
            target = (destination / name).resolve()
            target.relative_to(destination.resolve())
            if entry.is_dir(): target.mkdir(parents=True, exist_ok=True)
            else:
                target.parent.mkdir(parents=True, exist_ok=True)
                target.write_bytes(bundle.read(entry))
    print(f"Installed {item['package']} (checksum verified)", flush=True)

def main():
    args = argparse.ArgumentParser()
    args.add_argument('--emulator', action='store_true')
    args.add_argument('--runtime', action='store_true')
    args.add_argument('--api', choices=('28', '35'), default='35')
    args.add_argument('--command-tools', action='store_true')
    selected = args.parse_args()
    main_repo = repository(REPOSITORIES['main'])
    base = 'https://dl.google.com/android/repository/'
    items = [metadata(main_repo, name, base) for name in ('emulator', 'platform-tools')]
    if selected.command_tools:
        items = [metadata(main_repo, 'cmdline-tools;latest', base)]
    if selected.runtime:
        image_repo = repository(REPOSITORIES['image'])
        items.append(metadata(image_repo, f'system-images;android-{selected.api};default;x86_64',
                              base + 'sys-img/android/'))
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / 'android-download-manifest.json').write_text(json.dumps(items, indent=2), encoding='utf-8')
    print(json.dumps([{k: i[k] for k in ('package', 'size', 'url')} for i in items], indent=2), flush=True)
    if selected.emulator or selected.runtime or selected.command_tools:
        for item in items:
            install(item)

if __name__ == '__main__':
    main()
