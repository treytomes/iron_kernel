#!/usr/bin/env bash
set -euo pipefail

RESULTS_DIR="coverage-results"
REPORT_DIR="coverage-report"

echo "==> Running tests with coverage"
dotnet test IronKernel.Tests/IronKernel.Tests.csproj \
  --collect:"XPlat Code Coverage" \
  --results-directory "$RESULTS_DIR" \
  -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura

COBERTURA=$(find "$RESULTS_DIR" -name "coverage.cobertura.xml" | sort | tail -1)

echo "==> Generating report"
reportgenerator \
  -reports:"$COBERTURA" \
  -targetdir:"$REPORT_DIR" \
  -reporttypes:"Html;TextSummary" \
  -assemblyfilters:"+IronKernel.Common;+Userland" \
  -classfilters:"-*.Designer"

echo ""
cat "$REPORT_DIR/Summary.txt"
echo ""
echo "==> Full report: $REPORT_DIR/index.html"
