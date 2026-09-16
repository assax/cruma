# Obnova Cruma ze zálohy

Postup obnovy podle `deployment-pattern.md` §5 (OPS-003, FR-36). Obnova se musí vyzkoušet na jednorázovém serveru
**před uložením skutečných dat** a znovu po každé změně záloh; datum úspěšné zkoušky se zapisuje níže.

Zálohy dělá služba `backup` (restic): každý den v `CRUMA_BACKUP_TIME` a před každým nasazením (`deploy.sh`).
Obsahují logický dump PostgreSQL (`pg_dump --format=custom`) a svazek blobů, jsou šifrované heslem `RESTIC_PASSWORD`
a uložené mimo server (`RESTIC_REPOSITORY`) s retencí 7 denních, 5 týdenních a 12 měsíčních verzí.

## Co je potřeba mít mimo server

- `RESTIC_PASSWORD` – bez něj zálohy nejdou přečíst (uschovat mimo server i mimo úložiště záloh, NFR-9)
- `RESTIC_REPOSITORY` a přístupové klíče úložiště (`RESTIC_AWS_ACCESS_KEY_ID`, `RESTIC_AWS_SECRET_ACCESS_KEY`)
- ostatní hodnoty z `deploy/.env` (heslo databáze, doména, Google OAuth)
- přístup k repozitáři (kvůli `deploy/`) a k image `ghcr.io/assax/cruma-server:<verze>`

## Postup

1. **Nový server.** Hetzner Cloud VPS podle kontrolního seznamu `deployment-pattern.md` §6 (jen SSH klíče,
   firewall 22/80/443, automatické bezpečnostní aktualizace). Nainstalovat Docker nebo Podman s compose.
2. **Repozitář a tajné údaje.** Naklonovat repozitář, vytvořit `deploy/.env` z uschovaných hodnot.
3. **DNS.** Záznam domény přesměrovat na nový server (pro zkoušku stačí jiná subdoména).
4. **Prázdná databáze a záložní služba:**

   ```bash
   cd deploy
   export CRUMA_VERSION=<verze ze zálohovaného serveru>
   docker compose --env-file .env -f compose.yaml --profile prod up -d db
   docker compose --env-file .env -f compose.yaml --profile tools run --rm backup snapshots
   ```

5. **Obnova databáze a blobů** z posledního (nebo vybraného) snapshotu; svazek blobů musí být připojený pro zápis:

   ```bash
   docker compose --env-file .env -f compose.yaml --profile tools run --rm \
     -v cruma_blob-data:/blobs --entrypoint /usr/local/bin/restore.sh backup latest
   ```

6. **Start systému** stejnou verzí, ze které záloha pochází (migrace se zopakují naprázdno):

   ```bash
   ./deploy.sh <verze>
   ```

7. **Kontrola** (checklist):
   - [ ] `https://<doména>/health` vrací 200
   - [ ] přihlášení Googlem funguje
   - [ ] poznámky jsou vidět v prohlížeči i s obrázky (od I-2)
   - [ ] vyhledávání najde známou poznámku
   - [ ] desktop se synchronizuje (případně po změně adresy serveru)
   - [ ] audit obsahuje záznamy z doby před zálohou

## Zkoušky obnovy

| Datum | Kde | Výsledek |
|---|---|---|
| 2026-09-17 | nanečisto na vývojovém počítači (Podman, `deploy/.env.rehearsal`, lokální restic repozitář) | nasazení 0.1.0 přes `deploy.sh`, záloha, smazání svazku databáze, obnova `restore.sh`, server zdravý, značkový záznam obnoven. **Nenahrazuje zkoušku na jednorázovém serveru** (FR-36 akc. 3, 4). |
| – | jednorázový Hetzner VPS | čeká na T-56 (autor) |
