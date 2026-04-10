output "app_url" {
  description = "Public HTTPS URL of the Container App"
  value       = "https://${azurerm_container_app.main.ingress[0].fqdn}"
}

output "app_name" {
  description = "Container App name (use as AZURE_APP_NAME in GitHub Actions)"
  value       = azurerm_container_app.main.name
}

output "resource_group_name" {
  description = "Resource group name"
  value       = azurerm_resource_group.main.name
}
