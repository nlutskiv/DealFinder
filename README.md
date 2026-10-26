# DealFinder

Price comparison site for PC parts. It ran publicly at dealfinder.tech and is now offline; everything needed to run it locally is below.

It pulls product feeds from UK retailers through Awin, matches listings to a catalogue of real parts, and ranks them by price and value. It also tracks price history, sends price-drop email alerts, and has a PC builder that puts together a parts list for a given budget.

## Stack

- **Backend:** ASP.NET Core (.NET 10), EF Core, PostgreSQL
- **Frontend:** Angular 21
- **Services:** OpenAI (gpt-4o-mini) for the PC builder, Resend for alert emails
- **Hosting:** API on Railway (Docker), frontend on Vercel

## How it works

**Ingestion** (`Services/Feed`). A background job downloads the Awin feed list on a schedule and streams each allowed retailer's product CSV into Postgres.

Every retailer files products differently, so each row goes through three checks before it's kept:
1. Its category path has to match one of eight part types (GPU, CPU, motherboard, RAM, storage, PSU, cooler, case).
2. Its title has to carry a real model signal, such as a GPU chip name or a PSU wattage.
3. It can't contain a reject word, so laptops, prebuilt PCs, cables and accessories are dropped.

Kept rows are then normalised to a canonical model label (`RTX 4070 Super`, `Ryzen 7 7800X3D`, `2TB NVMe`). That label is what groups the same part from different retailers into one catalogue product. Condition (new, used, refurbished) is read from the title as well as the condition field, because some refurb sellers mark everything as new. Inserts and updates are batched.

**Rankings** (`Services/Rankings`). Each catalogue product takes its cheapest price including shipping. GPUs and CPUs get a value score of benchmark index per pound, and storage, RAM and PSUs use capacity or wattage per pound. Categories with no single metric (motherboards, cases, coolers) are ranked by price. Marketplace and refurb sellers are flagged separately from trusted retailers.

**History and alerts.** Two daily jobs snapshot the lowest price per product for the history charts, and email users when a tracked part falls to their target price or lower.

**PC builder.** The budget is split across part types, and candidate listings near each share are pulled from the database. gpt-4o-mini then picks a compatible set from those candidates that fits the budget, and each pick is checked against the candidate IDs. Every part in a build is therefore a real, buyable listing.

All endpoints are rate limited per IP, with a tighter limit on the builder.

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
