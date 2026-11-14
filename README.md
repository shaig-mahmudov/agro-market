# AgroMarket — B2B Direct Agri-Marketplace MVP

Managed B2B agricultural marketplace connecting local farmers directly with restaurants and cafes (HoReCa) via a **Managed Milk-Run Marketplace** model.

See [b2b_agri_marketplace_mvp_project_plan.md](./b2b_agri_marketplace_mvp_project_plan.md) for the full technical architecture and domain rules.

## Tech Stack

- **Backend:** .NET 10 (LTS) ASP.NET Core Minimal APIs (Vertical Slice Architecture), EF Core 10 (`Npgsql`), FluentValidation, OpenAPI + Scalar
- **Database:** PostgreSQL 17
- **Frontend:** Next.js (React) + Tailwind CSS (PWA) *(upcoming task)*
- **Storage & Notifications:** Cloudflare R2 (S3-compatible) + Telegram Bot / Phone OTP
- **Infrastructure:** Docker & Docker Compose

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
- [ ] **Task 8:** Weight & quality reconciliation (±10% variance guardrail), buyer confirmation/dispute & COD/BankTransfer/WeeklyInvoice settlement
- [ ] **Task 9:** Concurrency/E2E tests & production deployment (`docker-compose.prod.yml`, Caddy SSL, `efbundle` migrations)
