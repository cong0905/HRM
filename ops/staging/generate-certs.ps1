$certsDir = Join-Path $PSScriptRoot "certs"
if (-not (Test-Path $certsDir)) {
    New-Item -ItemType Directory -Path $certsDir -Force | Out-Null
}

$certPath = Join-Path $certsDir "staging.crt"
$keyPath = Join-Path $certsDir "staging.key"

if ((Test-Path $certPath) -and (Test-Path $keyPath)) {
    Write-Host "Staging certificates already exist in $certsDir."
    exit 0
}

Write-Host "Generating self-signed staging TLS certificates for Windows / local staging..."
if (Get-Command openssl -ErrorAction SilentlyContinue) {
    & openssl req -x509 -nodes -days 365 -newkey rsa:2048 `
        -keyout $keyPath -out $certPath `
        -subj "/C=VN/ST=HoChiMinh/L=HoChiMinh/O=HRM/OU=Staging/CN=staging.hrm.local" `
        -addext "subjectAltName=DNS:staging.hrm.local,DNS:localhost,IP:127.0.0.1"
    Write-Host "Certificates generated via OpenSSL."
} else {
    $cert = New-SelfSignedCertificate -DnsName "staging.hrm.local", "localhost", "127.0.0.1" -CertStoreLocation "Cert:\CurrentUser\My" -NotAfter (Get-Date).AddYears(1)
    $pwd = ConvertTo-SecureString -String "StagingSecret123!" -Force -AsPlainText
    $pfxPath = Join-Path $certsDir "staging.pfx"
    Export-PfxCertificate -Cert $cert -FilePath $pfxPath -Password $pwd | Out-Null
    
    # Export PEM/CRT representation
    $certBytes = $cert.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Cert)
    $certB64 = [Convert]::ToBase64String($certBytes, [Base64FormattingOptions]::InsertLineBreaks)
    $certPem = "-----BEGIN CERTIFICATE-----`r`n" + $certB64 + "`r`n-----END CERTIFICATE-----`r`n"
    [IO.File]::WriteAllText($certPath, $certPem)
    [IO.File]::WriteAllText($keyPath, "# Private key embedded in staging.pfx (password: StagingSecret123!)")
    Write-Host "Self-signed certificate generated as $pfxPath and $certPath."
}
