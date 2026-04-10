variable "subscription_id" {
  description = "Azure subscription ID"
  type        = string
  sensitive   = true
}

variable "location" {
  description = "Azure region (e.g. westeurope, eastus, japaneast)"
  type        = string
  default     = "westeurope"
}

variable "resource_group_name" {
  description = "Name of the resource group"
  type        = string
  default     = "rg-worldcup"
}

variable "plan_name" {
  description = "Name of the App Service Plan"
  type        = string
  default     = "plan-worldcup"
}

variable "webapp_name" {
  description = "Globally unique name for the Web App (becomes <name>.azurewebsites.net)"
  type        = string
}

variable "sku_name" {
  description = "App Service Plan SKU. F1 = free, B1 = ~$13/month (recommended)"
  type        = string
  default     = "B1"

  validation {
    condition     = contains(["F1", "B1", "B2", "B3"], var.sku_name)
    error_message = "sku_name must be one of: F1, B1, B2, B3."
  }
}
