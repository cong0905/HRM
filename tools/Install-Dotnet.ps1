$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$sdkVersion = (Get-Content (Join-Path $repoRoot 'global.json') -Raw | ConvertFrom-Json).sdk.version
if ($sdkVersion -notmatch '^10\.0\.\d+$') { throw 'Expected an exact .NET 10 SDK version in global.json.' }
if (-not [Environment]::Is64BitOperatingSystem -or $env:OS -ne 'Windows_NT') {
    throw 'This installer supports Windows x64. On CI use actions/setup-dotnet with global-json-file.'
}
$installDir = Join-Path $repoRoot '.dotnet'
$dotnetExe = Join-Path $installDir 'dotnet.exe'
if (Test-Path -LiteralPath $dotnetExe) {
    $installedVersion = & $dotnetExe --version
    if ($LASTEXITCODE -eq 0 -and $installedVersion -eq $sdkVersion) {
        Write-Host "SDK $sdkVersion already available at $installDir"
        exit 0
    }
}
$metadata = Invoke-RestMethod 'https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json'
$sdk = $metadata.releases | ForEach-Object { $_.sdks } |
    Where-Object { $_.version -eq $sdkVersion } | Select-Object -First 1
if (-not $sdk) { throw "SDK $sdkVersion not found in Microsoft's release metadata." }
$archive = $sdk.files | Where-Object { $_.rid -eq 'win-x64' -and $_.name -like '*.zip' } | Select-Object -First 1
if (-not $archive -or $archive.url -notlike 'https://builds.dotnet.microsoft.com/dotnet/Sdk/*') {
    throw 'Expected an official SDK archive.'
}
New-Item -ItemType Directory -Force $installDir | Out-Null
$zipPath = Join-Path $installDir 'sdk-download.zip'
if (-not (Test-Path -LiteralPath $zipPath) -or
    (Get-FileHash -LiteralPath $zipPath -Algorithm SHA512).Hash -ne $archive.hash) {
    Invoke-WebRequest -Uri $archive.url -OutFile $zipPath
}
if ((Get-FileHash -LiteralPath $zipPath -Algorithm SHA512).Hash -ne $archive.hash) {
    throw 'SDK SHA512 mismatch. The archive has not been extracted.'
}
Expand-Archive -LiteralPath $zipPath -DestinationPath $installDir -Force
$actualVersion = & $dotnetExe --version
if ($LASTEXITCODE -ne 0 -or $actualVersion -ne $sdkVersion) { throw 'Installed SDK version does not match global.json.' }
@{ version = $sdkVersion; url = $archive.url; sha512 = $archive.hash } |
    ConvertTo-Json | Set-Content (Join-Path $installDir 'install-receipt.json') -Encoding utf8
Write-Host "Verified SDK $sdkVersion installed locally. Use tools\dotnet.cmd or prepend $installDir to PATH."
