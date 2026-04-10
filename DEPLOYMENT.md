# Deploying to Azure Container Apps with OpenTofu

World Cup Formations (Blazor Server, .NET 10, SQLite) runs as a Docker container
on **Azure Container Apps** (Consumption plan). The Docker image is stored for
free on **GitHub Container Registry (ghcr.io)**. Every push to `master` builds
and redeploys automatically via GitHub Actions.

**Estimated cost:**
- `min_replicas = 0` (scale to zero) — effectively **free** for a portfolio app;
  first visit after idle takes ~10 seconds to cold-start
- `min_replicas = 1` (always on) — ~**$5–8/month**

> **Note on SQLite**: the container filesystem is ephemeral. On each cold start
> the database reseeds (~2–5 s). This is fine for a read-only portfolio app.

---

## Prerequisites

| Tool | Install |
|---|---|
| Azure CLI | [aka.ms/installazurecliwindows](https://aka.ms/installazurecliwindows) or `winget install Microsoft.AzureCLI` |
| OpenTofu ≥ 1.8 | [opentofu.org/docs/intro/install](https://opentofu.org/docs/intro/install/) |
| Docker | [docs.docker.com/get-docker](https://docs.docker.com/get-docker/) |

Verify:

```bash
az --version
tofu --version
docker --version
```

---

## Part 1 — Azure login

```bash
az login
az account show --query "{name:name, id:id}" -o table
```

Note your **subscription ID** for the next step.

---

## Part 2 — Configure variables

```bash
cp infra/terraform.tfvars.example infra/terraform.tfvars
```

Edit `infra/terraform.tfvars`:

```hcl
subscription_id     = "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
location            = "swedencentral"
resource_group_name = "rg-worldcup"
app_name            = "worldcup"
container_image     = "ghcr.io/yourgithubuser/worldcup:latest"
min_replicas        = 0
```

Replace `yourgithubuser` with your actual GitHub username. The image doesn't
exist yet — that's fine, we'll push it in Part 4.

> `terraform.tfvars` is gitignored — it will not be committed.

---

## Part 3 — Provision infrastructure

```bash
cd infra
tofu init
tofu plan
tofu apply
```

Type `yes` when prompted. Creates:
- Resource group
- Log Analytics workspace
- Container App Environment (Consumption plan)
- Container App

When complete:

```
Outputs:
  app_url            = "https://worldcup.<hash>.swedencentral.azurecontainerapps.io"
  app_name           = "worldcup"
  resource_group_name = "rg-worldcup"
```

The app will show an error until you push the Docker image in the next step.

---

## Part 4 — Push the first Docker image manually

Do this once before GitHub Actions takes over.

```bash
cd ..   # repo root

# Log in to GitHub Container Registry
echo $CR_PAT | docker login ghcr.io -u yourgithubuser --password-stdin
# (or: docker login ghcr.io  — then enter your GitHub username + Personal Access Token)

# Build and push
docker build -t ghcr.io/yourgithubuser/worldcup:latest .
docker push ghcr.io/yourgithubuser/worldcup:latest
```

Then update the Container App to use it:

```bash
az containerapp update \
  --name worldcup \
  --resource-group rg-worldcup \
  --image ghcr.io/yourgithubuser/worldcup:latest
```

Visit the URL from the tofu output. First request seeds the database (~5–10 s).

---

## Part 5 — Set up GitHub Actions (automated deploys)

Every push to `master` will build a new image and redeploy.

### 5.1 Create an Azure service principal

```bash
az ad sp create-for-rbac \
  --name "sp-worldcup-deploy" \
  --role contributor \
  --scopes /subscriptions/<your-subscription-id>/resourceGroups/rg-worldcup \
  --sdk-auth
```

Copy the entire JSON output — you need it in the next step.

### 5.2 Add secrets to GitHub

**Settings → Secrets and variables → Actions → New repository secret**

| Secret name | Value |
|---|---|
| `AZURE_CREDENTIALS` | The full JSON from the sp create command above |
| `AZURE_APP_NAME` | `worldcup` |
| `AZURE_RESOURCE_GROUP` | `rg-worldcup` |

The `GITHUB_TOKEN` for pushing to ghcr.io is provided automatically — no extra
secret needed.

### 5.3 Make your GitHub package public (optional but recommended)

By default ghcr.io packages are private. If you want the Container App to pull
without credentials, make the package public:

**GitHub → Your profile → Packages → worldcup → Package settings → Change visibility → Public**

Or keep it private — Container Apps can pull from private registries with a
registry credential (see Troubleshooting below).

### 5.4 Trigger the first automated deploy

```bash
git push origin master
```

Watch it run under **Actions** in your GitHub repo. Each push to `master` from
now on builds a fresh image tagged with the commit SHA and redeploys.

---

## Useful operations

### View live logs

```bash
az containerapp logs show \
  --name worldcup \
  --resource-group rg-worldcup \
  --follow
```

### Force a redeploy (same image)

```bash
az containerapp revision restart \
  --name worldcup \
  --resource-group rg-worldcup \
  --revision $(az containerapp revision list \
    --name worldcup \
    --resource-group rg-worldcup \
    --query "[0].name" -o tsv)
```

### Switch to always-on (no cold starts)

Edit `infra/terraform.tfvars`:

```hcl
min_replicas = 1
```

Then:

```bash
cd infra && tofu apply
```

### Tear everything down

```bash
cd infra
tofu destroy
```

Also delete the service principal if you no longer need it:

```bash
az ad sp delete --id $(az ad sp list --display-name sp-worldcup-deploy --query "[0].id" -o tsv)
```

---

## Troubleshooting

### Container App pulls fail (private image)

If your ghcr.io package is private, register the registry with the Container App:

```bash
az containerapp registry set \
  --name worldcup \
  --resource-group rg-worldcup \
  --server ghcr.io \
  --username yourgithubuser \
  --password <github-personal-access-token>
```

### Cold start takes 30+ seconds

The database is seeding. Blazor Server + EF Core + SQLite seed of ~37,000 rows
takes ~5–10 s. The remaining time is container startup. This only happens on
the first request after the container has been idle (scale-to-zero). Set
`min_replicas = 1` in `terraform.tfvars` and `tofu apply` to eliminate it.

### `tofu apply` fails with "feature not supported in region"

Try `location = "northeurope"` or `location = "eastus"` in `terraform.tfvars`.
Container Apps use the `Microsoft.App` provider, which has different regional
availability than App Service (`Microsoft.Web`).

### GitHub Actions deploy step fails: "containerapp not found"

Make sure `AZURE_APP_NAME` and `AZURE_RESOURCE_GROUP` secrets match exactly what
`tofu apply` created (check `tofu output`).

### App returns 502 immediately after deploy

The new revision is still starting. Container Apps does a zero-downtime revision
swap — wait ~30 seconds and refresh.
