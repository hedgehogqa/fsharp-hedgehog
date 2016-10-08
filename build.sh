#!/bin/sh -eu

if [ -z "${COMSPEC-}" ]; then
  MONO="mono"
else
  MONO=""
fi

# Only run bootstrapper if we haven't done so in the last 24 hours, this keeps builds fast.
if ! test -f .paket/paket.bootstrapper.run || find .paket/paket.bootstrapper.run -type f -mtime +1 | grep -q paket.bootstrapper.run; then
    $MONO .paket/paket.bootstrapper.exe
    touch .paket/paket.bootstrapper.run
fi

$MONO .paket/paket.exe restore
$MONO packages/FAKE/tools/FAKE.exe
