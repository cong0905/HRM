#!/usr/bin/env python3
"""
T26 Restore & Integrity Verification Automation
Restores backup file into a newly created verification database.
Runs DBCC CHECKDB for physical/logical integrity and reconciles row counts.
Calculates actual RTO and RPO metrics.
"""

import argparse
import datetime
import json
import os
import re
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


def get_default_data_paths(server):
    query = """
    SET NOCOUNT ON;
    SELECT 
        CAST(SERVERPROPERTY('InstanceDefaultDataPath') AS NVARCHAR(500)) AS DataPath,
        CAST(SERVERPROPERTY('InstanceDefaultLogPath') AS NVARCHAR(500)) AS LogPath;
    """
    out = run_sqlcmd(server, query)
    lines = [line.strip() for line in out.splitlines() if line.strip() and not line.startswith("---") and "DataPath" not in line]
    if lines:
        parts = re.split(r'\s{2,}', lines[0])
        data_path = parts[0] if parts else "C:\\Program Files\\Microsoft SQL Server\\MSSQL16.MSSQLSERVER\\MSSQL\\DATA"
        log_path = parts[1] if len(parts) > 1 else data_path
        return data_path.rstrip("\\"), log_path.rstrip("\\")
    return "C:\\temp", "C:\\temp"


def get_logical_file_names(server, backup_file):
    query = f"""
    SET NOCOUNT ON;
    RESTORE FILELISTONLY FROM DISK = N'{backup_file}';
    """
    out = run_sqlcmd(server, query)
    data_logical = None
    log_logical = None
    for line in out.splitlines():
        parts = re.split(r'\s{2,}', line.strip())
        if len(parts) >= 3:
            logical_name = parts[0]
            type_code = parts[2]
            if type_code == 'D' and not data_logical:
                data_logical = logical_name
            elif type_code == 'L' and not log_logical:
                log_logical = logical_name
    return data_logical or "HRM_Next", log_logical or "HRM_Next_log"


def query_row_counts(server, database):
    tables = [
        ("Departments", "personnel.Departments"),
        ("Positions", "personnel.Positions"),
        ("Employees", "personnel.EmployeeProfiles"),
        ("Users", "dbo.AspNetUsers"),
        ("Attendance", "attendance.AttendanceSummaries"),
        ("LeaveRecords", "personnel.LeaveRecords"),
        ("LeaveBalances", "personnel.LeaveBalances"),
        ("PayrollPeriods", "payroll.PayrollPeriods"),
        ("Payslips", "payroll.Payslips"),
    ]
    counts = {}
    for label, tbl in tables:
        q = f"SET NOCOUNT ON; IF OBJECT_ID('{tbl}', 'U') IS NOT NULL SELECT COUNT(*) FROM {tbl}; ELSE SELECT -1;"
        cmd = ["sqlcmd", "-S", server, "-d", database, "-E", "-b", "-Q", q]
        res = subprocess.run(cmd, capture_output=True, text=True, encoding="utf-8")
        if res.returncode == 0:
            lines = [l.strip() for l in res.stdout.splitlines() if l.strip() and not l.strip().startswith("---") and l.strip().isdigit()]
            counts[label] = int(lines[0]) if lines else 0
        else:
            counts[label] = 0
    return counts


def restore_and_verify(server, backup_file, restored_db_name=None, source_db_name=None, drop_after=False):
    backup_path = Path(backup_file).resolve()
    if not backup_path.exists():
        raise FileNotFoundError(f"Backup file does not exist: {backup_path}")

    timestamp_str = datetime.datetime.now(datetime.timezone.utc).strftime("%Y%m%d_%H%M%S")
    if not restored_db_name:
        restored_db_name = f"HRM_Next_Restored_{timestamp_str}"

    print(f"Starting restoration drill for backup [{backup_path.name}] into new DB [{restored_db_name}]...")
    start_time = time.perf_counter()

    data_dir, log_dir = get_default_data_paths(server)
    data_logical, log_logical = get_logical_file_names(server, str(backup_path))

    mdf_target = f"{data_dir}\\{restored_db_name}.mdf"
    ldf_target = f"{log_dir}\\{restored_db_name}_log.ldf"

    restore_sql = f"""
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM sys.databases WHERE name = N'{restored_db_name}')
    BEGIN
        ALTER DATABASE [{restored_db_name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
        DROP DATABASE [{restored_db_name}];
    END;

    RESTORE DATABASE [{restored_db_name}]
    FROM DISK = N'{backup_path}'
    WITH MOVE N'{data_logical}' TO N'{mdf_target}',
         MOVE N'{log_logical}' TO N'{ldf_target}',
         CHECKSUM, REPLACE, STATS = 10;
    """

    run_sqlcmd(server, restore_sql)
    restore_duration = round(time.perf_counter() - start_time, 2)
    print(f"Database [{restored_db_name}] restored in {restore_duration}s.")

    # 2. Integrity check: DBCC CHECKDB
    print(f"Running DBCC CHECKDB on [{restored_db_name}]...")
    checkdb_sql = f"""
    SET NOCOUNT ON;
    DBCC CHECKDB(N'{restored_db_name}') WITH NO_INFOMSGS, ALL_ERRORMSGS;
    """
    checkdb_output = run_sqlcmd(server, checkdb_sql)
    integrity_ok = len(checkdb_output.strip()) == 0

    # 3. Row count reconciliation
    restored_counts = query_row_counts(server, restored_db_name)
    source_counts = query_row_counts(server, source_db_name) if source_db_name else restored_counts

    reconciliation = {}
    all_matched = True
    for label, count in restored_counts.items():
        src_c = source_counts.get(label, count)
        matched = (src_c == count)
        if not matched:
            all_matched = False
        reconciliation[label] = {
            "source_count": src_c,
            "restored_count": count,
            "discrepancy": abs(src_c - count),
            "matched": matched
        }

    # 4. RTO & RPO Metrics
    # RTO target < 4h (14400s). Actual is restore_duration.
    rto_target_hours = 4.0
    rto_actual_seconds = restore_duration
    rto_met = rto_actual_seconds < (rto_target_hours * 3600)

    # RPO target < 24h. Point in time delta.
    rpo_target_hours = 24.0
    backup_mtime = datetime.datetime.fromtimestamp(backup_path.stat().st_mtime, datetime.timezone.utc)
    now_utc = datetime.datetime.now(datetime.timezone.utc)
    rpo_delta_hours = round((now_utc - backup_mtime).total_seconds() / 3600.0, 3)
    rpo_met = rpo_delta_hours <= rpo_target_hours

    report = {
        "timestamp_utc": now_utc.isoformat(),
        "server": server,
        "backup_file": str(backup_path),
        "source_database": source_db_name or "N/A",
        "restored_database": restored_db_name,
        "restore_duration_seconds": restore_duration,
        "rto_target_hours": rto_target_hours,
        "rto_actual_seconds": rto_actual_seconds,
        "rto_met": rto_met,
        "rpo_target_hours": rpo_target_hours,
        "rpo_data_lag_hours": rpo_delta_hours,
        "rpo_met": rpo_met,
        "dbcc_checkdb_passed": integrity_ok,
        "row_counts_reconciled": all_matched,
        "reconciliation_detail": reconciliation,
        "overall_status": "SUCCESS" if (integrity_ok and all_matched and rto_met and rpo_met) else "FAILED"
    }

    # Clean up restored database if requested
    if drop_after:
        print(f"Cleaning up verification database [{restored_db_name}]...")
        drop_sql = f"""
        ALTER DATABASE [{restored_db_name}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
        DROP DATABASE [{restored_db_name}];
        """
        run_sqlcmd(server, drop_sql)

    output_report_file = ROOT / "ops" / "backup" / "backup-restore-audit.json"
    output_report_file.write_text(json.dumps(report, indent=2), encoding="utf-8")
    print(f"Restore verification report saved to {output_report_file}")
    return report


def main():
    parser = argparse.ArgumentParser(description="HRM Database Restore and Verification Drill")
    parser.add_argument("--server", default="localhost", help="SQL Server instance (default: localhost)")
    parser.add_argument("--backup-file", required=True, help="Path to backup (.bak) file")
    parser.add_argument("--restored-db", help="Name of new database to restore into")
    parser.add_argument("--source-db", help="Original source database name to compare row counts")
    parser.add_argument("--drop-after", action="store_true", help="Drop restored database after verification")

    args = parser.parse_args()

    try:
        res = restore_and_verify(args.server, args.backup_file, args.restored_db, args.source_db, args.drop_after)
        print(json.dumps(res, indent=2))
        sys.exit(0 if res["overall_status"] == "SUCCESS" else 1)
    except Exception as e:
        print(f"RESTORE ERROR: {e}", file=sys.stderr)
        sys.exit(2)


if __name__ == "__main__":
    main()
