#!/usr/bin/env python3
"""
T26 Backup Automation Script
Performs full native SQL Server backup with CHECKSUM and COMPRESSION.
Calculates SHA256 hash and records execution metadata.
"""

import argparse
import datetime
import hashlib
import json
import os
import subprocess
import sys
import time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]


def run_sqlcmd(server, query):
    cmd = ["sqlcmd", "-S", server, "-E", "-b", "-Q", query]
    result = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8", errors="replace")
    if result.returncode != 0:
        raise RuntimeError(f"sqlcmd failed (code {result.returncode}):\nSTDOUT: {result.stdout}\nSTDERR: {result.stderr}")
    return result.stdout.strip()


def calculate_sha256(filepath):
    sha = hashlib.sha256()
    with open(filepath, "rb") as f:
        while chunk := f.read(1024 * 1024):
            sha.update(chunk)
    return sha.hexdigest()


def backup_database(server, database, output_dir=None):
    if not output_dir:
        output_dir = ROOT / "ops" / "backup" / "snapshots"
    else:
        output_dir = Path(output_dir)
    output_dir.mkdir(parents=True, exist_ok=True)

    timestamp = datetime.datetime.now(datetime.timezone.utc).strftime("%Y%m%d_%H%M%SZ")
    backup_filename = f"{database}_{timestamp}.bak"
    backup_path = output_dir / backup_filename
    absolute_backup_path = str(backup_path.resolve())

    print(f"Starting backup of database [{database}] to [{absolute_backup_path}]...")
    start_time = time.perf_counter()

    # Query with CHECKSUM and COMPRESSION for integrity and speed
    sql = f"""
    SET NOCOUNT ON;
    BACKUP DATABASE [{database}]
    TO DISK = N'{absolute_backup_path}'
    WITH FORMAT, INIT, COMPRESSION, CHECKSUM, STATS = 10;
    """

    sql_output = run_sqlcmd(server, sql)
    elapsed_seconds = round(time.perf_counter() - start_time, 2)

    if not backup_path.exists():
        raise FileNotFoundError(f"Backup file was not created: {absolute_backup_path}")

    file_size_bytes = backup_path.stat().st_size
    file_size_mb = round(file_size_bytes / (1024 * 1024), 2)
    file_hash = calculate_sha256(backup_path)

    metadata = {
        "database": database,
        "server": server,
        "backup_path": absolute_backup_path,
        "backup_filename": backup_filename,
        "backup_timestamp_utc": timestamp,
        "file_size_bytes": file_size_bytes,
        "file_size_mb": file_size_mb,
        "sha256": file_hash,
        "duration_seconds": elapsed_seconds,
        "compressed": True,
        "checksum_verified": True,
        "status": "SUCCESS"
    }

    meta_file = backup_path.with_suffix(".json")
    meta_file.write_text(json.dumps(metadata, indent=2), encoding="utf-8")

    print(f"Backup completed in {elapsed_seconds}s. Size: {file_size_mb} MB. SHA256: {file_hash[:16]}...")
    return metadata


def main():
    parser = argparse.ArgumentParser(description="HRM Database Backup Utility")
    parser.add_argument("--server", default="localhost", help="SQL Server instance (default: localhost)")
    parser.add_argument("--database", required=True, help="Target database name to backup")
    parser.add_argument("--output-dir", help="Destination folder for backup files")

    args = parser.parse_args()

    try:
        meta = backup_database(args.server, args.database, args.output_dir)
        print(json.dumps(meta, indent=2))
    except Exception as e:
        print(f"ERROR: {e}", file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()
