variable "subscription_id" {
  description = "Subscription ID for Azure"
  type        = string
}

variable "tenant_id" {
  description = "Tenant ID for Azure"
  type        = string
}

variable "rg_location" {
  description = "Azure region for the Resource Group"
  type        = string
  default     = "northeurope"
}

variable "storage_location" {
  description = "Azure region for the Storage Account"
  type        = string
  default     = "polandcentral"
}

variable "functions_location" {
  description = "Azure region for Function Apps"
  type        = string
  default     = "francecentral"
}

variable "project_name" {
  description = "Short name of the project (used in resource names)"
  type        = string
  default     = "faapp"
}

variable "environment" {
  description = "Environment name (dev, test, prod)"
  type        = string
  default     = "dev"
}

variable "sql_admin_password" {
  description = "Hasło admina do Azure SQL (podawane przez TF_VAR_sql_admin_password, nie trzymamy w repo)"
  type        = string
  sensitive   = true
}

variable "jwt_signing_key" {
  description = "Sekret do podpisywania JWT (TF_VAR_jwt_signing_key)"
  type        = string
  sensitive   = true
}
