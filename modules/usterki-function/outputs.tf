output "service_plan_id" {
  description = "ID of the App Service Plan used by the usterki Function App"
  value       = azurerm_service_plan.functions_plan.id
}
