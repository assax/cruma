# Image serveru Cruma (cruma-server:<verze>, OPS-004). Sestavení z kořene repozitáře:
#   podman build -f deploy/server.Dockerfile -t cruma-server:0.1.0 .
# Server hostuje i webového klienta (Cruma.Web), jehož editor se sestavuje přes Node.js.

FROM docker.io/library/node:24.9.0-bookworm-slim AS node

FROM mcr.microsoft.com/dotnet/sdk:10.0.401 AS build
COPY --from=node /usr/local/bin/node /usr/local/bin/node
COPY --from=node /usr/local/lib/node_modules /usr/local/lib/node_modules
RUN ln -s /usr/local/lib/node_modules/npm/bin/npm-cli.js /usr/local/bin/npm \
    && ln -s /usr/local/lib/node_modules/npm/bin/npx-cli.js /usr/local/bin/npx

WORKDIR /src
COPY . .
ARG VERSION=0.0.0
RUN dotnet publish src/Cruma.Server/Cruma.Server.csproj -c Release -p:Version=${VERSION} -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0.12-noble-chiseled AS runtime
WORKDIR /app
COPY --from=build /app/publish .
# Chiseled image běží jako neprivilegovaný uživatel „app“ (deployment-pattern.md §6).
USER app
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    Cruma__BehindProxy=true
EXPOSE 8080
ENTRYPOINT ["dotnet", "Cruma.Server.dll"]
