#!/bin/sh
# Záloha databáze a blobů do restic repozitáře mimo server (FR-36, OPS-002, NFR-9).
#   backup.sh run       – jedna záloha teď (také před každým nasazením, OPS-005)
#   backup.sh schedule  – denně v CRUMA_BACKUP_TIME (UTC, výchozí 03:30)
# Prostředí: RESTIC_REPOSITORY, RESTIC_PASSWORD (šifrovací klíč, mimo server i úložiště záloh),
#            případně AWS_ACCESS_KEY_ID / AWS_SECRET_ACCESS_KEY pro S3; PGHOST, PGUSER, PGDATABASE, PGPASSWORD.
set -eu

ensure_repository() {
    restic cat config >/dev/null 2>&1 || restic init
}

run_backup() {
    started=$(date -u +%Y-%m-%dT%H:%M:%SZ)
    ensure_repository
    dump=/tmp/cruma.dump
    pg_dump --format=custom --file="$dump" "${PGDATABASE:-cruma}"
    paths="$dump"
    if [ -d /blobs ]; then
        paths="$paths /blobs"
    fi
    # shellcheck disable=SC2086
    restic backup --tag cruma --host cruma $paths
    rm -f "$dump"
    # Více verzí záloh: denní, týdenní a měsíční (FR-36 akc. 2).
    restic forget --tag cruma --keep-daily 7 --keep-weekly 5 --keep-monthly 12 --prune
    echo "backup finished (started $started)"
}

seconds_until() {
    target="$1"
    now=$(date -u +%s)
    today=$(date -u +%Y-%m-%d)
    next=$(date -u -d "$today $target" +%s 2>/dev/null || date -u -D "%Y-%m-%d %H:%M" -d "$today $target" +%s)
    if [ "$next" -le "$now" ]; then
        next=$((next + 86400))
    fi
    echo $((next - now))
}

case "${1:-run}" in
    run)
        run_backup
        ;;
    schedule)
        time_of_day="${CRUMA_BACKUP_TIME:-03:30}"
        echo "daily backup scheduled at $time_of_day UTC"
        while true; do
            sleep "$(seconds_until "$time_of_day")"
            run_backup || echo "backup failed" >&2
        done
        ;;
    snapshots)
        restic snapshots --tag cruma
        ;;
    *)
        echo "usage: backup.sh run|schedule|snapshots" >&2
        exit 2
        ;;
esac
