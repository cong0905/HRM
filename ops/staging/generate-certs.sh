#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
CERTS_DIR="${SCRIPT_DIR}/certs"
mkdir -p "${CERTS_DIR}"

if [[ -f "${CERTS_DIR}/staging.crt" && -f "${CERTS_DIR}/staging.key" ]]; then
    echo "Staging certificates already exist in ${CERTS_DIR}."
    exit 0
fi

echo "Generating self-signed staging TLS certificates..."
openssl req -x509 -nodes -days 365 -newkey rsa:2048 \
    -keyout "${CERTS_DIR}/staging.key" \
    -out "${CERTS_DIR}/staging.crt" \
    -subj "/C=VN/ST=HoChiMinh/L=HoChiMinh/O=HRM/OU=Staging/CN=staging.hrm.local" \
    -addext "subjectAltName=DNS:staging.hrm.local,DNS:localhost,IP:127.0.0.1"

chmod 600 "${CERTS_DIR}/staging.key"
chmod 644 "${CERTS_DIR}/staging.crt"

echo "Staging certificates successfully generated at ${CERTS_DIR}."
