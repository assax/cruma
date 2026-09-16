# Provoz Cruma

Návrh: `.architecture/architecture-cruma/infra/deployment-pattern.md`. Vše běží z `compose.yaml` v Dockeru
i Podmanu (OPS-001); tajné údaje jsou v `deploy/.env` podle `.env.example` (SEC-006).

| Soubor | Účel |
|---|---|
| `compose.yaml` | profil `dev` (PostgreSQL pro vývoj), `prod` (proxy, server, db, backup), `tools` (migrate, jednorázová záloha a obnova) |
| `server.Dockerfile` | image `cruma-server:<verze>` (server + webový klient) |
| `Caddyfile` | reverzní proxy s automatickým TLS a HSTS |
| `backup/` | image záložní úlohy (pg_dump + restic), `backup.sh`, `restore.sh` |
| `deploy.sh` | nasazení: image → záloha → migrace → start → kontrola zdraví → návrat při selhání |
| `RESTORE.md` | postup obnovy a záznamy zkoušek |
| `desktop/pack.ps1` | instalátor a aktualizační feed desktopu (Velopack) |

## Nasazení

```bash
deploy/deploy.sh 0.1.0
```

S Podmanem: `COMPOSE="podman compose" deploy/deploy.sh 0.1.0`.

## Feed desktopu

Release workflow vytvoří artefakt `desktop-feed-<verze>` (výstup `pack.ps1`). Obsah se nahraje do svazku
`cruma_desktop-feed`, server ho vystaví bez přihlášení pod `https://<doména>/desktop/` (I1-D-5, plan.md N-6):

```bash
docker run --rm -v cruma_desktop-feed:/feed -v "$PWD/desktop-feed:/source:ro" alpine sh -c 'cp -R /source/. /feed/'
```

Desktop publikovaný s `-FeedUrl https://<doména>/desktop/` pak nabídne aktualizaci v Nastavení (FR-37 akc. 4).

## Zkouška nanečisto na vývojovém počítači

```bash
cd deploy
podman build -f server.Dockerfile -t cruma-server:0.1.0 ..
COMPOSE="podman compose" ENV_FILE=.env.rehearsal SKIP_PULL=1 bash deploy.sh 0.1.0
```

`.env.rehearsal`: `CRUMA_DOMAIN=localhost`, `CRUMA_SERVER_IMAGE=localhost/cruma-server`,
`RESTIC_REPOSITORY=/local-repository`, porty `CRUMA_HTTP_PORT=18080`, `CRUMA_HTTPS_PORT=18443`.
V Git Bash spouštět `compose run --entrypoint /…` s `MSYS_NO_PATHCONV=1`.
