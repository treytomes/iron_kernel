#!/usr/bin/env bash
set -euo pipefail

echo "==> Publishing IronKernel"
dotnet publish IronKernel/IronKernel.csproj -c Debug

echo "==> Publishing Userland"
dotnet publish Userland/Userland.csproj -c Debug

echo
echo "==> Publish complete"
echo "Kernel   -> publish"
echo "Userland -> publish/userland"
