#!/usr/bin/env bash
# Despliegue del paquete en el servidor: modo Cloudflare Tunnel (sin dominio ni puertos abiertos).
# Uso:  bash desplegar-servidor.sh
# Requiere: docker + docker compose v2, openssl. No necesita sudo si el usuario está en el grupo docker.
set -euo pipefail

ORIGEN="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DEST="${DEST:-$HOME/budget-app}"
PROYECTO="budget-civil"
RED="${PROYECTO}_interna"

echo "==> Destino: $DEST"
mkdir -p "$DEST/dist" "$DEST/deploy"
cp -f "$ORIGEN/docker-compose.prod.yml" "$ORIGEN/docker-compose.tunnel.yml" "$ORIGEN/.env.example" "$DEST/"
cp -f "$ORIGEN/deploy/Caddyfile" "$ORIGEN/deploy/Caddyfile.tunnel" "$DEST/deploy/"
[ -f "$ORIGEN/api.tar" ] && { echo "==> Cargando imagen desde api.tar"; docker load -i "$ORIGEN/api.tar"; }
rm -rf "${DEST:?}/dist"/* && cp -r "$ORIGEN/dist/." "$DEST/dist/"
cd "$DEST"

# 1) Variables de entorno con secretos generados en el servidor
if [ ! -f .env.prod ]; then
  MSSQL_SA_PASSWORD="Budget.$(openssl rand -hex 8)!"
  JWT_SIGNING_KEY="$(openssl rand -base64 48 | tr -d '\n')"
  cat > .env.prod <<EOF
# Generado por desplegar-servidor.sh el $(date -u +%F' '%T) UTC
DOMINIO=localhost
CORREO_TLS=
IMAGEN_API=budget-civil-api:1.0.0
BD_NOMBRE=BudgetCivilDb
MSSQL_PID=Express
MSSQL_SA_PASSWORD=$MSSQL_SA_PASSWORD
JWT_SIGNING_KEY=$JWT_SIGNING_KEY
JWT_ISSUER=BudgetCivil.Api
JWT_AUDIENCE=BudgetCivil.Client
JWT_EXPIRACION_MINUTOS=120
EOF
  chmod 600 .env.prod
fi

set -a
# shellcheck disable=SC1091
. ./.env.prod
set +a

if ! docker image inspect "$IMAGEN_API" >/dev/null 2>&1; then
  echo "ERROR: no existe la imagen '$IMAGEN_API'."
  echo "Constrúyela con: docker build -t $IMAGEN_API -f Dockerfile .  (o carga api.tar)"
  exit 1
fi

COMPOSE=(docker compose --env-file .env.prod -f docker-compose.prod.yml -f docker-compose.tunnel.yml)

# Modo de solo validación: no crea contenedores.
if [ "${1:-}" = "--validar" ]; then
  "${COMPOSE[@]}" config --quiet && echo "Configuración válida (modo --validar)"
  exit 0
fi

# 2) Base de datos y migraciones (en Production no se migran solas)
echo "==> Levantando SQL Server"
"${COMPOSE[@]}" up -d mssql
estado=""
for _ in $(seq 1 60); do
  estado="$(docker inspect -f '{{.State.Health.Status}}' budget-mssql 2>/dev/null || echo starting)"
  [ "$estado" = "healthy" ] && break
  sleep 5
done
[ "$estado" = "healthy" ] || { echo "ERROR: SQL Server no llegó a healthy"; docker logs --tail 30 budget-mssql; exit 1; }
echo "==> SQL Server healthy"

# Migraciones con la imagen de la API: mismo código, misma configuración, sin artefacto extra.
echo "==> Aplicando migraciones"
"${COMPOSE[@]}" run --rm --no-deps api --migrar

# 3) API + Caddy + túnel
echo "==> Levantando API, Caddy y túnel"
"${COMPOSE[@]}" up -d

echo "==> Esperando la URL pública del túnel (hasta 2 min)"
URL=""
for _ in $(seq 1 24); do
  URL="$(docker logs budget-tunel 2>&1 | grep -o 'https://[a-z0-9-]*\.trycloudflare\.com' | head -1 || true)"
  [ -n "$URL" ] && break
  sleep 5
done

echo "==> Estado de los contenedores"
"${COMPOSE[@]}" ps

if [ -n "$URL" ]; then
  echo
  echo "================ APLICACIÓN PUBLICADA ================"
  echo "  URL:       $URL"
  echo "  Salud:     $URL/health"
  echo "  Bootstrap: POST $URL/api/v1/admin/bootstrap/first-administrator"
  echo "  Nota: la URL cambia cada vez que se reinicia budget-tunel."
  echo "======================================================"
else
  echo "El túnel aún no publicó la URL. Revisa: docker logs budget-tunel"
fi
