terraform {
  required_version = ">= 1.7"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 3.97"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.6"
    }
  }
}

provider "azurerm" {
  features {
    key_vault {
      purge_soft_delete_on_destroy = false
    }
  }
}

locals {
  common_tags = {
    project     = "codeshift"
    Environment = var.environment
    Owner       = var.owner
    CostCenter  = var.cost_center
  }
}

resource "azurerm_resource_group" "main" {
  name     = var.resource_group_name
  location = var.location
  tags     = local.common_tags
}

# ── LockDown landing zone references ─────────────────────────────────────────

data "azurerm_subnet" "spoke_app" {
  name                 = "snet-app"
  virtual_network_name = "vnet-spoke-workloads"
  resource_group_name  = "rg-${var.environment}-networking"
}

data "azurerm_subnet" "spoke_data" {
  name                 = "snet-data"
  virtual_network_name = "vnet-spoke-workloads"
  resource_group_name  = "rg-${var.environment}-networking"
}

data "azurerm_virtual_network" "spoke_workloads" {
  name                = "vnet-spoke-workloads"
  resource_group_name = "rg-${var.environment}-networking"
}

data "azurerm_log_analytics_workspace" "lockdown" {
  name                = "law-${var.environment}-lockdown"
  resource_group_name = "rg-${var.environment}-monitoring"
}
