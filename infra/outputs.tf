output "public_ip" {
  description = "Public IP address of the VM — point your DNS A record here"
  value       = azurerm_public_ip.main.ip_address
}

output "ssh_command" {
  description = "SSH command to connect to the VM"
  value       = "ssh ${var.admin_username}@${azurerm_public_ip.main.ip_address}"
}

output "app_url" {
  description = "URL of the app (after DNS/nginx are configured)"
  value       = "http://${azurerm_public_ip.main.ip_address}"
}
