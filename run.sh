#!/usr/bin/env bash
set -euo pipefail

KERNEL_DLL="publish/IronKernel.dll"

if [[ ! -f "$KERNEL_DLL" ]]; then
  echo "Kernel not published. Run ./publish.sh first."
  exit 1
fi

echo "==> Running IronKernel"
dotnet "$KERNEL_DLL"