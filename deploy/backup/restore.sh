#!/bin/sh
# Obnova databáze (a blobů) ze zálohy restic (deploy/RESTORE.md, OPS-003).
#   restore.sh [snapshot]   – výchozí snapshot „latest“
# Databáze se obnoví do PGDATABASE; existující objekty se nahradí. Server během obnovy musí stát.
set -eu

snapshot="${1:-latest}"
target=/tmp/restore
rm -rf "$target"
mkdir -p "$target"

restic restore "$snapshot" --tag cruma --target "$target"
pg_restore --clean --if-exists --no-owner --dbname="${PGDATABASE:-cruma}" "$target/tmp/cruma.dump"

if [ -d "$target/blobs" ] && [ "$(ls -A "$target/blobs" 2>/dev/null)" ]; then
    if [ -w /blobs ]; then
        cp -R "$target/blobs/." /blobs/
    else
        echo "bloby nejsou obnovené: /blobs je jen pro čtení – spusťte obnovu s -v cruma_blob-data:/blobs (RESTORE.md)" >&2
    fi
fi

rm -rf "$target"
echo "restore of snapshot $snapshot finished"
