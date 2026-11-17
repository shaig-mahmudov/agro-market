# AgroMarket — B2B Direct Agri-Marketplace MVP

Managed B2B agricultural marketplace connecting local farmers directly with restaurants and cafes (HoReCa) via a **Managed Milk-Run Marketplace** model.

See [b2b_agri_marketplace_mvp_project_plan.md](./b2b_agri_marketplace_mvp_project_plan.md) for the full technical architecture and domain rules.

## Tech Stack

- **Backend:** .NET 10 (LTS) ASP.NET Core Minimal APIs (Vertical Slice Architecture), EF Core 10 (`Npgsql`), FluentValidation, `SixLabors.ImageSharp`, OpenAPI + Scalar
- **Database & Media Storage:** PostgreSQL 17 (with automatic PNG/JPG/JPEG → WebP conversion stored in `media_assets` and served with immutable HTTP caching)
- **Frontend:** Next.js 16 (React) + Tailwind CSS (PWA / Static Export for GitHub Pages)
- **Notifications:** Telegram Bot / Phone OTP
- **Infrastructure:** Render Blueprint (`render.yaml`), Docker, Docker Compose, Caddy (automatic HTTPS), EF Core `efbundle`

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
- Bootstrap accounts endpoint: `GET /api/auth/bootstrap-accounts`
- Scalar API Reference: `GET /scalar/v1`

## Bootstrap Accounts & Sample Data

On startup (`Seed:Enabled = true`), `DatabaseSeedHostedService` idempotently seeds **9 bootstrap accounts** (with `BusinessProfiles`), **9 tiered produce listings** across 3 verified farms, **3 multi-farmer `CheckoutGroups` (6 split `Orders`)**, and **2 morning Milk-Run `DeliveryRuns`**.

All bootstrap accounts can sign in using Demo OTP code **`123456`** (when `Auth:EnableBootstrapDemoOtp = true`):

| Role | Phone Number (E.164) | Full Name | Business Name | Region | Verified |
| :--- | :--- | :--- | :--- | :--- | :--- |
| **Admin** | `+994500000000` | Platform Admin | AgroMarket Operations HQ | Baku | Yes |
| **Farmer** | `+994501111111` | Rashad Aliyev | Guba Highland Orchards | Guba-Khachmaz | Yes |
| **Farmer** | `+994501111112` | Farid Hasanov | Lankaran Citrus & Subtropics | Lankaran-Astara | Yes |
| **Farmer** | `+994501111113` | Aysel Mammadova | Zira Eco Greenhouses | Absheron | Yes |
| **Farmer** | `+994501111114` | Kamran Babayev | Sheki Walnut & Orchard Co. | Sheki-Zagatala | No *(Pending)* |
| **Restaurant** | `+994502222221` | Leyla Mammadova | Caspian Bistro & Grill | Baku | Yes |
| **Restaurant** | `+994502222222` | Orkhan Guliyev | Icherisheher Courtyard Kitchen | Baku | Yes |
| **Restaurant** | `+994502222223` | Nigar Karimova | Port Baku Artisan Cafe | Baku | No *(Pending)* |
| **Driver** | `+994503333331` | Elchin Mustafayev | Baku Cold-Chain Fleet | Baku | Yes |

## Implementation Roadmap (Incremental Tasks)

- [x] **Task 1:** Repository setup, `.NET 10` VSA solution scaffolding, shared `Common/` primitives, Scalar/OpenAPI, `docker-compose.yml`
- [x] **Task 2:** Core domain entities, EF Core `AppDbContext`, PostgreSQL configurations (`numeric`, `JSONB`, `xmin`), and initial migration
- [x] **Task 3:** Passwordless Phone OTP auth, JWT + Refresh Tokens & Telegram Bot deep-link
- [x] **Task 4:** Admin user verification, Business Profiles & Farmer Product Catalog (`PriceTiers`, `JSONB`, WebP image uploads stored in PostgreSQL)
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

## Production Deployment

### Option A: Render (Backend + PostgreSQL 17)
1. Push this repository to GitHub.
2. In the Render Dashboard, click **New + → Blueprint** and select this repository.
3. Render will automatically detect [`render.yaml`](./render.yaml) and provision:
   - `agromarket-db` (PostgreSQL 17 free tier)
   - `agromarket-api` (Docker Web Service)
4. On startup, the API automatically applies EF Core migrations (`Seed__AutoMigrateOnStartup=true`) and seeds the bootstrap accounts.
5. Set `Cors__AllowedOrigins` to your GitHub Pages URL (e.g. `https://<username>.github.io`).

### Option B: VPS / Docker Compose + Caddy SSL + efbundle

```bash
cp .env.prod.example .env
# Edit .env with your domain, strong POSTGRES_PASSWORD, Jwt__SigningKey & Telegram secrets

# Builds standalone EF Core efbundle migrator (runs before api starts), .NET 10 API, and Caddy HTTPS proxy
docker compose --env-file .env -f docker-compose.prod.yml up -d --build
```
