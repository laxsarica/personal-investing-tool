#!/bin/sh
# start.sh — entrypoint for unified container
# Starts the .NET API on 127.0.0.1:5000, then Nginx on $PORT.

set -e

# Default to 8080 if PORT is not set by environment (Voroa/Render/Cloud sets $PORT)
export PORT="${PORT:-8080}"

# ── 1. Start the .NET API (Kestrel on localhost:5000) ───────────────────
ASPNETCORE_ENVIRONMENT=Production \
ASPNETCORE_URLS="http://127.0.0.1:5000" \
DOTNET_ROLL_FORWARD=Major \
DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false \
dotnet /app/api/ScreenEdge.Api.dll &

# Give Kestrel a moment before Nginx starts accepting proxied requests
sleep 2

# ── 2. Substitute $PORT into Nginx config and start Nginx ───────────────
envsubst '${PORT}' < /etc/nginx/nginx.conf.template > /etc/nginx/nginx.conf

exec nginx -g 'daemon off;'
