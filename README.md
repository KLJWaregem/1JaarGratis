# 1JaarGratis
Mimics the point system of the gameshow "1 jaar gratis"

## Running with Docker

```bash
docker compose up -d --build
```

The app listens on `http://localhost:8080` inside the container and persists its SQLite
database in the `db-data` volume, so data survives rebuilds/restarts.

The app calls `UseHttpsRedirection()` and `UseHsts()` (the QR scanner needs HTTPS on
phones), so it expects to run behind a TLS-terminating reverse proxy (e.g. Traefik, Caddy,
nginx) that forwards `X-Forwarded-Proto`. Point that proxy at container port `8080` and it
will pick up the original scheme automatically. Running it standalone without such a proxy
means the browser will be redirected to an HTTPS URL the container isn't serving.

## CI/CD

`.github/workflows/docker-publish.yml` builds the image and pushes it to
`ghcr.io/<owner>/<repo>` on every push to `main` and on version tags (`vX.Y.Z`), using the
repo's built-in `GITHUB_TOKEN` — no extra secrets needed. Enable it by making sure the
repo's Actions settings allow `packages: write` (Settings → Actions → General → Workflow
permissions), which is the default for most repos.
