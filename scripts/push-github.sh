#!/usr/bin/env bash
# Non-interactive-friendly push after one-time: brew install gh && gh auth login && gh auth setup-git
set -euo pipefail
cd "$(dirname "$0")/.."
export GIT_TERMINAL_PROMPT=0
if command -v gh >/dev/null 2>&1; then
  gh auth setup-git 2>/dev/null || true
fi
branch=$(git rev-parse --abbrev-ref HEAD)
git push -u origin "$branch"
