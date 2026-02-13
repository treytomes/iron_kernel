#!/usr/bin/env bash
set -euo pipefail

echo "==> Building solution"
dotnet build IronKernel.sln -c Release

echo "==> Build complete"