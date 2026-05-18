# Trading313 — Paper-Trading Portfolio App

> Diploma-thesis project: a Trading 212 / Robinhood-style stock portfolio web app. Virtual cash, real market data, no real orders. Stocks-only.

Built with ASP.NET Core 8, React 19 + TypeScript, MySQL 8, and the Twelve Data free tier.

## Contents

- [Feature highlights](#feature-highlights)
- [Tech stack](#tech-stack)
- [Quick start](#quick-start)
- [Demo credentials](#demo-credentials)
- [Configuration](#configuration)
- [Common tasks](#common-tasks)
- [Troubleshooting](#troubleshooting)
- [Repository layout](#repository-layout)
- [Architecture notes](#architecture-notes)
- [License](#license)

## Feature highlights

**Portfolio & trading**
- Market buy / sell with virtual $10 000 starting balance, average-cost basis, realized P/L on sell
- Limit, stop-loss and trailing-stop pending orders (`OrderExecutionService` evaluates every 60 s)
- Cash deposit / withdraw with full transaction history
- Recurring dollar-cost-averaging orders (configurable frequency)
- Position notes & tags; transaction notes & tags with tag-filtered realized P/L

**Market data (Twelve Data)**
- Symbol search, latest quote, daily OHLC history with smart gap-detection caching
- Company profile (sector, industry, market cap, P/E, EPS, dividend yield, beta) — 7-day cache
- Logos, 52-week high/low marker, dividends, stock splits, earnings calendar
- Analyst consensus + price targets (curated seed for popular tickers)
- Insider transactions (curated seed for popular tickers)
- Rate-limited to 8/min and 800/day with a persistent daily counter; falls back to last cached quote with `isStale: true` on denial
- Background `QuoteRefreshService` batches one call per tick for held + watched symbols during US market hours
- **Live WebSocket ticks** via SignalR (`/hub/prices`); subscribed symbols flash green/red on each update

**Analytics**
- Daily portfolio snapshots (recomputed from `Transactions`, not current `Positions` — historically correct)
- Performance line chart with S&P 500 benchmark overlay, range buttons 1M / 3M / 6M / 1Y / 5Y / MAX
- Allocation pie (by symbol) + sector allocation pie + per-symbol returns bar chart
- **Risk metrics** card: portfolio beta, annualized volatility, Sharpe ratio, max drawdown
- **Advanced metrics** card: TWR, MWR (IRR via Newton-Raphson), Sortino ratio, best / worst day, win rate
- Diversification score (0–100) from position count + concentration + sector entropy, with suggestions
- Tax report per year: FIFO short / long-term gains, dividends received, fees; CSV download + print-to-PDF

**UX**
- Light / dark / system theme with Bootstrap 5 `data-bs-theme`
- English + Bulgarian (`react-i18next`)
- Collapsible sidebar (72 px collapsed → 240 px expanded), persisted to `localStorage`
- Breadcrumbs on every page, deep-linkable tabs via `?tab=` / `?list=`
- **Cmd-K command palette** with symbol search, navigation, theme + language switching
- Mini sparklines on holdings + watchlist rows
- Print-to-PDF tax report
- Stock comparison page (normalized-to-100 multi-line chart for 2–4 symbols)
- Watchlists (multiple named lists), price alerts, achievements badges, portfolio goals

**Admin & ops**
- User management (search, promote / demote, enable / disable)
- API usage panel with today / last-hour Twelve Data quota and recent calls
- Manual "Run snapshots now" trigger
- **Weekly email digest** of trades + P/L (in-app viewer always; SMTP send if configured)

## Tech stack

| Layer | Tech | Version |
|---|---|---|
| Backend | ASP.NET Core Web API · C# | net8.0 |
| ORM | Entity Framework Core | 8.0.10 |
| MySQL provider | Pomelo.EntityFrameworkCore.MySql | 8.0.2 |
| Database | MySQL via Docker | 8.4 |
| Auth | ASP.NET Identity + JWT bearer (HMAC-SHA256, 4 h tokens) | 8.0.10 |
| Real-time | SignalR (WebSockets) | built-in |
| API docs | Swashbuckle (Swagger / OpenAPI) | 6.6 |
| Resilience | Polly via `Microsoft.Extensions.Http.Polly` | 8.0.10 |
| Rate limiting | `Microsoft.AspNetCore.RateLimiting` | built-in |
| Frontend | React + TypeScript + Vite | React 19 · Vite 8 |
| UI | Bootstrap 5 + react-bootstrap | 5.3 / 2.10 |
| Icons | Flaticon UICONS (`@flaticon/flaticon-uicons`) | 3.3 |
| Charts | Recharts | 3.8 |
| Server state | TanStack Query | 5.x |
| Forms | React Hook Form + Zod | 7.x / 4.x |
| i18n | react-i18next + i18next-browser-languagedetector | 14.x / 8.x |
| Realtime client | `@microsoft/signalr` | 8.x |
| External API | Twelve Data (free tier: 8/min, 800/day) | — |

## Quick start

End-to-end on a fresh Linux machine in under 15 minutes.

### Prerequisites

| Tool | How to verify | Install (Ubuntu / Mint) |
|---|---|---|
| .NET 8 SDK | `dotnet --version` → `8.x` | `sudo apt install -y dotnet-sdk-8.0` |
| Node.js 20+ | `node --version` | `curl -fsSL https://deb.nodesource.com/setup_20.x \| sudo -E bash -` then `sudo apt install -y nodejs` |
| Docker + compose | `docker compose version` | follow https://docs.docker.com/engine/install/ |
| `dotnet-ef` | `dotnet ef --version` | `dotnet tool install --global dotnet-ef` (add `~/.dotnet/tools` to `$PATH`) |
| Twelve Data API key | sign up (free, no card) at https://twelvedata.com/ | — |

### 1 — MySQL via Docker

```bash
cd docker
cp .env.example .env           # ports default to 3310 / 8081 to dodge a system MySQL on 3306
docker compose up -d
```

Verify:

```bash
docker compose ps              # mysql healthy, adminer up
```

- MySQL on `localhost:3310` (user `trading212`, password from `docker/.env`, db `trading212_dev`)
- Adminer (web DB browser) on http://localhost:8081

### 2 — Backend secrets

```bash
cd ../backend/Trading313.Api
dotnet user-secrets set "TwelveData:ApiKey" "YOUR_TWELVEDATA_KEY"
dotnet user-secrets set "Jwt:Key" "$(openssl rand -base64 48)"
```

`appsettings.Development.json` already has a placeholder JWT key that works for local play — `user-secrets` simply overrides it without committing anything sensitive.

### 3 — Apply migrations + start backend

```bash
dotnet ef database update      # creates ~30 tables
dotnet run                     # launches on http://localhost:7000 (Development profile)
```

You should see Swagger at http://localhost:7000/swagger and `GET /health` returning `{ "status": "ok" }`.

The first run auto-seeds the default admin (`admin@trading212.local` / `Admin1234`).

### 4 — (Optional) Seed demo data

Three demo users with backdated transactions across 8 popular symbols + backfilled daily snapshots:

```bash
dotnet run -- seed
```

Creates `demo1@trading212.local`, `demo2@trading212.local`, `demo3@trading212.local` — password `Demo1234` on all three.

For a richer demo (260 transactions, full year of price history for charts, hardcoded sector data for the pie), also run:

```bash
cd ../../scripts
python3 seed_sectors.py        # hardcodes sectors for 13 popular tickers
python3 seed_spy_history.py    # bulk-loads SPY daily bars for benchmark line
python3 seed_demo1_big.py      # 260 transactions for demo1
```

These Python scripts bypass the in-app rate limiter and hit Twelve Data / MySQL directly. They take a Twelve Data key from `~/.microsoft/usersecrets/3e8ff94c-29f5-42eb-a233-ba30b45af4a3/secrets.json` (set in step 2) or from the `TWELVE_DATA_KEY` env var.

### 5 — Frontend

In a second terminal:

```bash
cd frontend
cp .env.example .env           # default backend URL is http://localhost:7000
npm install
npm run dev
```

Open http://localhost:5174. You should see a green **API: ok** badge in the sidebar. Log in as `demo1` to see the seeded portfolio.

### Verifying everything works

| Check | Expected |
|---|---|
| `curl http://localhost:7000/health` | `{"status":"ok",...}` |
| http://localhost:7000/swagger | Swagger UI loads, "Authorize" button visible |
| http://localhost:5174 | App loads, sidebar shows "API: ok" |
| Log in as `demo1` → Dashboard | Cash balance, top holdings, mini chart, popular stocks |
| `/analytics` | Performance chart with green line; sector pie populated; advanced metrics card with TWR / MWR |
| `/stocks/AAPL` | Header with price + 52-week bar; analyst consensus card; insider trades card |
| Press Cmd-K (or Ctrl-K) | Command palette opens, type "AAPL" → symbol jumps to top |

## Demo credentials

| Email | Password | Role | Notes |
|---|---|---|---|
| `admin@trading212.local` | `Admin1234` | Admin | Auto-seeded on first backend run |
| `demo1@trading212.local` | `Demo1234` | User | After `dotnet run -- seed` |
| `demo2@trading212.local` | `Demo1234` | User | After `dotnet run -- seed` |
| `demo3@trading212.local` | `Demo1234` | User | After `dotnet run -- seed` |

Note: the demo emails still use the `@trading212.local` domain (the database, connection string, and `localStorage` keys keep the `trading212` namespace for backward compatibility — only the *code identifiers* and product name were renamed to `Trading313`).

## Configuration

### Backend (`appsettings.json` + `appsettings.Development.json` + user-secrets)

| Key | Default (Dev) | Purpose |
|---|---|---|
| `ConnectionStrings:Default` | `Server=localhost;Port=3310;Database=trading212_dev;Uid=trading212;Pwd=trading212pass;` | MySQL connection |
| `Jwt:Key` | placeholder (override via `user-secrets`) | HMAC-SHA256 signing key, 32+ chars |
| `Jwt:Issuer` | `Trading313.Api.Dev` | JWT `iss` claim |
| `Jwt:Audience` | `Trading313.Web.Dev` | JWT `aud` claim |
| `Jwt:AccessTokenLifetimeMinutes` | `240` | Token TTL |
| `TwelveData:ApiKey` | **required** (set via `user-secrets`) | API key |
| `TwelveData:BaseUrl` | `https://api.twelvedata.com` | Endpoint |
| `TwelveData:RequestsPerMinute` | `8` | Per-minute ceiling |
| `TwelveData:RequestsPerDay` | `800` | Daily ceiling (persists across restarts via `ApiUsageLog`) |
| `Cors:AllowedOrigins` | `["http://localhost:5173","http://localhost:5174"]` | CORS allow-list |
| `Seed:Enabled` | `true` (Dev) / `false` (Prod) | Master switch for default-admin + demo seeders |
| `Seed:DefaultAdminEmail` | `admin@trading212.local` | Default admin email |
| `Seed:DefaultAdminPassword` | `Admin1234` | Default admin password |
| `App:AuthorName` / `App:AuthorEmail` | (empty) | Shown in Swagger info block |
| `Smtp:Host`, `Smtp:Port`, `Smtp:Username`, `Smtp:Password`, `Smtp:FromAddress`, `Smtp:EnableSsl` | (empty) | Optional — when configured, weekly digests are also emailed. If empty, digests are still generated and visible in-app at `/digests`. |

### Frontend (`frontend/.env`)

| Key | Default | Purpose |
|---|---|---|
| `VITE_API_BASE_URL` | `http://localhost:7000` | Backend base URL (also used by SignalR via the Vite `/hub` proxy) |
| `VITE_APP_AUTHOR_NAME` | `Your Name` | Footer credit |
| `VITE_APP_AUTHOR_EMAIL` | `you@example.com` | Footer email |
| `VITE_APP_COPYRIGHT_YEAR` | `2026` | Footer year |

### Docker (`docker/.env`)

| Key | Default | Purpose |
|---|---|---|
| `MYSQL_ROOT_PASSWORD` | `rootpass` | Root password |
| `MYSQL_DATABASE` | `trading212_dev` | Schema name |
| `MYSQL_USER` | `trading212` | App user |
| `MYSQL_PASSWORD` | `trading212pass` | App user password |
| `MYSQL_PORT` | `3310` | Host port — picked to dodge a system MySQL on 3306 |
| `ADMINER_PORT` | `8081` | Adminer port |

## Common tasks

```bash
# Run backend tests
cd backend && dotnet test

# Reset DB from scratch (destroys data)
cd docker && docker compose down -v && docker compose up -d
cd ../backend/Trading313.Api && dotnet ef database update && dotnet run -- seed

# Create a new migration
cd backend/Trading313.Api
dotnet ef migrations add MyMigrationName
dotnet ef database update

# Run backend on a different port
ASPNETCORE_URLS=http://localhost:5128 dotnet run --no-launch-profile
# (then update frontend/.env: VITE_API_BASE_URL=http://localhost:5128)

# Open the API in Swagger
xdg-open http://localhost:7000/swagger

# Stream backend logs
tail -f /tmp/trading313-api.log    # if launched in background; or watch the terminal
```

## Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| `dotnet ef` "command not found" | EF global tool isn't on `$PATH` | `export PATH="$PATH:$HOME/.dotnet/tools"` (add to `.bashrc` to persist) |
| Migration fails: "Unable to connect" | MySQL container not up yet | `docker compose ps` → wait for `healthy`; check `MYSQL_PORT` in `docker/.env` |
| Frontend "Network Error" / red API badge | Backend not running or wrong port | `curl http://localhost:7000/health`; check `VITE_API_BASE_URL` in `frontend/.env`; restart Vite after `.env` changes |
| 401 on every request after rename | Old JWT in `localStorage` issued with old issuer | Log out and back in |
| Sector pie all "Unknown" / SPY benchmark line missing | Rate-limited on profile / SPY fetch | Run `python3 scripts/seed_sectors.py` and `python3 scripts/seed_spy_history.py` |
| "Twelve Data rate limit exceeded" toast | Free-tier 8/min hit | Wait a minute; the rate limiter serves last cached values with `isStale: true` automatically |
| Login fails with "Account is disabled" | Identity lockout (5 bad attempts) | Wait 15 min or unlock via Adminer (`AspNetUsers.LockoutEnd`) |
| `dotnet user-secrets set` errors | First run only; needs initialization | Already initialized for this project (`UserSecretsId` in csproj). If you see "no secrets file" run `dotnet user-secrets init` once. |
| WebSocket `/hub/prices` 401 in console | JWT not picked up by SignalR | Token is passed via `?access_token=…` on negotiate. Re-login if expired. |
| Port 3310 already in use | Another MySQL on same port | Edit `MYSQL_PORT` in `docker/.env`, then update the backend `ConnectionStrings:Default` (or override via `user-secrets`) |

## Repository layout

```
/
├── backend/
│   ├── Trading313.sln
│   ├── Trading313.Api/
│   │   ├── Controllers/          Auth, Users, Stocks, Portfolio, Watchlist, Analytics, Admin/*, Digests
│   │   ├── Services/             Auth, Users, Stocks, MarketData, Portfolio, Watchlist, Analytics,
│   │   │                          Admin, Orders, RecurringOrders, Dividends, Goals, Digests
│   │   ├── Realtime/             SignalR PriceHub + PricePublisher
│   │   ├── Background/           IHostedService: QuoteRefresh, DailySnapshot, OrderExecution,
│   │   │                          AlertEvaluation, RecurringOrder, EmailDigest
│   │   ├── Domain/Entities/      ApplicationUser, Stock, Transaction, Position, WatchlistItem,
│   │   │                          DailyPortfolioSnapshot, EarningsEntry, DividendEvent, StockSplit,
│   │   │                          PendingOrder, CashTransaction, PriceAlert, RecurringOrder, Goal,
│   │   │                          AnalystRating, InsiderTrade, EmailDigest, ApiUsageLog
│   │   ├── Domain/Enums/         TransactionType, OrderSide, OrderStatus, …
│   │   ├── Data/                 AppDbContext + Configurations + Migrations
│   │   ├── Infrastructure/       Auth/JwtTokenService, MarketData/TwelveDataClient + RateLimiter,
│   │   │                          Seeding/IdentitySeeder + DemoDataSeeder
│   │   └── Dtos/                 Request/response DTOs grouped by feature
│   └── Trading313.Tests/         xUnit tests (PortfolioService + SnapshotService)
├── frontend/
│   └── src/
│       ├── pages/                Login, Register, Dashboard, Portfolio, StockDetail, Stocks,
│       │                          Watchlist, Orders, Compare, Analytics, Profile, TaxReport,
│       │                          Digests, Admin, NotFound
│       ├── components/           Sidebar, Breadcrumbs, ConfirmDialog, Button, CommandPalette,
│       │                          LivePrice, PreferencesCard, …
│       │   ├── forms/            TextField, PasswordField, NumberField, SelectField
│       │   ├── dashboard/        SummaryCards, MiniPerformanceChart, Hotlist, EarningsCalendar, …
│       │   ├── portfolio/        HoldingsTable, TransactionTable, EditPositionModal,
│       │   │                      EditTransactionModal, TagPlSummaryCard
│       │   ├── stocks/           StockHeader, PriceChart, CompanyProfileCard, FiftyTwoWeekRange,
│       │   │                      AnalystConsensusCard, InsiderTradesCard, SplitHistoryCard,
│       │   │                      DividendHistoryCard, YourPositionCard
│       │   ├── analytics/        PerformanceChart, AllocationPie, SectorPie, ReturnsBarChart,
│       │   │                      RiskMetricsCard, DiversificationCard, AdvancedMetricsCard
│       │   ├── trade/            TradeModal, PlaceOrderModal, CreateAlertModal
│       │   └── admin/            UsersTable, SystemPanel
│       ├── api/                  Axios client + TanStack Query hooks per feature, livePrices (SignalR)
│       ├── auth/                 AuthContext, useAuth, RequireAuth, RequireRole, tokenStorage
│       ├── theme/                ThemeContext (light/dark/system)
│       ├── layouts/              AppLayout (sidebar + breadcrumbs + outlet + footer)
│       ├── hooks/                useDebounce, usePagedData, usePersistedNumber, useDocumentTitle, …
│       ├── lib/                  format, toast, download, indicators
│       └── locales/              en.json + bg.json
├── docker/                       docker-compose: MySQL 8 + Adminer
├── scripts/                      Python helpers: seed_sectors, seed_spy_history, seed_demo1_big
├── docs/
│   ├── architecture.md
│   ├── security.md
│   ├── thesis-changes.md
│   └── diagrams/                 architecture.md + erd.md (Mermaid)
├── PLAN.md                       Original 11-epic implementation plan
├── IMPROVEMENTS.md               Round 1–5 feature batches (all shipped)
└── README.md                     This file
```

## Architecture notes

**Backend layout.** Single ASP.NET Core 8 project with folder layers (`Controllers / Services / Domain / Data / Dtos / Infrastructure / Background / Realtime`). Not Clean Architecture with multiple sub-projects — one bounded context, one assembly, easier to navigate during a defense demo.

**Twelve Data caching — three tiers.** `IMemoryCache` (60 s hot path) → MySQL `PriceCache` (60 s freshness window, survives restarts) → upstream Twelve Data. `HistoricalPrices` is cached forever (past closes are immutable). `QuoteRefreshService` batches one Twelve Data call per minute over the union of all held and watched symbols. `TokenBucketRateLimiter` enforces 8/min and 800/day; on denial the API returns the last cached quote with `isStale: true` rather than 503.

**Position math.** Materialized `Positions` table updated atomically with each `Transactions` insert. Buy: `new_avg = (qty × avg + buy_qty × buy_price + fees) / (qty + buy_qty)`. Sell: `quantity` is reduced, `AverageCost` is unchanged for remaining shares, `realized_pl = (sell_price − avg_cost) × sell_qty − fees` is stored on the transaction. `Quantity = 0` triggers `IsClosed = true` (preserved for history).

**Daily snapshots.** `DailySnapshotService` runs at 23:00 UTC. On first Analytics visit per user, `SnapshotBackfillService` replays transactions from the earliest one to today using cached `HistoricalPrices`. Snapshots are always recomputed from `Transactions`, never the current `Positions` table — positions you held on day D and later sold must still appear in D's snapshot.

**Auth.** ASP.NET Identity + JWT bearer (HMAC-SHA256, 4 h tokens). No refresh tokens in v1 (documented trade-off in `docs/security.md`). JWT is sent via `Authorization: Bearer …` for REST and via `?access_token=…` query parameter for the SignalR `/hub/prices` WebSocket (the browser WebSocket API can't set custom headers).

**Real-time.** SignalR hub at `/hub/prices` with per-connection symbol subscriptions. `PricePublisher` is called from `QuoteRefreshService` after each refresh tick; it fans the new quotes out to subscribed clients via `priceUpdate` events. Frontend `useLivePrices(symbols)` hook shares a single connection across components with refcounted subscriptions and exposes the most recent quote per symbol; the `<LivePrice>` component flashes green / red for 600 ms on each tick.

**i18n.** `react-i18next` with English + Bulgarian bundles in `src/locales/*.json`. Language preference persisted via `i18next-browser-languagedetector`.

More detail: [`docs/architecture.md`](docs/architecture.md) (component, sequence, deployment diagrams), [`docs/diagrams/erd.md`](docs/diagrams/erd.md) (ER), [`docs/security.md`](docs/security.md) (auth, rate limiting, headers, secrets, known limitations).

## License

MIT — see [`LICENSE`](LICENSE).
