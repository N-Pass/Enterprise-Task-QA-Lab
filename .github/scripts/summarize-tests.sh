#!/usr/bin/env bash
# Reads the counters of every TRX file in the given directory, writes a table to the
# GitHub job summary and fails when a suite executed zero tests (e.g. tests not discovered),
# so the pipeline can never be green without actually running tests.
set -euo pipefail

results_dir="${1:-TestResults}"
summary="${GITHUB_STEP_SUMMARY:-/dev/stdout}"
status=0

shopt -s nullglob
files=("$results_dir"/*.trx)
if [ ${#files[@]} -eq 0 ]; then
  echo "::error::No TRX files found in $results_dir"
  exit 1
fi

{
  echo "| Test results | Total | Passed | Failed |"
  echo "|---|---:|---:|---:|"
} >> "$summary"

for file in "${files[@]}"; do
  counters=$(grep -o '<Counters [^>]*>' "$file")
  read_counter() { echo "$counters" | sed -E "s/.* $1=\"([0-9]+)\".*/\1/"; }
  total=$(read_counter total)
  passed=$(read_counter passed)
  failed=$(read_counter failed)
  name=$(basename "$file" .trx)

  echo "| $name | $total | $passed | $failed |" >> "$summary"
  echo "::notice title=$name::$passed of $total tests passed, $failed failed"

  if [ "$total" -eq 0 ]; then
    echo "::error title=$name::No tests were executed"
    status=1
  fi
done

exit $status
