SHELL := /bin/bash

COMPOSE := docker compose
INFRA := docker-compose-infra.yml
APP := docker-compose.yml

DB_SERVICE := postgres
DB_USER := postgres

.PHONY: help \
	infra-up infra-down infra-logs infra-ps infra-reset infra-reset-db \
	app-up app-down app-logs app-ps app-reset \
	all-up all-down all-logs all-ps all-reset \
	db-shell db-create-auth

help:
	@echo "INFRA (docker-compose-infra.yml) — только postgres/kafka/zookeeper/kafka-ui"
	@echo "  infra-up         Поднять инфраструктуру (postgres + kafka + zookeeper + kafka-ui) в фоне"
	@echo "  infra-down       Остановить инфраструктуру (volume/данные сохраняются)"
	@echo "  infra-ps         Показать статус контейнеров инфры"
	@echo "  infra-logs       Смотреть логи инфры (follow, последние 200 строк)"
	@echo "  infra-reset      Пересоздать контейнеры инфры (без удаления volumes/данных)"
	@echo "  infra-reset-db   Сбросить БД: down -v (удалит pgdata), поднять инфру заново и создать DB auth"
	@echo ""
	@echo "APP (docker-compose.yml) — сервисы приложения (обычно запускать вместе с infra)"
	@echo "  app-up           Поднять app-сервисы в фоне (без инфры)"
	@echo "  app-down         Остановить app-сервисы"
	@echo "  app-ps           Показать статус app-сервисов"
	@echo "  app-logs         Смотреть логи app-сервисов (follow, последние 200 строк)"
	@echo "  app-reset        Пересоздать контейнеры app (без удаления volumes)"
	@echo ""
	@echo "ALL (infra + app вместе) — интеграционный стенд"
	@echo "  all-up           Поднять infra + app в фоне (docker compose -f infra -f app up -d)"
	@echo "  all-down         Остановить infra + app (volumes сохраняются)"
	@echo "  all-ps           Показать статус всех сервисов"
	@echo "  all-logs         Смотреть логи всех сервисов (follow, последние 200 строк)"
	@echo "  all-reset        Полный сброс стенда: down -v (удалит volumes, включая БД) + up + создать DB auth"
	@echo ""
	@echo "DB утилиты (работают через infra compose)"
	@echo "  db-shell         Открыть psql внутри контейнера postgres"
	@echo "  db-create-auth   Создать базу auth (idempotent: если есть — ничего не делает)"
	@echo ""
	@echo "Примеры:"
	@echo "  make infra-up               # только инфраструктура для локального запуска сервиса"
	@echo "  make all-reset              # интеграционный стенд с чистой БД"
	@echo "  make infra-reset-db          # быстро стереть БД и поднять заново (infra)"
	@echo ""

infra-up:
	$(COMPOSE) -f $(INFRA) up -d

infra-down:
	$(COMPOSE) -f $(INFRA) down --remove-orphans

infra-logs:
	$(COMPOSE) -f $(INFRA) logs -f --tail=200

infra-ps:
	$(COMPOSE) -f $(INFRA) ps

infra-reset:
	$(COMPOSE) -f $(INFRA) up -d --force-recreate

infra-reset-db:
	$(COMPOSE) -f $(INFRA) down -v --remove-orphans
	$(COMPOSE) -f $(INFRA) up -d
	$(MAKE) db-create-auth

app-up:
	$(COMPOSE) -f $(APP) up -d

app-down:
	$(COMPOSE) -f $(APP) down --remove-orphans

app-logs:
	$(COMPOSE) -f $(APP) logs -f --tail=200

app-ps:
	$(COMPOSE) -f $(APP) ps

app-reset:
	$(COMPOSE) -f $(APP) up -d --force-recreate

all-up:
	$(COMPOSE) -f $(INFRA) -f $(APP) up -d

all-down:
	$(COMPOSE) -f $(INFRA) -f $(APP) down --remove-orphans

all-logs:
	$(COMPOSE) -f $(INFRA) -f $(APP) logs -f --tail=200

all-ps:
	$(COMPOSE) -f $(INFRA) -f $(APP) ps

all-reset:
	$(COMPOSE) -f $(INFRA) -f $(APP) down -v --remove-orphans
	$(COMPOSE) -f $(INFRA) -f $(APP) up -d
	$(MAKE) db-create-auth

db-shell:
	$(COMPOSE) -f $(INFRA) exec $(DB_SERVICE) psql -U $(DB_USER)

db-create-auth:
	$(COMPOSE) -f $(INFRA) exec -T $(DB_SERVICE) psql -U $(DB_USER) -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname='auth'" | grep -q 1 || \
	$(COMPOSE) -f $(INFRA) exec -T $(DB_SERVICE) psql -U $(DB_USER) -d postgres -c "CREATE DATABASE auth;"