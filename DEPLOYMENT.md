# Deploying to Azure App Service with OpenTofu

This guide provisions World Cup Formations (Blazor Server, .NET 10, SQLite) on
Azure App Service using **OpenTofu** (open-source Terraform fork) and your Azure
for Students subscription.

---

## Why App Service, not Static Web Apps or Functions

| Azure service | Blazor Server? | Why |
|---|---|---|
| **App Service** | ✅ Works | Runs a persistent .NET process; supports SignalR WebSockets |
| Static Web Apps | ❌ No | Only hosts static files or Blazor WebAssembly |
| Azure Functions | ❌ No | Serverless — no persistent connections, no SignalR |
| Container Apps | ✅ Works | Docker-based alternative; more setup required |

Blazor Server relies on a long-lived SignalR connection. App Service keeps the
process running continuously, which is what it needs.

---

## Prerequisites

| Tool | Install |
|---|---|
| Azure CLI | [aka.ms/installazurecliwindows](https://aka.ms/installazurecliwindows) or `winget install Microsoft.AzureCLI` |
| OpenTofu | [opentofu.org/docs/intro/install](https://opentofu.org/docs/intro/install/) |
| .NET 10 SDK | [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download) |

Verify:

```bash
az --version
tofu --version    # should print OpenTofu v1.8.x or later
dotnet --version  # should print 10.x.x
```

---

## Part 1 — Azure login

```bash
az login
```

A browser window opens. Sign in with your student account.

Confirm you are on the right subscription:

```bash
az account show --query "{name:name, id:id}" -o table
```

If you have multiple subscriptions:

```bash
az account set --subscription "<subscription-id-or-name>"
```

Note your **subscription ID** — you will need it in the next step.

---

## Part 2 — Configure OpenTofu variables

The infrastructure lives in `infra/`. Copy the example vars file:

```bash
cp infra/terraform.tfvars.example infra/terraform.tfvars
```

Open `infra/terraform.tfvars` and fill in your values:

```hcl
subscription_id     = "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"   # from az account show
location            = "westeurope"     # or eastus, japaneast, etc.
resource_group_name = "rg-worldcup"
plan_name           = "plan-worldcup"
webapp_name         = "worldcup-formations"   # must be globally unique
sku_name            = "B1"                    # see tier table below
```

> `terraform.tfvars` is in `.gitignore` — it will not be committed.

### Tier guide

| SKU | vCPU | RAM | Cost/month | Notes |
|---|---|---|---|---|
| `F1` | shared | 1 GB | $0 | 60 CPU-min/day cap; no Always On; app sleeps after 20 min idle |
| `B1` | 1 | 1.75 GB | ~$13 | **Recommended.** Always On, custom domain + SSL |
| `B2` | 2 | 3.5 GB | ~$26 | Use if B1 feels sluggish under load |

At B1, $100 of student credit covers ~7.5 months.

---

## Part 3 — Provision infrastructure with OpenTofu

```bash
cd infra

# Download the azurerm provider
tofu init

# Preview what will be created (nothing is changed yet)
tofu plan

# Create the resources (~2 minutes)
tofu apply
```

Type `yes` when prompted. When it completes you will see:

```
Outputs:
  webapp_name = "worldcup-formations"
  webapp_url  = "https://worldcup-formations.azurewebsites.net"
  resource_group_name = "rg-worldcup"
```

The App Service is now running and waiting for your application code.

---

## Part 4A — Deploy manually (fastest first deploy)

Use this when you want to push a one-off build without GitHub Actions.

### 4A.1 Publish the app

```bash
cd ..   # back to repo root

dotnet publish src/WorldCupFormations.Web/WorldCupFormations.Web.csproj \
  --configuration Release \
  --output ./publish
```

### 4A.2 Zip the published output

**Linux / macOS:**
```bash
cd publish && zip -r ../deploy.zip . && cd ..
```

**Windows (PowerShell):**
```powershell
Compress-Archive -Path publish\* -DestinationPath deploy.zip -Force
```

### 4A.3 Upload and deploy

```bash
az webapp deploy \
  --name worldcup-formations \
  --resource-group rg-worldcup \
  --src-path deploy.zip \
  --type zip
```

Deployment takes ~60–90 seconds. Open the app:

```bash
az webapp browse --name worldcup-formations --resource-group rg-worldcup
```

The first request takes 5–15 seconds while the app seeds the database.
Subsequent cold starts skip seeding (the database already exists at
`/home/data/worldcup.db`).

---

## Part 4B — Automated deployment via GitHub Actions (recommended)

Set this up once; every push to `master` deploys automatically.

### 4B.1 Download the publish profile

```bash
az webapp deployment list-publishing-profiles \
  --name worldcup-formations \
  --resource-group rg-worldcup \
  --xml \
  --output tsv > publish-profile.xml
```

Open `publish-profile.xml` and copy its **entire contents** (XML starting
with `<publishData>`).

> Delete the file after copying — it contains credentials.
> `rm publish-profile.xml`

### 4B.2 Add secrets to your GitHub repository

**Settings → Secrets and variables → Actions → New repository secret**

| Secret name | Value |
|---|---|
| `AZURE_WEBAPP_NAME` | `worldcup-formations` (the name you set in `terraform.tfvars`) |
| `AZURE_PUBLISH_PROFILE` | The full XML content you copied above |

### 4B.3 The workflow file is already in the repo

`.github/workflows/azure-deploy.yml` runs on every push to `master`:

1. Sets up .NET 10
2. Runs `dotnet publish --configuration Release`
3. Deploys to App Service using the publish profile

Trigger your first automated deploy:

```bash
git push origin master
```

Watch it run under **Actions** in your GitHub repo. Green checkmark = live.

---

## Part 5 — Verify the live app

```bash
# Check the app is responding
curl -I https://worldcup-formations.azurewebsites.net

# Stream live logs (useful for debugging startup)
az webapp log tail \
  --name worldcup-formations \
  --resource-group rg-worldcup
```

The startup log shows EF Core applying migrations, then the seed insert.

---

## Useful operations

### Force a database reseed

If you update seed data and want a fresh import:

```bash
# SSH into the container
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

### Scale up the plan

Edit `infra/terraform.tfvars`:

```hcl
sku_name = "B2"
```

Then:

```bash
cd infra && tofu apply
```

### Add a custom domain

1. Buy or transfer a domain (Namecheap, Cloudflare, etc.)
2. Add a CNAME: `www` → `worldcup-formations.azurewebsites.net`
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

### Store Terraform state in Azure (optional, for teams)

By default OpenTofu stores state in `infra/terraform.tfstate` on your local
machine. For a shared team setup, uncomment the `backend "azurerm"` block in
`infra/main.tf` and create a storage account first:

```bash
az group create --name rg-tfstate --location westeurope

az storage account create \
  --name <unique-storage-name> \
  --resource-group rg-tfstate \
  --sku Standard_LRS

az storage container create \
  --name tfstate \
  --account-name <unique-storage-name>
```

Then fill in the backend block in `infra/main.tf` and run `tofu init` again.

---

## Tear everything down

```bash
cd infra
tofu destroy
```

Type `yes` when prompted. This deletes the App Service, plan, and resource
group in one operation and stops all billing immediately.

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
fuser -k 5292/tcp    # Linux/macOS
# Windows: netstat -ano | findstr :5292 → taskkill /PID <pid> /F
```

### Startup crashes with a migration error

The database file may be from a different migration baseline. Delete it:

```bash
az webapp ssh --name worldcup-formations --resource-group rg-worldcup
rm /home/data/worldcup.db
exit
az webapp restart --name worldcup-formations --resource-group rg-worldcup
```

### GitHub Actions fails at "Set up .NET"

The `10.0.x` version string may not yet be available on GitHub-hosted runners.
Change `DOTNET_VERSION` in `.github/workflows/azure-deploy.yml` to `9.0.x` and
update the `dotnet_version` in `infra/main.tf` to `"9.0"` accordingly, then
`tofu apply`.

### App sleeps after 20+ minutes (F1 tier)

F1 has no Always On. Either:
- Change `sku_name` to `"B1"` in `terraform.tfvars` and run `tofu apply`
- Or use a free uptime monitor (UptimeRobot) to ping the app every 5 minutes

### `tofu apply` fails: "subscription not found"

Make sure `subscription_id` in `terraform.tfvars` matches what
`az account show` reports, and that you are logged in with `az login`.
