param(
    [string]$TaskFile,
    [string]$Workspace = 'D:\dotnet project'
)
$ErrorActionPreference = 'Stop'
if (-not $TaskFile) { $TaskFile = Read-Host 'Duong dan file giao viec da duoc ban duyet' }
if (-not (Test-Path -LiteralPath $TaskFile -PathType Leaf)) { throw 'Khong tim thay file giao viec.' }
if (Test-Path -LiteralPath (Join-Path $PSScriptRoot 'STOP')) {
    throw 'Dang co STOP. Chi xoa file STOP sau khi ban muon bat dau luot moi.'
}
python -B (Join-Path $PSScriptRoot 'bridge.py') run --workspace $Workspace --task $TaskFile --max-rounds 3
exit $LASTEXITCODE
