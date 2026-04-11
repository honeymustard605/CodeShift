variable "resource_group_name" {
  type        = string
  description = "Azure resource group name"
  default     = "rg-codeshift"
}

variable "location" {
  type        = string
  description = "Azure region"
  default     = "eastus"
}

variable "environment" {
  type        = string
  description = "Deployment environment (dev, staging, prod)"
  default     = "prod"
}

variable "app_service_sku" {
  type        = string
  description = "App Service plan SKU"
  default     = "B2"
}

variable "postgres_sku" {
  type        = string
  description = "Azure Database for PostgreSQL flexible server SKU"
  default     = "Standard_B1ms"
}

variable "postgres_storage_mb" {
  type        = number
  default     = 32768
}

variable "postgres_admin_password" {
  type      = string
  sensitive = true
}

variable "container_image" {
  type        = string
  description = "Full container image reference for the API"
}
