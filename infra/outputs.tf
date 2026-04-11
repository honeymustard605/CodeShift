output "api_url" {
  value       = "https://${azurerm_linux_web_app.api.default_hostname}"
  description = "Public URL of the CodeShift API"
}

output "storage_account_name" {
  value = azurerm_storage_account.uploads.name
}

output "key_vault_uri" {
  value = azurerm_key_vault.main.vault_uri
}
