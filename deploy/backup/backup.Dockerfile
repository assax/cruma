# Záložní úloha Cruma (deployment-pattern.md §4, OPS-002): pg_dump ve verzi serveru PostgreSQL + restic.
FROM docker.io/restic/restic:0.19.1 AS restic

FROM docker.io/library/postgres:18.6-alpine
COPY --from=restic /usr/bin/restic /usr/local/bin/restic
COPY deploy/backup/backup.sh /usr/local/bin/backup.sh
COPY deploy/backup/restore.sh /usr/local/bin/restore.sh
RUN chmod 0755 /usr/local/bin/backup.sh /usr/local/bin/restore.sh \
    && mkdir -p /local-repository && chown postgres:postgres /local-repository
# Neprivilegovaný uživatel (deployment-pattern.md §6).
USER postgres
WORKDIR /tmp
ENTRYPOINT ["/usr/local/bin/backup.sh"]
CMD ["schedule"]
