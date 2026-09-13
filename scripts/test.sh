#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
mkdir -p artifacts
dotnet run --project tests/CoreTests/CoreTests.csproj -c Release | tee artifacts/core-checks.log
