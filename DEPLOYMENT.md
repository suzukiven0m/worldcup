# Deploying to Azure VM with OpenTofu

This guide provisions World Cup Formations (Blazor Server, .NET 10, SQLite) on
an Azure Linux VM using **OpenTofu**. App Service is blocked by the Azure for
Students policy; a `Standard_B2as_v2` VM in `swedencentral` works instead.

**What gets created:**
- Ubuntu 24.04 LTS VM (`Standard_B2as_v2` — 2 vCPU, 8 GB RAM)
- Static public IP, VNet, NSG (ports 22/80/443)
- nginx reverse proxy → Blazor Server on 127.0.0.1:5000
- `worldcup.service` systemd unit (auto-restart on crash/reboot)
- SQLite database at `/var/worldcup/worldcup.db` (survives redeployments)

---

## Prerequisites

| Tool | Install |
|---|---|
| Azure CLI | [aka.ms/installazurecliwindows](https://aka.ms/installazurecliwindows) or `winget install Microsoft.AzureCLI` |
| OpenTofu ≥ 1.8 | [opentofu.org/docs/intro/install](https://opentofu.org/docs/intro/install/) |
| .NET 10 SDK | [dotnet.microsoft.com/download](https://dotnet.microsoft.com/download) |

Verify:

```bash
az --version
tofu --version
dotnet --version   # should print 10.x.x
```

---

## Part 1 — Azure login

```bash
az login
az account show --query "{name:name, id:id}" -o table
```

If you have multiple subscriptions:

```bash
az account set --subscription "<subscription-id>"
```

---

## Part 2 — SSH key

The VM uses SSH key auth. If you don't have a key yet:

```bash
ssh-keygen -t ed25519 -C "worldcup-vm"
# Accept defaults — saves to ~/.ssh/id_ed25519
```

---

## Part 3 — Configure OpenTofu variables

```bash
cp infra/terraform.tfvars.example infra/terraform.tfvars
```

Edit `infra/terraform.tfvars`:

```hcl
subscription_id     = "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"   # from az account show
location            = "swedencentral"
resource_group_name = "rg-worldcup"
vm_name             = "vm-worldcup"
vm_size             = "Standard_B2as_v2"
admin_username      = "azureuser"
ssh_public_key_path = "~/.ssh/id_ed25519.pub"
```

> `terraform.tfvars` is gitignored — it will not be committed.

---

## Part 4 — Provision the VM

```bash
cd infra
tofu init
tofu plan
tofu apply
```

Type `yes` when prompted. Takes ~3 minutes. When complete:

```
Outputs:
  public_ip   = "x.x.x.x"
  ssh_command = "ssh azureuser@x.x.x.x"
  app_url     = "http://x.x.x.x"
```

The VM boots and cloud-init runs in the background (~2 minutes after apply).
It installs .NET 10, nginx, and registers the systemd service. The service
starts automatically once you deploy the app code in the next step.

---

## Part 5 — Deploy the app

### 5.1 Publish locally

```bash
cd ..   # repo root

dotnet publish src/WorldCupFormations.Web/WorldCupFormations.Web.csproj \
  --configuration Release \
  --output ./publish
```

### 5.2 Copy to the VM

Replace `x.x.x.x` with your IP from the tofu output:

```bash
# Linux / macOS
rsync -avz --delete publish/ azureuser@x.x.x.x:/tmp/worldcup-deploy/

# Windows (PowerShell) — use scp instead
scp -r publish\* azureuser@x.x.x.x:/tmp/worldcup-deploy/
```

### 5.3 Install and start

```bash
ssh azureuser@x.x.x.x

# On the VM:
sudo rsync -a --delete /tmp/worldcup-deploy/ /opt/worldcup/
sudo chown -R worldcup:worldcup /opt/worldcup
sudo systemctl start worldcup
sudo systemctl status worldcup   # should show "active (running)"
exit
```

Open `http://x.x.x.x` in your browser. The first request seeds the database
(~5–15 s). Subsequent starts skip seeding.

---

## Part 6 — HTTPS with Let's Encrypt (optional but recommended)

You need a domain name pointing to the VM's public IP first (DNS A record).

```bash
ssh azureuser@x.x.x.x

sudo apt install -y certbot python3-certbot-nginx
sudo certbot --nginx -d yourdomain.com
# Follow prompts — certbot edits nginx config and installs the cert

sudo systemctl reload nginx
exit
```

Certbot auto-renews the certificate via a systemd timer. No further action needed.

---

## Redeploying after code changes

```bash
# From repo root:
dotnet publish src/WorldCupFormations.Web/WorldCupFormations.Web.csproj \
  --configuration Release --output ./publish

rsync -avz --delete publish/ azureuser@x.x.x.x:/tmp/worldcup-deploy/

ssh azureuser@x.x.x.x "
  sudo rsync -a --delete /tmp/worldcup-deploy/ /opt/worldcup/ &&
  sudo chown -R worldcup:worldcup /opt/worldcup &&
  sudo systemctl restart worldcup
"
```

The SQLite database at `/var/worldcup/worldcup.db` is untouched between deploys.

---

## Useful operations

### View live app logs

```bash
ssh azureuser@x.x.x.x
sudo journalctl -u worldcup -f
```

### Force a database reseed

```bash
ssh azureuser@x.x.x.x
sudo systemctl stop worldcup
sudo rm /var/worldcup/worldcup.db
sudo systemctl start worldcup
sudo journalctl -u worldcup -f   # watch the seed run
```

### Check nginx status

```bash
ssh azureuser@x.x.x.x
sudo systemctl status nginx
sudo nginx -t             # test config syntax
sudo cat /var/log/nginx/error.log
```

### Check cloud-init completed successfully

```bash
ssh azureuser@x.x.x.x
sudo cloud-init status    # should print "done"
sudo cat /var/log/cloud-init-output.log
dotnet --version          # should print 10.x.x
```

### Scale (resize the VM)

```hcl
# infra/terraform.tfvars
vm_size = "Standard_B4as_v2"   # 4 vCPU, 16 GB
```

```bash
cd infra && tofu apply
```

Azure stops, resizes, and restarts the VM. Downtime ~2 minutes.

---

## Estimated cost (Azure for Students)

| VM size | vCPU | RAM | Cost/month |
|---|---|---|---|
| Standard_B2as_v2 | 2 | 8 GB | ~$30 |
| Standard_B4as_v2 | 4 | 16 GB | ~$60 |

At ~$30/month, $100 of student credit covers ~3 months. The VM is significantly
more powerful than App Service B1 for the price point.

---

## Tear everything down

```bash
cd infra
tofu destroy
```

Deletes the VM, NIC, public IP, NSG, VNet, and resource group. All billing stops.

---

## Troubleshooting

### cloud-init hasn't finished yet

After `tofu apply`, wait 2–3 minutes before deploying. Check:

```bash
ssh azureuser@x.x.x.x
sudo cloud-init status
```

If it shows `running`, wait and check again. If `error`:

```bash
sudo cat /var/log/cloud-init-output.log
```

### `systemctl start worldcup` fails — "no such file"

The publish files haven't been copied yet, or were copied to the wrong path.
Verify:

```bash
ls /opt/worldcup/WorldCupFormations.Web.dll
```

### nginx returns 502 Bad Gateway

The app isn't running. Check:

```bash
sudo systemctl status worldcup
sudo journalctl -u worldcup -n 50
```

### SSH connection refused

The VM may still be booting (wait ~1 minute after `tofu apply`), or port 22
is blocked. Verify the NSG rule exists:

```bash
az network nsg rule list --nsg-name vm-worldcup-nsg --resource-group rg-worldcup -o table
```

### `tofu apply` fails: "subscription not found"

Make sure `subscription_id` in `terraform.tfvars` matches `az account show`,
and that you are logged in with `az login`.
