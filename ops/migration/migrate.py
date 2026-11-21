#!/usr/bin/env python3
"""
T23 Migration Orchestration and CLI Tool
Provides dry-run validation, idempotent migration execution, and data reconciliation.
"""

import argparse
import os
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DOTNET_BIN = ROOT / ".dotnet" / ("dotnet.exe" if os.name == "nt" else "dotnet")
PROJECT_PATH = ROOT / "ops" / "migration" / "HrmMigration.csproj"


def get_dotnet():
    if DOTNET_BIN.exists():
        return str(DOTNET_BIN)
    return "dotnet"


def run_command(cmd, legacy_conn, target_conn, report_path=None):
    dotnet = get_dotnet()
    args = [
        dotnet,
        "run",
        "--project",
        str(PROJECT_PATH),
        "--",
        cmd,
        "--legacy",
        legacy_conn,
        "--target",
        target_conn,
    ]
    if report_path:
        args.extend(["--report", str(report_path)])

    env = os.environ.copy()
    env["DOTNET_ROOT"] = str(Path(dotnet).parent)
    env["PATH"] = str(Path(dotnet).parent) + os.pathsep + env.get("PATH", "")

    result = subprocess.run(args, cwd=ROOT, env=env)
    return result.returncode


def main():
    parser = argparse.ArgumentParser(description="HRM T23 Data Migration CLI")
    subparsers = parser.add_subparsers(dest="command", required=True)

    # dry-run
    p_dry = subparsers.add_parser("dry-run", help="Execute read-only data validation")
    p_dry.add_argument("--legacy", default=os.environ.get("HRM_LEGACY_SQLSERVER", ""))
    p_dry.add_argument("--target", default=os.environ.get("HRM_T05_SQLSERVER", ""))
    p_dry.add_argument("--report", help="Output path for validation report")

    # migrate
    p_mig = subparsers.add_parser("migrate", help="Execute migration into target DB")
    p_mig.add_argument("--legacy", default=os.environ.get("HRM_LEGACY_SQLSERVER", ""))
    p_mig.add_argument("--target", default=os.environ.get("HRM_T05_SQLSERVER", ""))

    # reconcile
    p_rec = subparsers.add_parser("reconcile", help="Reconcile source and target data")
    p_rec.add_argument("--legacy", default=os.environ.get("HRM_LEGACY_SQLSERVER", ""))
    p_rec.add_argument("--target", default=os.environ.get("HRM_T05_SQLSERVER", ""))
    p_rec.add_argument("--report", help="Output path for markdown reconciliation report")

    args = parser.parse_args()

    if not args.legacy:
        parser.error("Legacy connection string required via --legacy or HRM_LEGACY_SQLSERVER")
    if not args.target:
        parser.error("Target connection string required via --target or HRM_T05_SQLSERVER")

    code = run_command(args.command, args.legacy, args.target, getattr(args, "report", None))
    sys.exit(code)


if __name__ == "__main__":
    main()
