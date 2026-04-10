variable "subscription_id" {
  description = "Azure subscription ID"
  type        = string
  sensitive   = true
}

variable "location" {
  description = "Azure region"
  type        = string
  default     = "swedencentral"
}

variable "resource_group_name" {
  description = "Name of the resource group"
  type        = string
  default     = "rg-worldcup"
}

variable "app_name" {
  description = "Name used for all resources (Container App, environment, logs)"
  type        = string
  default     = "worldcup"
}

variable "container_image" {
  description = "Docker image to deploy, e.g. ghcr.io/youruser/worldcup:latest"
  type        = string
  # Public placeholder — replaced on first real deploy via az containerapp update
  default     = "mcr.microsoft.com/azuredocs/containerapps-helloworld:latest"
}

variable "min_replicas" {
  description = "0 = scale to zero (free, ~10s cold start); 1 = always on (~$5-8/mo)"
  type        = number
  default     = 0
}
