#!/usr/bin/env bash
set -euo pipefail

PUBLISH_DIR="publish"
KERNEL_DLL="$PUBLISH_DIR/IronKernel.dll"

if [[ ! -f "$KERNEL_DLL" ]]; then
  echo "Kernel not published. Run ./publish.sh first."
  exit 1
fi

echo "==> Running IronKernel"
cd "$PUBLISH_DIR"
dotnet IronKernel.dll