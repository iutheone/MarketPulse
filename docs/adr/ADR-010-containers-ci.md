# ADR-010: Containers, Kubernetes, and CI

## Status

Accepted (Phase 10)

## Context

Local `dotnet run` is not enough to show how the system ships. Images, a compose profile, cluster manifests, and a CI pipeline belong in the repo. Secrets must stay out of git.

## Decision

- Multi-stage Dockerfiles under `deploy/docker/` for API, workers, and the nginx-hosted SPA.
- Compose profile `app` runs those images against Redpanda/Redis/Postgres. Infra-only remains `docker compose up -d`.
- Kubernetes demo manifests in `deploy/k8s/` use `IfNotPresent` local tags and probes on `/health/live` and `/health/ready`.
- GitHub Actions runs tests, production frontend build, image builds, and vulnerability listings. Known transitive advisories (e.g. MessagePack via SignalR Redis) must not secretly store API keys.

The Twelve Data key is an environment variable / Kubernetes Secret only.

## Consequences

- SPA is same-origin behind nginx (`VITE_API_URL` empty) so SignalR and REST share one host.
- Demo K8s is one replica per component, not HA.
