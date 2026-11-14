# B2B Direct Agri-Marketplace MVP — Technical Project Plan

## 1. Executive Summary

**Objective:** Build and launch a Minimum Viable Product (MVP) for a **managed B2B agricultural marketplace** connecting local farmers directly with **restaurants and cafes** (HoReCa). Farmers keep ownership of their inventory and set transparent farm-gate prices, while the platform acts as a **lean digital facilitator and logistics coordinator ("Option B: Managed Milk-Run Marketplace")**—eliminating traditional multi-tier broker markups (30–50%) in favor of a transparent platform + delivery fee.

> **MVP Scope Guardrails:**
> * **Target Buyers:** Independent restaurants, cafes, and small kitchens only (supermarkets—with strict barcoding, Net-30/60 credit terms, and penalty SLAs—are deferred to Post-MVP).
> * **Settlement First, Payment Gateway Later:** The core MVP launches with **Cash on Delivery (COD), Bank Transfer on Handover, or Weekly Consolidated Invoicing** for trusted restaurants. Online payment gateways/escrow are deferred until repeat order volume is proven.

### Core Business Workflows
* **Low-Friction Farmer Onboarding & Listing:** Farmers register via phone number (SMS / Telegram OTP + bot deep-link), upload produce photos, set minimum order quantities, and define volume-based tiered pricing. Day-to-day order confirmations can be handled via mobile PWA or simple Telegram Bot inline actions.
* **Restaurant Daily Purchasing Rhythm:** Restaurants browse verified local produce, place multi-farmer orders before the **daily cutoff window (e.g., 19:00)** for **next-morning delivery (06:00–10:00)**, and use **1-click "Repeat Last Order"** for staple restocks.
* **Managed Morning "Milk-Run" Aggregation:** A multi-farmer checkout splits cleanly into per-farmer fulfillment orders behind the scenes, grouped under a single morning pickup/delivery run so the restaurant receives **one consolidated delivery and one invoice**.
* **Weight & Quality Reconciliation:** Orders track **Estimated Order Weight** (at checkout), **Actual Delivered Weight**, and optional **Rejected Weight** (if produce fails doorstep quality check) to calculate `FinalTotalAmount` accurately. Variance beyond a tolerance threshold (e.g., ±10%) requires buyer confirmation or photo proof.

---

## 2. Technology Stack & System Architecture

The backend is designed as a **Single-Project Modular Monolith** structured around **Vertical Slice Architecture (VSA)**. Features are grouped by business capability (`Features/Auth`, `Features/Products`, `Features/Orders`, `Features/Logistics`, `Features/Admin`), minimizing boilerplate and maximizing MVP delivery speed.

| Layer | Technology Choice | Architectural Purpose |
| :--- | :--- | :--- |
| **Runtime & Framework** | **.NET 10 (LTS) — ASP.NET Core Minimal APIs** | High-throughput, low-memory (~60–90 MB RAM) HTTP endpoints using `MapGroup`, `TypedResults`, rate limiting, and primary constructors. |
| **ORM & Data Access** | **Entity Framework Core 10** (`Npgsql`) | Code-first schema management via EF Core Migrations, `AsNoTracking()` for fast read queries, `xmin` optimistic concurrency, and LINQ projections to prevent N+1 issues. |
| **Primary Database** | **PostgreSQL 17** | ACID-compliant transactional storage for inventory, tiered pricing, multi-farmer order splitting, and weight/fee reconciliation. |
| **Validation & API Docs** | **FluentValidation + .NET 10 OpenAPI / Scalar** | Automated request validation via `IEndpointFilter` and interactive API documentation out of the box. |
| **Authentication** | **JWT + Refresh Tokens & Phone OTP** | Passwordless phone auth via SMS / Telegram Gateway API (plus Telegram Bot linking for `TelegramChatId` alerts), protected by built-in IP/phone rate limiting. |
| **Media Storage** | **Cloudflare R2 (S3-Compatible)** | Zero-egress-fee object storage via presigned URLs for compressed WebP produce photos and delivery/scale proof photos. |
| **Containerization** | **Docker & Docker Compose** | Multi-stage .NET 10 container builds and unified local/production environment orchestration. |
| **Frontend (Client)** | **Next.js (React) + Tailwind CSS (PWA)** | Restaurant procurement dashboard (fast catalog, 1-click re-order, invoices) + responsive mobile PWA for farmers and admin dispatch. |

---

## 3. Database Selection: Why PostgreSQL is the Most Logical Choice

**PostgreSQL is unequivocally the best and most logical database for this platform.** For a B2B marketplace handling inventory, financial ledgers, and variable product attributes, PostgreSQL outperforms alternatives across technical fit, scalability, and hosting cost:

1. **Strict ACID Transactions & Concurrency Control:** When two restaurants order from a farmer's last 100 kg of tomatoes at the same second, PostgreSQL's row-level locking and EF Core optimistic concurrency (`xmin` system column) prevent overselling.
2. **Exact Financial & Weight Precision:** PostgreSQL's `numeric(12,2)` / `decimal` types guarantee exact arithmetic for tiered per-kilogram pricing, platform fees, and fractional delivery weights, avoiding floating-point rounding discrepancies.
3. **Hybrid Relational + `JSONB` Schema:** Core transactional tables (`Users`, `Products`, `Orders`) benefit from strict foreign-key integrity, while `JSONB` columns store dynamic produce metadata (e.g., crop variety, harvest date, packaging type, organic status) without frequent schema migrations.
4. **Native Geospatial Expansion (PostGIS):** When scaling to automated milk-run route optimization and radius-based farm clustering (*"farms along Route A within 30 km"*), PostGIS integrates natively into the existing database.
5. **Zero Licensing Cost & Lightweight Containers:** Runs smoothly in lightweight Docker containers and is natively supported on managed Postgres providers and Linux VPS setups.

### Quick Database Comparison for This MVP

| Database | Verdict | Reasoning |
| :--- | :--- | :--- |
| **PostgreSQL** | **Best Choice** | Ideal balance of relational integrity, `JSONB` flexibility, PostGIS readiness, low RAM usage, and free open-source licensing. |
| **SQL Server** | Overkill / Expensive | Great .NET integration, but high memory footprint (2 GB+ RAM minimum) and costly production licensing for startups. |
| **MySQL / MariaDB** | Second Best | Reliable for basic CRUD, but weaker `JSON` querying, inferior geospatial tooling, and less robust transactional DDL compared to Postgres. |
| **MongoDB** | Poor Fit | Lack of native multi-table relational integrity makes tiered pricing, stock deductions, and financial order auditing error-prone. |

---

## 4. Recommended Project Structure (Vertical Slice Architecture)

```text
AqroMarket.Api/
├── Common/                         # ValidationFilter, PagedResult, ErrorHandling, RateLimitPolicies
├── Database/                       # EF Core DbContext, Entities, Configurations, and Migrations
│   ├── Entities/                   # User, BusinessProfile, Product, PriceTier, CheckoutGroup, Order, OrderItem, DeliveryRun
│   └── AppDbContext.cs
├── Features/                       # Self-contained business features (Vertical Slices)
│   ├── Auth/
│   │   ├── SendOtp.cs              # POST /api/auth/send-otp (Rate-limited SMS / Telegram Gateway)
│   │   ├── VerifyOtp.cs            # POST /api/auth/verify-otp (Issues JWT + Refresh Token)
│   │   └── LinkTelegram.cs         # POST /api/auth/link-telegram (Stores TelegramChatId via bot deep-link)
│   ├── Products/
│   │   ├── CreateProduct.cs        # POST /api/products (Farmer lists produce + R2 presigned upload)
│   │   ├── UpdateProduct.cs        # PATCH /api/products/{id} (Stock/price update, soft-delete)
│   │   └── ListProducts.cs         # GET /api/products (Restaurant search & filtering)
│   ├── Orders/
│   │   ├── Checkout.cs             # POST /api/orders/checkout (Splits multi-farmer cart -> CheckoutGroup + Orders)
│   │   ├── RepeatOrder.cs          # POST /api/orders/{checkoutGroupId}/repeat (1-click restaurant re-order)
│   │   ├── FarmerRespond.cs        # PATCH /api/orders/{id}/respond (Accept/Reject before cutoff)
│   │   ├── ReconcileDelivery.cs    # PATCH /api/orders/{id}/reconcile (Actual & rejected kg + proof photo)
│   │   └── ConfirmReceipt.cs       # PATCH /api/orders/{id}/confirm (Buyer confirms/disputes variance > ±10%)
│   ├── Logistics/
│   │   ├── CreateDeliveryRun.cs    # POST /api/logistics/runs (Group morning orders into driver milk-run)
│   │   └── UpdateRunStatus.cs      # PATCH /api/logistics/runs/{id}/status (PickedUp -> InTransit -> Completed)
│   └── Admin/
│       ├── VerifyUser.cs           # PATCH /api/admin/users/{id}/verify (Approve pilot farmers/restaurants)
│       └── MarkOrderPaid.cs        # PATCH /api/admin/orders/{id}/payment (Reconcile COD / bank transfer)
├── Infrastructure/                 # Cloudflare R2, SMS/Telegram Gateway, Telegram Bot Webhook
├── Dockerfile                      # Multi-stage .NET 10 build
└── Program.cs                      # DI registration, Auth, RateLimiter, OpenAPI/Scalar, Endpoint mapping
```

---

## 5. Core Domain Data Model (EF Core Entities)

* **`Users`**: `Id` (UUID), `PhoneNumber` (Unique), `FullName`, `Role` (`Farmer`, `Restaurant`, `Driver`, `Admin`), `TelegramChatId` (`long?`), `IsVerified` (`bool`), `CreatedAt` (`timestamptz`).
* **`RefreshTokens`**: `Id` (UUID), `UserId` (FK), `TokenHash`, `ExpiresAt`, `RevokedAt`.
* **`BusinessProfiles`**: `UserId` (FK), `BusinessName`, `Region`, `Address`, `Latitude`, `Longitude`, `PreferredDeliveryWindow` (e.g., `06:00-09:00`), `TaxId` (optional for invoicing).
* **`Products`**: `Id` (UUID), `FarmerId` (FK), `Category`, `Title`, `Description`, `AvailableStockKg` (`decimal`), `MinOrderKg` (`decimal`), `BasePricePerKg` (`decimal`), `Metadata` (`JSONB` — harvest date, grade, packaging), `ImageUrls` (`string[]`), `IsActive`, `IsDeleted` (soft delete), `UpdatedAt`, `xmin` (optimistic concurrency token).
* **`PriceTiers`**: `Id` (UUID), `ProductId` (FK), `MinQuantityKg` (`decimal`), `PricePerKg` (`decimal`) *(e.g., 10–49 kg = $1.50/kg, 50+ kg = $1.25/kg)*.
* **`CheckoutGroups`**: `Id` (UUID), `RestaurantId` (FK), `DeliveryAddress`, `ScheduledDeliveryDate` (`date`), `CutoffAt` (`timestamptz`), `TotalDeliveryFee` (`decimal`), `CreatedAt`. *(Allows one restaurant checkout across multiple farmers to be tracked and invoiced together).*
* **`DeliveryRuns`**: `Id` (UUID), `DriverName`, `DriverPhone`, `VehicleNote`, `RunDate` (`date`), `Status` (`Planned`, `PickingUp`, `InTransit`, `Completed`), `CreatedAt`.
* **`Orders`**: `Id` (UUID), `CheckoutGroupId` (FK), `RestaurantId` (FK), `FarmerId` (FK), `DeliveryRunId` (FK, nullable), `FulfillmentType` (`PlatformCourier`, `FarmerDelivery`, `BuyerPickup`), `Status` (`Pending`, `Confirmed`, `PickedUp`, `InTransit`, `DeliveredPendingApproval`, `Delivered`, `Disputed`, `Cancelled`), `EstimatedTotalAmount`, `PlatformFeeAmount`, `DeliveryFeeAmount`, `FinalTotalAmount`, `PaymentMethod` (`CashOnDelivery`, `BankTransfer`, `WeeklyInvoice`), `PaymentStatus` (`Unpaid`, `Paid`, `Disputed`), `ExpiresAt` (auto-cancel if farmer misses confirmation), `ConfirmedAt`, `DeliveredAt`, `CancelledAt`, `CreatedAt`.
* **`OrderItems`**: `Id` (UUID), `OrderId` (FK), `ProductId` (FK), `ProductTitleSnapshot` (`string`), `UnitPriceApplied` (`decimal` — locked at checkout tier), `EstimatedWeightKg` (`decimal`), `ActualDeliveredWeightKg` (`decimal?`), `RejectedWeightKg` (`decimal?`), `ProofPhotoUrl` (`string?`).

### Key Domain Rules Built into the Model
1. **Tier Lock-In:** `UnitPriceApplied` is locked based on the `EstimatedWeightKg` tier at checkout so a minor packing shortfall (e.g., 48 kg packed on a 50 kg order) does not unexpectedly penalize the restaurant with a higher per-kg tier.
2. **Stock Safety:** `Checkout` deducts `EstimatedWeightKg` inside a DB transaction using `xmin` concurrency. Unconfirmed `Pending` orders automatically release stock after `ExpiresAt`; variance on delivery reconciles back to `AvailableStockKg` (if lower) or caps positive variance within available stock.
3. **Two-Step Variance Guardrail:** If `|ActualDeliveredWeightKg - EstimatedWeightKg| <= 10%` and `RejectedWeightKg == 0`, delivery moves straight to `Delivered`. Larger variances or rejected items enter `DeliveredPendingApproval` for restaurant sign-off.

---

## 6. 4-Week MVP Execution Roadmap (Parallel Vertical Slices)

To avoid a Week 4 frontend bottleneck, backend endpoints and Next.js / Telegram UI are built **together per feature slice** every week:

| Phase | Timeline | End-to-End Deliverables (Backend + Frontend + Ops) |
| :--- | :--- | :--- |
| **Phase 1: Foundation, Auth & Admin** | **Week 1** | Initialize `.NET 10` VSA API + `PostgreSQL 17` + `Docker Compose` + Scalar UI; implement rate-limited Phone OTP + JWT/Refresh Tokens + Telegram Bot deep-link; deploy bare **Next.js PWA** shell + login & Admin user-verification screen to staging. |
| **Phase 2: Supply Catalog & Restaurant Discovery** | **Week 2** | Build Farmer profile & Product CRUD (`PriceTiers`, `JSONB`, soft-delete) + Cloudflare R2 presigned uploads; build Farmer listing UI + Restaurant catalog browsing, filtering, and cart UI with multi-farmer grouping preview. |
| **Phase 3: Ordering, Milk-Run Logistics & Reconciliation** | **Week 3** | Implement transactional `Checkout` (cutoff window + stock lock + order split), `RepeatOrder`, Telegram instant order alerts with farmer `[Accept]`/`[Reject]` actions, Admin `DeliveryRun` assignment UI, and weight/quality reconciliation + COD/bank-transfer invoice view. |
| **Phase 4: Hardening & Pilot Launch** | **Week 4** | Run concurrency/oversell & end-to-end flow tests; configure production VPS (`Docker Compose` + Caddy SSL + separate migration step); onboard first **10 pilot farmers** and **5 restaurants**; run first live morning milk-run deliveries. |

---

## 7. Docker Compose & Deployment Strategy

Using **Docker Compose** keeps local development and production consistent:

* **Local Development (`docker-compose.yml`):** Runs `.NET 10 API`, `postgres:17-alpine` (named volume `pgdata`), and local Next.js dev/preview with automatic EF Core migrations for convenience.
* **Production Deployment (VPS — Hetzner / DigitalOcean):**
  * Run `docker compose -f docker-compose.prod.yml up -d` alongside a lightweight **Caddy** container for automatic HTTPS/SSL certificates.
  * Run `efbundle` / EF Core migrations as an explicit pre-start deploy step (rather than inside `Program.cs` startup) to ensure safe, predictable schema updates.

---

## 8. Post-MVP Roadmap (Phase 2 — After Pilot Traction)
* **Automated Online Payments & Payouts:** Integrate local payment gateway (card/escrow or automated split payouts to farmers within 24 hours of `Delivered`).
* **Supermarket Expansion:** Add formal e-invoicing, Net-15/Net-30 credit limits, and barcode/crate labeling.
* **Driver Mobile View & PostGIS Routing:** Dedicated driver checklist app and automated pickup route sequencing using PostGIS coordinates.
