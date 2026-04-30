resource "azurerm_storage_account" "uploads" {
  name                     = "stcodeshift${var.environment}"
  resource_group_name      = azurerm_resource_group.main.name
  location                 = azurerm_resource_group.main.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  min_tls_version                   = "TLS1_2"
  allow_nested_items_to_be_public   = false

  blob_properties {
    delete_retention_policy {
      days = 7
    }
  }

  tags = local.common_tags
}

resource "azurerm_storage_container" "codebases" {
  name                  = "codebases"
  storage_account_name  = azurerm_storage_account.uploads.name
  container_access_type = "private"
}
