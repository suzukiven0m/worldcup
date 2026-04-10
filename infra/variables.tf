variable "subscription_id" {
  description = "Azure subscription ID"
  type        = string
  sensitive   = true
}

variable "location" {
  description = "Azure region (e.g. swedencentral, eastus, japaneast)"
  type        = string
  default     = "swedencentral"
}

variable "resource_group_name" {
  description = "Name of the resource group"
  type        = string
  default     = "rg-worldcup"
}

variable "vm_name" {
  description = "Name of the Virtual Machine"
  type        = string
  default     = "vm-worldcup"
}

variable "vm_size" {
  description = "VM SKU — Standard_B2as_v2 works on Azure for Students in swedencentral"
  type        = string
  default     = "Standard_B2as_v2"
}

variable "admin_username" {
  description = "SSH admin username"
  type        = string
  default     = "azureuser"
}

variable "ssh_public_key_path" {
  description = "Path to your SSH public key"
  type        = string
  default     = "~/.ssh/id_ed25519.pub"
}
