#!/usr/bin/env bash
set -euo pipefail

echo "==> Cleaning solution"
rm -r -f ./publish
rm -r -f ./IronKernel/bin
rm -r -f ./IronKernel/obj
rm -r -f ./IronKernel.Common/bin
rm -r -f ./IronKernel.Common/obj
rm -r -f ./Userland/bin
rm -r -f ./Userland/obj

echo "==> Clean complete"