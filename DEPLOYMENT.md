# Deploying to Azure App Service

This guide covers deploying World Cup Formations (Blazor Server, .NET 10, SQLite) to
Azure App Service using your Azure for Students subscription.

---

## Why App Service, not Static Web Apps or Functions

| Azure service | Blazor Server? | Why |
|---|---|---|
| **App Service** | ✅ Works | Runs a persistent .NET process; supports SignalR WebSockets |
| Static Web Apps | ❌ No | Only hosts static files or Blazor WebAssembly |
| Azure Functions | ❌ No | Serverless — no persistent connections, no SignalR |
| Container Apps | ✅ Works | Docker-based alternative; more setup required |

Blazor Server relies on a long-lived SignalR connection between the browser and the
server. App Service keeps the process running, which is exactly what it needs.

---

## Prerequisites

| Tool | Install |
|---|---|
| Azure CLI | `winget install Microsoft.AzureCLI` · [aka.ms/installazurecliwindows](https://aka.ms/installazurecliwindows) |
| .NET 10 SDK | `winget install Microsoft.DotNet.SDK.10` · [dot.net/download](https://dotnet.microsoft.com/download) |
| Git | Already installed if you can push this repo |

Verify:

```bash
az --version
dotnet --version   # should print 10.x.x
```

---

## Part 1 — Azure Setup (one-time)

### 1.1 Log in

```bash
az login
```

A browser window opens. Sign in with your student account
(the one that has Azure for Students credits).

### 1.2 Confirm your subscription

```bash
az account show --query "{name:name, id:id}" -o table
```

If you have multiple subscriptions, select the student one:

```bash
az account set --subscription "<subscription-id-or-name>"
```

### 1.3 Create a Resource Group

A resource group is a logical container for all your Azure resources.
Pick a region close to your users (`westeurope`, `eastus`, `japaneast`, etc.).

```bash
az group create \
  --name rg-worldcup \
  --location westeurope
```

### 1.4 Create an App Service Plan

The plan defines the hardware tier. Start with **B1 Basic** (1 core, 1.75 GB RAM,
~$13/month — well within student credits). B1 enables "Always On" so the app
doesn't sleep on idle traffic.

```bash
az appservice plan create \
  --name plan-worldcup \
  --resource-group rg-worldcup \
  --sku B1 \
  --is-linux
```

> **Free tier (F1)**: If you want zero cost, replace `--sku B1` with `--sku F1`.
> F1 has a 60-CPU-minute/day limit, no Always On (app sleeps after 20 min idle),
> and slower cold starts. Fine for occasional demos; not great for a portfolio
> that needs to impress at a moment's notice.

### 1.5 Create the Web App

Pick a globally unique name — it becomes `<name>.azurewebsites.net`.

```bash
az webapp create \
  --name worldcup-formations \
  --resource-group rg-worldcup \
  --plan plan-worldcup \
  --runtime "DOTNETCORE:10.0"
```

> If `DOTNETCORE:10.0` isn't listed run `az webapp list-runtimes --os linux` to see
> current options. Use the closest available (e.g. `DOTNETCORE:9.0`).

### 1.6 Set the database path environment variable

This puts the SQLite database in `/home/data/` — a directory that persists across
redeployments on App Service Linux (backed by Azure Files). Without this, the database
lands inside the deployed package directory and gets wiped on every new deploy, forcing
a full reseed each time (~2–3 s, which is acceptable, but avoidable).

```bash
az webapp config appsettings set \
  --name worldcup-formations \
  --resource-group rg-worldcup \
  --settings DB_PATH="/home/data/worldcup.db"
```

### 1.7 Enable Always On (B1 and above only)

```bash
az webapp config set \
  --name worldcup-formations \
  --resource-group rg-worldcup \
  --always-on true
```

---

## Part 2A — Deploy manually (fastest first deploy)

Use this when you want to push a one-off build without setting up GitHub Actions yet.

### 2A.1 Publish the app locally

```bash
dotnet publish src/WorldCupFormations.Web/WorldCupFormations.Web.csproj \
  --configuration Release \
  --output ./publish
```

### 2A.2 Zip the published output

**Linux / macOS:**
```bash
cd publish && zip -r ../deploy.zip . && cd ..
```

**Windows (PowerShell):**
```powershell
Compress-Archive -Path publish\* -DestinationPath deploy.zip -Force
```

### 2A.3 Upload and deploy

```bash
az webapp deploy \
  --name worldcup-formations \
  --resource-group rg-worldcup \
  --src-path deploy.zip \
  --type zip
```

Deployment takes about 60–90 seconds. When it completes:

```bash
az webapp browse --name worldcup-formations --resource-group rg-worldcup
```

Your browser should open `https://worldcup-formations.azurewebsites.net`.
The first request takes 5–15 seconds while the app starts and seeds the database.

---

## Part 2B — Automated deployment via GitHub Actions (recommended)

Set this up once; every push to `master` deploys automatically.

### 2B.1 Download the publish profile

```bash
az webapp deployment list-publishing-profiles \
  --name worldcup-formations \
  --resource-group rg-worldcup \
  --xml \
  --output tsv > publish-profile.xml
```

Open `publish-profile.xml` and copy its **entire contents** (it's XML starting with
`<publishData>`).

> Delete this file locally after copying — it contains credentials.
> `rm publish-profile.xml`

### 2B.2 Add secrets to your GitHub repository

Go to your repo on GitHub:
**Settings → Secrets and variables → Actions → New repository secret**

| Secret name | Value |
|---|---|
| `AZURE_WEBAPP_NAME` | `worldcup-formations` (the name you chose in 1.5) |
| `AZURE_PUBLISH_PROFILE` | The full XML content you copied above |

### 2B.3 The workflow file is already in the repo

`.github/workflows/azure-deploy.yml` was added alongside this guide. It:

1. Runs on every push to `master` (and on manual trigger)
2. Sets up .NET 10
3. Runs `dotnet publish --configuration Release`
4. Deploys to App Service using the publish profile

Push to `master` to trigger the first automated deploy:

```bash
git add .
git commit -m "add Azure deployment workflow"
git push origin master
```

Watch it run under **Actions** in your GitHub repo. Green checkmark = live.

---

## Part 3 — Verify the live app

```bash
# Check the app is responding
curl -I https://worldcup-formations.azurewebsites.net

# Stream live logs (useful for debugging startup)
az webapp log tail \
  --name worldcup-formations \
  --resource-group rg-worldcup
```

The startup log should show EF Core applying migrations and then the seed insert.
Subsequent restarts skip seeding (database already exists at `/home/data/worldcup.db`).

---

## Useful operations

### Force a database reseed

The database is seeded once on first startup. If you update the JSON seed files and
want to re-import the data, delete the database and restart the app:

```bash
# Open an SSH session into the container
az webapp ssh --name worldcup-formations --resource-group rg-worldcup

# Inside the container:
rm /home/data/worldcup.db
exit

# Restart the app to trigger a fresh seed
az webapp restart --name worldcup-formations --resource-group rg-worldcup
```

### View environment variables

```bash
az webapp config appsettings list \
  --name worldcup-formations \
  --resource-group rg-worldcup \
  --output table
```

### Scale up the plan (if the app feels slow)

```bash
az appservice plan update \
  --name plan-worldcup \
  --resource-group rg-worldcup \
  --sku B2   # 2 cores, 3.5 GB RAM
```

### Add a custom domain

1. Buy or transfer a domain (Namecheap, Cloudflare, etc.)
2. Add a CNAME record: `www` → `worldcup-formations.azurewebsites.net`
3. In Azure:

```bash
az webapp config hostname add \
  --webapp-name worldcup-formations \
  --resource-group rg-worldcup \
  --hostname www.yourdomain.com
```

4. Add a free managed TLS certificate:

```bash
az webapp config ssl create \
  --name worldcup-formations \
  --resource-group rg-worldcup \
  --hostname www.yourdomain.com
```

---

## Tiers and cost on Azure for Students

Azure for Students gives **$100 USD in free credits** and renews annually.

| Tier | vCPU | RAM | Cost/month | Notes |
|---|---|---|---|---|
| **F1 Free** | shared | 1 GB | $0 | 60 CPU-min/day cap, no Always On, no custom domain SSL |
| **B1 Basic** | 1 | 1.75 GB | ~$13 | Recommended. Always On, custom domain + SSL |
| **B2 Basic** | 2 | 3.5 GB | ~$26 | Use if B1 feels sluggish under load |

At B1, $100 of student credit covers ~7.5 months. The app is read-only and
single-threaded by nature; B1 is more than sufficient.

**Estimated total cost for a year** (B1 × 12 months ≈ $156):
- Year 1: covered by student credit ($100) + ~$56 out of pocket, or top up with a
  second student subscription year
- After student subscription: ~$13/month ongoing

To check your remaining credit balance:

```bash
az consumption budget list 2>/dev/null || true
# Or visit: https://www.microsoftazuresponsorships.com/Balance
```

---

## Troubleshooting

### "Application Error" on first visit

The app is still seeding the database. Wait 10–20 seconds and refresh. If it
persists, check the log stream:

```bash
az webapp log tail --name worldcup-formations --resource-group rg-worldcup
```

### "address already in use" locally

A previous `dotnet run` process is still holding the port.

```bash
fuser -k 5292/tcp    # Linux
# or
netstat -ano | findstr :5292   # Windows — then: taskkill /PID <pid> /F
```

### Startup crashes with a migration error

The database file may be corrupt or from a different migration baseline. Delete it and
let it reseed:

```bash
az webapp ssh --name worldcup-formations --resource-group rg-worldcup
rm /home/data/worldcup.db
exit
az webapp restart --name worldcup-formations --resource-group rg-worldcup
```

### GitHub Actions deploy fails at "Set up .NET"

The `10.0.x` version string may not yet be available on GitHub-hosted runners.
Change `DOTNET_VERSION` in `.github/workflows/azure-deploy.yml` to `9.0.x` and
also update the `--runtime` in the App Service to match.

### App sleeps and takes 30+ seconds to respond (F1 tier)

App Service Free tier has no Always On. Either:
- Upgrade to B1 (`az appservice plan update --sku B1`)
- Or use a free uptime monitor (UptimeRobot, Better Uptime) to ping the app every
  5 minutes to keep it warm

---

## Tear everything down

When you no longer need the deployment, delete the entire resource group —
this removes the App Service, plan, and all associated resources in one command:

```bash
az group delete --name rg-worldcup --yes --no-wait
```
