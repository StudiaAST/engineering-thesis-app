resource "azurerm_mssql_server" "sqlsrv" {
  name                = "faapp-dev-sqlsrv"
  resource_group_name = azurerm_resource_group.rg.name
  location            = "Poland Central"
  version             = "12.0"

  # placeholdery – po imporcie ustawimy je poprawnie albo wyciszymy
  administrator_login          = "placeholderadmin"
  administrator_login_password = "P@ssw0rd-PLACEHOLDER-ChangeMe123!"

  public_network_access_enabled = true

  lifecycle {
    ignore_changes = [
      administrator_login,
      administrator_login_password,
      tags
    ]
  }

}

resource "azurerm_mssql_database" "sqldb" {
  name                 = "faapp-dev-sqldb"
  server_id            = azurerm_mssql_server.sqlsrv.id
  storage_account_type = "Local"

}
