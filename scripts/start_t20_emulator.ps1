$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$toolRoot = Join-Path $projectRoot '.cache/android-t20'
$env:ANDROID_HOME = Join-Path $toolRoot 'sdk'
$env:ANDROID_USER_HOME = Join-Path $toolRoot 'user'
$env:ANDROID_EMULATOR_HOME = $env:ANDROID_USER_HOME
$env:ANDROID_AVD_HOME = Join-Path $toolRoot 'avd'
$deviceRoot = Join-Path $env:ANDROID_AVD_HOME 'HRM_T20_API28.avd'
$evidenceRoot = Join-Path $projectRoot 'docs/verification/t20'
New-Item -ItemType Directory -Force -Path $deviceRoot,$env:ANDROID_USER_HOME | Out-Null
$config = @"
AvdId=HRM_T20_API28
avd.ini.encoding=UTF-8
abi.type=x86_64
hw.cpu.arch=x86_64
hw.cpu.ncore=2
hw.ramSize=2048
hw.lcd.width=480
hw.lcd.height=800
hw.lcd.density=160
hw.keyboard=yes
hw.mainKeys=no
hw.gpu.enabled=yes
hw.gpu.mode=swiftshader
disk.dataPartition.size=2048M
image.sysdir.1=system-images\android-28\default\x86_64\
tag.id=default
tag.display=Default
PlayStore.enabled=false
showDeviceFrame=no
skin.name=480x800
fastboot.forceColdBoot=yes
"@
Set-Content -LiteralPath (Join-Path $deviceRoot 'config.ini') -Value $config -Encoding ascii
Set-Content -LiteralPath (Join-Path $env:ANDROID_AVD_HOME 'HRM_T20_API28.ini') -Value "avd.ini.encoding=UTF-8`npath=$deviceRoot`ntarget=android-28" -Encoding ascii
$port = 5580
if (Get-NetTCPConnection -State Listen -LocalPort $port -ErrorAction SilentlyContinue) { throw 'Emulator port already in use; do not start another instance' }
$process = Start-Process -FilePath (Join-Path $env:ANDROID_HOME 'emulator/emulator.exe') -ArgumentList @('-avd','HRM_T20_API28','-port',"$port",'-no-window','-no-audio','-no-snapshot','-writable-system','-no-boot-anim','-gpu','swiftshader','-memory','2048','-no-metrics') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $evidenceRoot 'emulator.stdout.log') -RedirectStandardError (Join-Path $evidenceRoot 'emulator.stderr.log')
@{ pid=$process.Id; serial='emulator-5580'; avd='HRM_T20_API28'; apiLevel=28; os='Android 9'; headless=$true } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $evidenceRoot 'emulator-state.json')
Write-Output "Started owned T20 Android emulator PID $($process.Id), serial emulator-5580"
