output "webapp_url" {
  description = "HTTPS URL of the deployed web app"
  value       = "https://${azurerm_linux_web_app.main.default_hostname}"
}

output "webapp_name" {
  description = "Name of the Azure Web App (use as AZURE_WEBAPP_NAME secret in GitHub Actions)"
  value       = azurerm_linux_web_app.main.name
}

output "resource_group_name" {
  description = "Name of the resource group"
  value       = azurerm_resource_group.main.name
}
