#!/bin/sh
# start.sh — entrypoint for unified container
# Starts the .NET API on 127.0.0.1:5000, then Nginx on $PORT.

set -e

# Default to 8080 if PORT is not set by environment (Voroa/Render/Cloud sets $PORT)
export PORT="${PORT:-8080}"

# ── 1. Start the .NET API (Kestrel on localhost:5000) ───────────────────
cd /app/api

export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Production}"
export ASPNETCORE_URLS="http://127.0.0.1:5000"
export ASPNETCORE_CONTENTROOT=/app/api
export DOTNET_ROLL_FORWARD=Major
export DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE=false

dotnet /app/api/ScreenEdge.Api.dll &
API_PID=$!

# Wait for Kestrel to start up and verify process is alive
echo "Waiting for .NET API to start on http://127.0.0.1:5000..."
count=0
max_retries=30
while [ $count -lt $max_retries ]; do
    if ! kill -0 $API_PID 2>/dev/null; then
        echo "[FATAL] .NET API process exited unexpectedly! See error logs above."
        exit 1
    fi
    if curl -s -f -o /dev/null http://127.0.0.1:5000/api/screener 2>/dev/null || curl -s -o /dev/null http://127.0.0.1:5000 2>/dev/null; then
        echo ".NET API is up and listening on port 5000."
        break
    fi
    sleep 1
    count=$((count + 1))
done

# ── 2. Substitute $PORT into Nginx config and start Nginx ───────────────
envsubst '${PORT}' < /etc/nginx/nginx.conf.template > /etc/nginx/nginx.conf

echo "Starting Nginx on port ${PORT}..."
exec nginx -g 'daemon off;'
