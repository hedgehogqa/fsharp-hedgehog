#!/bin/sh -eu

find_windows_endings () {
  grep -E '$' . -R -l \
    --exclude-dir='.fake' \
    --exclude-dir='.git' \
    --exclude-dir='.paket' \
    --exclude-dir='packages' \
    --exclude-dir='bin' \
    --exclude-dir='obj' \
    --exclude-dir='img' \
    $@
}

# debugging travis
#find .

if find_windows_endings -q; then
  echo "Found windows line endings:"
  find_windows_endings
  exit 1
fi
