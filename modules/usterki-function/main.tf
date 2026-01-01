variable "sql_admin_password" {
  description = "Hasło admina SQL przekazywane z root (TF_VAR_sql_admin_password)"
  type        = string
  sensitive   = true
}

resource "azurerm_service_plan" "functions_plan" {
  name                = "${var.project_name}-${var.environment}-func-plan"
  resource_group_name = var.resource_group_name
  location            = var.functions_location

  os_type  = "Linux"
  sku_name = "Y1"
}

resource "azurerm_linux_function_app" "usterki" {
  name                = "${var.project_name}-${var.environment}-usterki-func"
  resource_group_name = var.resource_group_name
  location            = var.functions_location

  service_plan_id            = azurerm_service_plan.functions_plan.id
  storage_account_name       = var.storage_account_name
  storage_account_access_key = var.storage_account_access_key

  https_only = true

  tags = {
    environment = var.environment
    project     = var.project_name
  }

  identity {
    type = "SystemAssigned"
  }

  app_settings = {
    FUNCTIONS_WORKER_RUNTIME = "node"
    STORAGE_ACCOUNT_NAME     = var.storage_account_name
    USTERKI_CONTAINER_NAME   = "usterki"
    WEBSITE_RUN_FROM_PACKAGE = "1"
  }

  connection_string {
    name  = "SQL_CONNECTION_STRING"
    type  = "SQLAzure"
    value = "Server=tcp:faapp-dev-sqlsrv.database.windows.net,1433;Initial Catalog=faapp-dev-sqldb;Persist Security Info=False;User ID=sqladmin;Password=${var.sql_admin_password};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
  }

  site_config {
    application_stack {
      node_version = "18"
    }
  }

  lifecycle {
    ignore_changes = [
      site_config,
      app_settings["WEBSITE_RUN_FROM_PACKAGE"],
      app_settings["AZURE_STORAGE_CONNECTION_STRING"],
      app_settings["DefaultEndpointsProtocol"],
      app_settings["WEBSITE_MOUNT_ENABLED"],
      storage_account_access_key
    ]
  }
}
