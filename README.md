![build](https://github.com/planara/planara-gateway/actions/workflows/build.yml/badge.svg)
![deploy](https://github.com/planara/planara-gateway/actions/workflows/deploy.yml/badge.svg?branch=main)
![version](https://img.shields.io/github/v/tag/planara/planara-gateway?sort=semver)

## Planara.Gateway

GraphQL Gateway (HotChocolate Fusion) для микросервисов Planara.

Предоставляет единый GraphQL endpoint для клиента (`/planara/api`) и собирает
общую схему из сабграфов (микросервисов) через Redis schema registry.
Клиенты работают с единой схемой без прямых обращений к сервисам вида
`/api/auth`, `/api/accounts` и т.д.

Gateway не хранит бизнес-логику — только маршрутизирует запросы в сабграфы,
проксирует заголовки и предоставляет общую GraphQL схему.

## Features

- Единый GraphQL endpoint: `/planara/api`
- Fusion Gateway (HotChocolate)
- Сбор/обновление схемы из Redis (schema registry + Pub/Sub обновления)
- Rate limiting (Fixed Window)
- Health check endpoint: `/health`
- CORS (для клиента)
- Header propagation (`Authorization`, `GraphQL-Preflight`)
- Настройка HTTP транспорта для сабграфов (`SocketsHttpHandler`, `MaxConnectionsPerServer`)

## Development notes

- Gateway не заменяет авторизацию сабграфов. Авторизация (`[Authorize]`) и бизнес-правила остаются в микросервисах.
- Заголовок `Authorization` проксируется в сабграфы через header propagation.
