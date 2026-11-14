# AgroMarket — B2B Direct Agri-Marketplace MVP

Managed B2B agricultural marketplace connecting local farmers directly with restaurants and cafes (HoReCa) via a **Managed Milk-Run Marketplace** model.

See [b2b_agri_marketplace_mvp_project_plan.md](./b2b_agri_marketplace_mvp_project_plan.md) for the full technical architecture and domain rules.

## Tech Stack

- **Backend:** .NET 10 (LTS) ASP.NET Core Minimal APIs (Vertical Slice Architecture), EF Core 10 (`Npgsql`), FluentValidation, OpenAPI + Scalar
- **Database:** PostgreSQL 17
- **Frontend:** Next.js 16 (React) + Tailwind CSS (PWA)
- **Storage & Notifications:** Cloudflare R2 (S3-compatible) + Telegram Bot / Phone OTP
- **Infrastructure:** Docker, Docker Compose, Caddy (automatic HTTPS), EF Core `efbundle`

## Quick Start (Local with PostgreSQL in Docker)

```bash
# 1. Start PostgreSQL 17 in Docker (no local PostgreSQL install needed)
docker compose up -d postgres

# 2. Apply EF Core migrations & run API locally
dotnet tool restore
dotnet ef database update --project AgroMarket.Api
dotnet run --project AgroMarket.Api

# Or run the full stack (PostgreSQL + API) in Docker Compose
cp .env.example .env
docker compose up --build
# 3. Run the Next.js PWA frontend (optional, in another terminal)
cd agro-market-web
cp .env.example .env.local
npm install
npm run dev
```

Once running locally in Development:
- Frontend PWA: `http://localhost:3000`
- Health check: `GET /health`
- Ping endpoint: `GET /api/ping`
- Scalar API Reference: `GET /scalar/v1`

## Implementation Roadmap (Incremental Tasks)

- [x] **Task 1:** Repository setup, `.NET 10` VSA solution scaffolding, shared `Common/` primitives, Scalar/OpenAPI, `docker-compose.yml`
- [x] **Task 2:** Core domain entities, EF Core `AppDbContext`, PostgreSQL configurations (`numeric`, `JSONB`, `xmin`), and initial migration
- [x] **Task 3:** Passwordless Phone OTP auth, JWT + Refresh Tokens & Telegram Bot deep-link
- [x] **Task 4:** Admin user verification, Business Profiles & Farmer Product Catalog (`PriceTiers`, `JSONB`, R2 presigned uploads)
- [x] **Task 5:** Next.js PWA shell, OTP login, Admin verification, Farmer listing & Restaurant catalog/cart UI
- [x] **Task 6:** Transactional multi-farmer `Checkout` (cutoff window + `xmin` stock lock), 1-click `RepeatOrder` & expired order release worker
- [x] **Task 7:** Farmer order confirmation, Telegram Bot webhook `[Accept]`/`[Reject]` actions & Morning Milk-Run `DeliveryRuns`
- [x] **Task 8:** Weight & quality reconciliation (±10% variance guardrail), buyer confirmation/dispute & COD/BankTransfer/WeeklyInvoice settlement
- [x] **Task 9:** Concurrency/E2E tests & production deployment (`docker-compose.prod.yml`, Caddy SSL, `efbundle` migrations)

## Automated Concurrency & E2E Tests

Run the integration test suite against PostgreSQL 17 (requires `docker compose up -d postgres`):

```bash
dotnet test AgroMarket.slnx --no-restore --disable-build-servers
```

The test suite spins up isolated per-fixture `agromarket_test_*` databases and verifies:
- Simultaneous checkouts exceeding available stock cannot oversell (PostgreSQL transaction + `xmin` optimistic concurrency returns `201 Created` / `409 Conflict`).
- Stale `RowVersion` product updates are rejected with `409 Conflict`.
- End-to-end flows: Phone OTP + verification, multi-farmer split checkout + tier lock-in, Telegram deep-link & inline webhook confirm, farmer reject stock restoration, morning Milk-Run dispatch cascade, `±10%` variance auto-deliver vs `DeliveredPendingApproval`/dispute/approval, payment settlement, and `RepeatOrder`.

## Production Deployment (VPS / Docker Compose + Caddy SSL + efbundle)

```bash
cp .env.prod.example .env
# Edit .env with your domain, strong POSTGRES_PASSWORD, Jwt__SigningKey, Telegram & R2 secrets

# Builds standalone EF Core efbundle migrator (runs before api starts), .NET 10 API, Next.js PWA, and Caddy HTTPS proxy
docker compose --env-file .env -f docker-compose.prod.yml up -d --build
```
