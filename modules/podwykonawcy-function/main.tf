resource "azurerm_linux_function_app" "podwykonawcy" {
  name                = "${var.project_name}-${var.environment}-podwyk-func"
  resource_group_name = var.resource_group_name
  location            = var.functions_location

  # używamy istniejącego planu z zewnątrz
  service_plan_id = var.service_plan_id

  storage_account_name       = var.storage_account_name
  storage_account_access_key = var.storage_account_access_key

  https_only = true

  identity {
    type = "SystemAssigned"
  }

  app_settings = {
    FUNCTIONS_WORKER_RUNTIME    = "node"
    STORAGE_ACCOUNT_NAME        = var.storage_account_name
    PODWYKONAWCY_CONTAINER_NAME = "podwykonawcy"
    WEBSITE_RUN_FROM_PACKAGE    = "1"
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
