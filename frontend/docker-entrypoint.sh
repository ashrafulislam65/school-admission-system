#!/bin/sh
set -e

# Container chalu howar shomoy API_URL theke config.js toiri hoy
API_URL="${API_URL:-http://localhost:8080}"

cat > /usr/share/nginx/html/config.js <<EOF
window.__APP_CONFIG__ = { apiUrl: "${API_URL}" }
EOF