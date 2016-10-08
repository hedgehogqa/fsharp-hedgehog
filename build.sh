#!/bin/sh -eu

if [ -z "${COMSPEC-}" ]; then
  MONO="mono"
else
  MONO=""
fi

$MONO .paket/paket.bootstrapper.exe
$MONO .paket/paket.exe restore
$MONO packages/FAKE/tools/FAKE.exe
