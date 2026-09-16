# Cruma — Deployment Pattern

> **Type:** Pattern
> **Scope:** infra
> **Enforces:** OPS-001..OPS-005, SEC-006, SEC-008, PER-003
> **Derived from:** D-6.1 (Hetzner Cloud VPS, containers), D-6.3, operator note 2026-09-15 (local Podman, .NET 10); C-18; SPEC FR-36

## Purpose

Describes how Cruma runs locally during development and in production, and how data is backed up and restored.

---

# 1. Environments

| Environment | Server | PostgreSQL | Blobs | Proxy |
|---|---|---|---|---|
| Local development | `dotnet run` natively (debugging, hot reload) | container in Podman | local folder | none (Kestrel HTTPS dev certificate) |
| Tests | in-process test host | Testcontainers (Podman) | temporary folder | none |
| Production | container `cruma-server` | container | volume | container with automatic TLS |

---

# 2. Container definition

`deploy/compose.yaml` — runnable by `podman compose` and `docker compose` without modification (OPS-001):

```text
services
├── proxy          reverse proxy with automatic Let's Encrypt certificates (recommended: Caddy); only public ports 80/443
├── server         cruma-server:<version>; env from secrets; volume for blobs
├── migrate        cruma-server:<version> run with the migration command; started by the deploy script, not on boot
├── db             postgres:<major.minor>; volume for data; internal network only
└── backup         scheduled backup job (see §4)

networks: internal (server, db, backup, migrate), edge (proxy, server)
volumes:  db-data, blob-data
```

- Image tags are explicit versions (OPS-004).
- Secrets are passed as files or environment variables from a file outside the repository (SEC-006).
- A `dev` profile in the same compose file starts only `db` for local development.

---

# 3. Deployment procedure

```text
1. build and tag cruma-server:<version>; push to the registry
2. on the server: pull the new image
3. run backup now                                               (OPS-005)
4. run migrate with the new image                               (PER-003, OPS-005)
5. restart server with the new image
6. health check; on failure: restore previous image tag, restore database from step 3 if migrations are not backward compatible
```

`deploy/deploy.sh` implements these steps; it is the only supported way to deploy.

On the VPS the stack may be supervised with systemd (for Podman, Quadlet units generated from the same service
definitions are acceptable) as long as `compose.yaml` remains the source of truth.

---

# 4. Backups (FR-36)

- **What:** logical PostgreSQL dump + blob volume.
- **When:** at least daily and before every deployment (OPS-002, OPS-005).
- **Where:** off the server, to S3-compatible object storage (e.g. Hetzner Object Storage) or another provider.
- **How:** an encrypted, deduplicating backup tool (recommended: restic) with a retention policy keeping daily,
  weekly and monthly versions.
- **Encryption:** backups are encrypted by the backup tool with a key stored outside the server and outside the
  backup storage (NFR-9).
- **VPS snapshots** (provider feature) are an additional safety net, not a substitute.

# 5. Restore (OPS-003)

`deploy/RESTORE.md` describes, step by step: provisioning a fresh VPS, restoring the database dump and the blob
volume from backup, starting the stack, verifying with a checklist (sign in, open notes with images, search,
sync from desktop). The procedure is executed on a throwaway VPS before real data is stored and after every
change to the backup setup; the date of the last successful test is recorded in that file.

---

# 6. Hardening checklist (production VPS)

- SSH key authentication only; password login disabled.
- Firewall: only 22 (restricted), 80, 443 open.
- Automatic security updates for the OS.
- Containers run as non-root users.
- PostgreSQL not published on any host port.

---

# End of Document
