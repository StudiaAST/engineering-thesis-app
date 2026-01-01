module "podwykonawcy_function" {
  source = "./modules/podwykonawcy-function"

  project_name        = var.project_name
  environment         = var.environment
  resource_group_name = azurerm_resource_group.rg.name
  functions_location  = var.functions_location

  storage_account_name       = azurerm_storage_account.sa.name
  storage_account_access_key = azurerm_storage_account.sa.primary_access_key

  # korzystamy z istniejącego planu Y1
  service_plan_id = module.usterki_function.service_plan_id

}

module "usterki_function" {
  source = "./modules/usterki-function"

  project_name        = var.project_name
  environment         = var.environment
  resource_group_name = azurerm_resource_group.rg.name
  functions_location  = var.functions_location

  storage_account_name       = azurerm_storage_account.sa.name
  storage_account_access_key = azurerm_storage_account.sa.primary_access_key
  sql_admin_password         = var.sql_admin_password

}
module "auth_function" {
  source = "./modules/auth-function"

  location            = var.functions_location
  resource_group_name = azurerm_resource_group.rg.name

  function_app_name          = "faapp-dev-auth-func"
  service_plan_id            = module.usterki_function.service_plan_id
  storage_account_name       = azurerm_storage_account.sa.name
  storage_account_access_key = azurerm_storage_account.sa.primary_access_key

  jwt_signing_key = var.jwt_signing_key

  cors_allowed_origins = [
    "http://localhost:5234",
    "https://localhost:7234"
  ]

  tags = {
    project     = var.project_name
    environment = var.environment
  }
}
