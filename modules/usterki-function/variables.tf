variable "project_name" {
  description = "Project short name, e.g. faapp"
  type        = string
}

variable "environment" {
  description = "Environment name, e.g. dev"
  type        = string
}

variable "resource_group_name" {
  description = "Name of the Resource Group where the Function App will be deployed"
  type        = string
}

variable "functions_location" {
  description = "Azure region for this Function App"
  type        = string
}

variable "storage_account_name" {
  description = "Name of the Storage Account used by this Function App"
  type        = string
}

variable "storage_account_access_key" {
  description = "Primary access key for the Storage Account"
  type        = string
}
