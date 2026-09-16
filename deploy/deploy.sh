#!/usr/bin/env bash
# Nasazení Cruma (deployment-pattern.md §3) – jediný podporovaný způsob nasazení.
#
#   deploy/deploy.sh <verze>
#
# Kroky: stažení image → záloha teď (OPS-005) → migrace jako samostatný krok (PER-003) → start nové verze →
# kontrola zdraví → při selhání návrat k předchozí verzi.
#
# Prostředí:
#   COMPOSE    příkaz compose, výchozí „docker compose“ (Podman: COMPOSE="podman compose")
#   ENV_FILE   soubor s tajnými údaji, výchozí deploy/.env (mimo repozitář, SEC-006)
#   SKIP_PULL  1 = image serveru už je lokálně (zkouška nanečisto)
set -euo pipefail

VERSION="${1:?Použití: deploy/deploy.sh <verze>}"
cd "$(dirname "$0")"

COMPOSE="${COMPOSE:-docker compose}"
ENV_FILE="${ENV_FILE:-.env}"
STATE_FILE=".deployed-version"

compose() {
    # shellcheck disable=SC2086
    $COMPOSE --env-file "$ENV_FILE" -f compose.yaml "$@"
}

log() {
    echo "[$(date -u +%H:%M:%S)] $*"
}

[ -f "$ENV_FILE" ] || { echo "Chybí $ENV_FILE (vzor .env.example)" >&2; exit 1; }
set -a
# shellcheck disable=SC1090
. "$ENV_FILE"
set +a
for variable in CRUMA_DOMAIN CRUMA_DB_PASSWORD RESTIC_REPOSITORY RESTIC_PASSWORD; do
    [ -n "${!variable:-}" ] || { echo "V $ENV_FILE chybí $variable" >&2; exit 1; }
done

export CRUMA_VERSION="$VERSION"
previous="$(cat "$STATE_FILE" 2>/dev/null || true)"

log "1/6 image verze $VERSION"
if [ "${SKIP_PULL:-0}" != "1" ]; then
    compose --profile prod pull server proxy db
fi
compose --profile prod build backup

log "2/6 databáze"
compose --profile prod up -d db

if [ -n "$previous" ]; then
    log "3/6 záloha před nasazením (OPS-005)"
    compose --profile tools run --rm backup run
else
    log "3/6 první nasazení – záloha se přeskočí (databáze je prázdná)"
fi

log "4/6 migrace databáze (PER-003)"
compose --profile tools run --rm migrate

log "5/6 start verze $VERSION"
compose --profile prod up -d

log "6/6 kontrola zdraví"
for attempt in $(seq 1 30); do
    if compose --profile prod exec -T proxy wget -q -O - http://server:8080/health >/dev/null 2>&1; then
        echo "$VERSION" > "$STATE_FILE"
        log "nasazeno: $VERSION"
        exit 0
    fi
    sleep 2
done

log "SELHÁNÍ: verze $VERSION neodpovídá"
if [ -n "$previous" ]; then
    log "návrat k předchozí verzi $previous"
    CRUMA_VERSION="$previous" compose --profile prod up -d server
    echo "Pokud migrace verze $VERSION nejsou zpětně kompatibilní, obnovte databázi ze zálohy z kroku 3 (deploy/RESTORE.md)." >&2
fi
exit 1
