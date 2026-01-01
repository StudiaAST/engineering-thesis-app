
# Locals – nazwy zasobów


locals {
  resource_group_name = "${var.project_name}-${var.environment}-rg"

  # Stała nazwa storage account – tylko małe litery i cyfry, 3–24 znaków
  storage_account_name = "faappdevsa01"
}


# Resource Group (lokalizacja: var.rg_location)


resource "azurerm_resource_group" "rg" {
  name     = local.resource_group_name
  location = var.rg_location
}


# Storage Account (lokalizacja: var.storage_location)


resource "azurerm_storage_account" "sa" {
  name                     = local.storage_account_name
  resource_group_name      = azurerm_resource_group.rg.name
  location                 = var.storage_location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  account_kind             = "StorageV2"

  min_tls_version = "TLS1_2"

  tags = {
    project     = var.project_name
    environment = var.environment
  }
}


# Kontenery BLOB


resource "azurerm_storage_container" "usterki" {
  name                  = "usterki"
  storage_account_id    = azurerm_storage_account.sa.id
  container_access_type = "private"
}

resource "azurerm_storage_container" "podwykonawcy" {
  name                  = "podwykonawcy"
  storage_account_id    = azurerm_storage_account.sa.id
  container_access_type = "private"
}
