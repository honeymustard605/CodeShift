data "azurerm_client_config" "current" {}

resource "azurerm_key_vault" "main" {
  name                = "kv-codeshift-${var.environment}"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  tenant_id           = data.azurerm_client_config.current.tenant_id
  sku_name            = "standard"

  access_policy {
    tenant_id = data.azurerm_client_config.current.tenant_id
    object_id = data.azurerm_client_config.current.object_id

    secret_permissions = ["Get", "List", "Set", "Delete", "Purge"]
  }

  tags = local.common_tags
}

resource "azurerm_key_vault_access_policy" "api" {
  key_vault_id = azurerm_key_vault.main.id
  tenant_id    = data.azurerm_client_config.current.tenant_id
  object_id    = azurerm_linux_web_app.api.identity[0].principal_id

  secret_permissions = ["Get", "List"]
}

resource "azurerm_key_vault_secret" "anthropic_api_key" {
  name         = "anthropic-api-key"
  key_vault_id = azurerm_key_vault.main.id
  value        = var.anthropic_api_key
}

resource "azurerm_key_vault_secret" "db_connection" {
  name         = "db-connection-string"
  key_vault_id = azurerm_key_vault.main.id
  value = join(";", [
    "Host=${azurerm_postgresql_flexible_server.main.fqdn}",
    "Database=codeshift",
    "Username=codeshift",
    "Password=${var.postgres_admin_password}",
    "SslMode=Require"
  ])
}
