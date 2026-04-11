resource "azurerm_service_plan" "main" {
  name                = "asp-codeshift-${var.environment}"
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  os_type             = "Linux"
  sku_name            = var.app_service_sku
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
  }

  app_settings = {
    ASPNETCORE_ENVIRONMENT                    = var.environment == "prod" ? "Production" : "Development"
    "ConnectionStrings__DefaultConnection"    = "@Microsoft.KeyVault(SecretUri=${azurerm_key_vault_secret.db_connection.id})"
    WEBSITES_PORT                             = "8080"
  }

  tags = {
    project     = "codeshift"
    environment = var.environment
  }
}
