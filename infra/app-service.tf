resource "azurerm_service_plan" "main" {
  name                = "asp-codeshift-${var.environment}"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  os_type             = "Linux"
  sku_name            = var.app_service_sku
  tags                = local.common_tags
}

resource "azurerm_linux_web_app" "api" {
  name                = "app-codeshift-api-${var.environment}"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  service_plan_id     = azurerm_service_plan.main.id

  identity {
    type = "SystemAssigned"
  }

  site_config {
    application_stack {
      docker_image_name = var.container_image
    }
    vnet_route_all_enabled = true
  }

  virtual_network_subnet_id = data.azurerm_subnet.spoke_app.id

  app_settings = {
    ASPNETCORE_ENVIRONMENT   = var.environment == "prod" ? "Production" : "Development"
    "ConnectionStrings__Default" = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.db_connection.versionless_id})"
    "Anthropic__ApiKey"      = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.anthropic_api_key.versionless_id})"
    "Cors__Origin"           = var.cors_origin
    WEBSITES_PORT            = "8080"
  }

  tags = local.common_tags
}

resource "azurerm_monitor_diagnostic_setting" "api" {
  name                       = "diag-codeshift-api-${var.environment}"
  target_resource_id         = azurerm_linux_web_app.api.id
  log_analytics_workspace_id = data.azurerm_log_analytics_workspace.lockdown.id

  enabled_log { category = "AppServiceHTTPLogs" }
  enabled_log { category = "AppServiceConsoleLogs" }
  enabled_log { category = "AppServiceAppLogs" }

  metric {
    category = "AllMetrics"
  }
}
