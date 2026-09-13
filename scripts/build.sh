#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
: "${TML_PATH:?Set TML_PATH to your tModLoader installation directory}"
dotnet build TerrariaProgression/TerrariaProgression.csproj -c Release -p:TmlInstallPath="$TML_PATH"
