variable "location" { type = string }
variable "resource_group_name" { type = string }

variable "function_app_name" { type = string }
variable "service_plan_id" { type = string }
variable "storage_account_name" { type = string }
variable "storage_account_access_key" { type = string }

variable "jwt_signing_key" {
  type      = string
  sensitive = true
}

variable "cors_allowed_origins" {
  type    = list(string)
  default = []
}

variable "tags" {
  type    = map(string)
  default = {}
}
