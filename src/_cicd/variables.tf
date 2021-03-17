// main/common variables
variable "namespace" {
  description = "Albedo Team product's namespace"
  type        = string
  default     = "albedoteam-products"
}

variable "do_registry_name" {
  description = "Digital Ocean registry name"
  type        = string
  default     = "registry.digitalocean.com/albedoteam"
}

// project variables
variable "project_secrets_name" {
  description = "Secrets name"
  type        = string
  default     = "identity-business-users-secrets" 
}

variable "project_name" {
  description = "Source name"
  type        = string
  default     = "identity-business-users"
}

variable "project_label" {
  description = "Deployment Label / Container Name"
  type        = string
  default     = "IdentityBusinessUsers"
}

variable "project_image_tag" {
  description = "Image tag to be pulled from registry"
  type        = string
  default     = "latest"
}

variable "project_replicas_count" {
  description = "Number of container replicas to provision."
  type        = number
  default     = 1
}

// project settings variables
variable "settings_broker_connection_string" {
  description = "Broker Connection String"
  type        = string
  sensitive   = true
  default     = ""
}

variable "settings_db_connection_string" {
  description = "Db Connection String"
  type        = string
  sensitive   = true
  default     = ""
}

variable "settings_db_name" {
  description = "Db Name"
  type        = string
  default     = ""
}

variable "settings_identity_server_org_url" {
  description = "Identity Server Organization URL"
  type        = string
  default     = ""
}

variable "settings_identity_server_client_id" {
  description = "Identity Server Client Id"
  type        = string
  default     = ""
}

variable "settings_identity_server_api_url" {
  description = "Identity Server Api Url"
  type        = string
  default     = ""
}

variable "settings_identity_server_api_key" {
  description = "Identity Server Api Key"
  type        = string
  default     = ""
}