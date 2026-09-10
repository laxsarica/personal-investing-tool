# ═════════════════════════════════════════════════════════════════════════
# Stage 1 — Build Angular Frontend
# ═════════════════════════════════════════════════════════════════════════
FROM node:22-alpine AS web-build

WORKDIR /build/web

# Install dependencies first (layer-cached when package.json hasn't changed)
COPY ScreenEdge.Web/ClientApp/package*.json ./
RUN npm install

# Copy source and build for production
COPY ScreenEdge.Web/ClientApp/ ./
RUN npm run build -- --configuration production

# ═════════════════════════════════════════════════════════════════════════
# Stage 2 — Build .NET Backend
# ═════════════════════════════════════════════════════════════════════════
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS api-build

WORKDIR /build/api

# Restore packages first (layer-cached when .csproj files haven't changed)
COPY ScreenEdge.Api/ScreenEdge.Api.csproj                         ScreenEdge.Api/
COPY ScreenEdge.Entity/ScreenEdge.Entity.csproj                 ScreenEdge.Entity/
COPY ScreenEdge.Repository/ScreenEdge.Repository.csproj         ScreenEdge.Repository/
COPY ScreenEdge.Screener/ScreenEdge.Screener.csproj             ScreenEdge.Screener/
COPY ScreenEdge.Broker/ScreenEdge.Broker.csproj                 ScreenEdge.Broker/
COPY Ta.Indicator/Ta.Indicator.csproj                           Ta.Indicator/
COPY Ta.CustomIndicator/Ta.CustomIndicator.csproj               Ta.CustomIndicator/

RUN dotnet restore ScreenEdge.Api/ScreenEdge.Api.csproj

# Copy full source and publish
COPY ScreenEdge.Api/             ScreenEdge.Api/
COPY ScreenEdge.Entity/          ScreenEdge.Entity/
COPY ScreenEdge.Repository/      ScreenEdge.Repository/
COPY ScreenEdge.Screener/        ScreenEdge.Screener/
COPY ScreenEdge.Broker/          ScreenEdge.Broker/
COPY Ta.Indicator/               Ta.Indicator/
COPY Ta.CustomIndicator/         Ta.CustomIndicator/

RUN dotnet publish ScreenEdge.Api/ScreenEdge.Api.csproj \
    -c Release \
    -o /publish/api \
    --no-restore \
    --self-contained false

# ═════════════════════════════════════════════════════════════════════════
# Stage 3 — Final Runtime Image
#
# Strategy:
#   • Nginx serves Angular static files on $PORT (Voroa / Cloud port)
#   • Nginx reverse-proxies /api/* to .NET Kestrel on 127.0.0.1:5000
#   • start.sh launches both processes; Nginx runs in foreground (PID 1)
# ═════════════════════════════════════════════════════════════════════════
FROM mcr.microsoft.com/dotnet/aspnet:9.0-bookworm-slim AS final

ENV DOTNET_ROLL_FORWARD=Major

# Install nginx + envsubst (from gettext-base) in one layer
RUN apt-get update \
    && apt-get install -y --no-install-recommends nginx gettext-base curl \
    && apt-get clean \
    && rm -rf /var/lib/apt/lists/*

# ── .NET API ──────────────────────────────────────────────────────────
COPY --from=api-build /publish/api /app/api

# ── MasterData Directory (Pre-seed scrip master) ──────────────────────
RUN mkdir -p /app/api/MasterData && chmod 777 /app/api/MasterData
COPY Data/MasterData/OpenAPIScripMaster.json /app/api/MasterData/

# ── Angular Static Files ──────────────────────────────────────────────
COPY --from=web-build /build/web/dist/ClientApp/browser /usr/share/nginx/html

# ── Nginx Configuration (Template; $PORT substituted at startup) ──────
COPY nginx.conf /etc/nginx/nginx.conf.template

# ── Entrypoint Script ─────────────────────────────────────────────────
COPY start.sh /start.sh
RUN chmod +x /start.sh && sed -i 's/\r$//' /start.sh

# Voroa / Cloud sets $PORT dynamically (default 8080)
EXPOSE 8080

CMD ["/start.sh"]
