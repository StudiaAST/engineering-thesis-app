resource "azurerm_linux_function_app" "this" {
  name                = var.function_app_name
  location            = var.location
  resource_group_name = var.resource_group_name

  service_plan_id            = var.service_plan_id
  storage_account_name       = var.storage_account_name
  storage_account_access_key = var.storage_account_access_key

  https_only = true

  site_config {
    # Koszt: minimalny – bez App Insights i bez AlwaysOn (na Consumption i tak nie ma AlwaysOn)
    application_stack {
      dotnet_version = "8.0"
    }

    cors {
      allowed_origins     = var.cors_allowed_origins
      support_credentials = true
    }
  }

  app_settings = {
    "FUNCTIONS_EXTENSION_VERSION" = "~4"
    "FUNCTIONS_WORKER_RUNTIME"    = "dotnet-isolated"

    # JWT (na razie MVP)
    "JWT__SigningKey" = var.jwt_signing_key
    "JWT__Issuer"     = "ConstructHub"
    "JWT__Audience"   = "ConstructHubClient"

    # Tanie logowanie: nie dokładamy AppInsights tutaj
    # (nic nie ustawiamy dla APPLICATIONINSIGHTS_CONNECTION_STRING)
  }

  tags = var.tags
}
