terraform {
  required_version = ">= 1.8.0"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.0"
    }
  }

  # Uncomment and fill in to store state in Azure Storage (see DEPLOYMENT.md)
  # backend "azurerm" {
  #   resource_group_name  = "rg-tfstate"
  #   storage_account_name = "<your-storage-account>"
  #   container_name       = "tfstate"
  #   key                  = "worldcup.terraform.tfstate"
  # }
}

provider "azurerm" {
  features {}
  subscription_id = var.subscription_id
}

# ── Resource Group ─────────────────────────────────────────────────────────────
resource "azurerm_resource_group" "main" {
  name     = var.resource_group_name
  location = var.location
}

# ── App Service Plan ───────────────────────────────────────────────────────────
resource "azurerm_service_plan" "main" {
  name                = var.plan_name
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  os_type             = "Linux"
  sku_name            = var.sku_name
}

# ── Web App ────────────────────────────────────────────────────────────────────
resource "azurerm_linux_web_app" "main" {
  name                = var.webapp_name
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  service_plan_id     = azurerm_service_plan.main.id

  https_only = true

  site_config {
    # always_on is not supported on the Free (F1) tier
    always_on = var.sku_name != "F1"

    application_stack {
      dotnet_version = "10.0"
    }

    # Allow SignalR WebSocket connections (required for Blazor Server)
    websockets_enabled = true
  }

  app_settings = {
    # Put the SQLite database outside the deployed directory so it survives
    # redeployments without having to reseed 37 000 records every time.
    "DB_PATH"                = "/home/data/worldcup.db"
    "ASPNETCORE_ENVIRONMENT" = "Production"
  }

  logs {
    http_logs {
      file_system {
        retention_in_days = 7
        retention_in_mb   = 35
      }
    }
    application_logs {
      file_system_level = "Warning"
    }
  }

  tags = {
    project = "world-cup-formations"
  }
}
