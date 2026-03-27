#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
WEB_DIR="$SCRIPT_DIR/FinMind.Web"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "ERROR: dotnet is not installed or not in PATH."
  exit 1
fi

if [ ! -d "$WEB_DIR" ]; then
  echo "ERROR: FinMind.Web directory not found at: $WEB_DIR"
  exit 1
fi

cd "$WEB_DIR"
dotnet run

#并确认服务监听在：
#And confirmed it is listening on:
#http://localhost:5195
