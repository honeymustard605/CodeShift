terraform {
  backend "azurerm" {
    resource_group_name  = "rg-terraform-state"
    storage_account_name = "stcodeshift"
    container_name       = "tfstate"
    key                  = "codeshift.tfstate"
  }
}
