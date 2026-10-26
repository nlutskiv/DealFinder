# DealFinder

Price comparison site for PC parts, live at [dealfinder.tech](https://dealfinder.tech).

It pulls product feeds from UK retailers through Awin, matches listings to a catalogue of real parts, and ranks them by price and value. It also tracks price history, sends price-drop email alerts, and has a PC builder that puts together a parts list for a given budget.

## Stack

- **Backend:** ASP.NET Core (.NET 10), EF Core, PostgreSQL
- **Frontend:** Angular 21
- **Services:** OpenAI (gpt-4o-mini) for the PC builder, Resend for alert emails
- **Hosting:** API on Railway (Docker), frontend on Vercel

## How it works

- `AwinFeedRefreshJob` downloads the Awin feed list on a schedule, keeps only the allowed advertisers, and ingests their product CSVs into Postgres.
- Listings are classified by category, matched to catalogue products, and scored on query match, price and value (`Services/Scoring`).
- Two daily jobs snapshot prices for the history charts and check alerts against them.
- The PC builder splits the budget across categories and pulls candidate listings for each. The model then picks a compatible set from those candidates within the budget, so every part in a build is a real listing.
- Endpoints are rate limited per IP, with a tighter limit on the AI-backed builder.

## Running locally

You need the .NET 10 SDK, Node 20+ and a Postgres database.

**API**

```bash
cd DealFinder.Api
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Database=dealfinder;Username=postgres;Password=..."
dotnet user-secrets set "OpenAI:ApiKey" "..."
dotnet user-secrets set "Awin:FeedListUrl" "..."
dotnet run
```

Migrations run on startup. The API listens on `http://localhost:5185`.

**Client**

```bash
cd dealfinder-client
npm install
npm start
```

This opens on `http://localhost:4200` and points at the local API (`src/environments/environment.ts`).

## Configuration

| Key | Purpose |
|---|---|
| `ConnectionStrings:DefaultConnection` | Postgres connection string |
| `OpenAI:ApiKey` | Used by the PC builder |
| `Awin:FeedListUrl` | Awin feed list URL (contains your Awin key) |
| `Awin:AllowedAdvertiserIds` / `AllowedAdvertiserNames` | Which retailers to ingest |
| `Awin:SyncIntervalHours`, `RunOnStartup`, `MaxConcurrentFeeds` | Feed job settings |
| `Resend:ApiKey`, `Resend:FromAddress` | Price-alert emails |

In production these are set as environment variables, with `__` in place of `:` (for example `OpenAI__ApiKey`). Keep them out of the repo: `appsettings.Development.json` is gitignored.
