#!/usr/bin/env bash
set -euo pipefail

echo "==> Publishing IronKernel"
dotnet publish IronKernel/IronKernel.csproj -c Release

echo "==> Publishing Userland"
dotnet publish Userland/Userland.csproj -c Release

echo
echo "==> Publish complete"
echo "Kernel   -> publish"
echo "Userland -> publish/userland"
