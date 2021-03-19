terraform {
  required_providers {
    kubernetes = {
      source  = "hashicorp/kubernetes"
      version = ">= 2.0.0"
    }
  }
  backend "kubernetes" {
    secret_suffix    = "identity-business-users"
    load_config_file = true
  }
}

provider "kubernetes" {
  config_path = "~/.kube/config"
}

resource "kubernetes_secret" "identity" {
  metadata {
    name      = var.project_secrets_name
    namespace = var.namespace
  }
  data = {
    Broker_Host                       = var.settings_broker_connection_string
    DatabaseSettings_ConnectionString = var.settings_db_connection_string
    DatabaseSettings_DatabaseName     = var.settings_db_name
    IdentityServer_OrgUrl             = var.settings_identity_server_org_url
    IdentityServer_ClientId           = var.settings_identity_server_client_id
    IdentityServer_ApiUrl             = var.settings_identity_server_api_url
    IdentityServer_ApiKey             = var.settings_identity_server_api_key
  }
}

resource "kubernetes_deployment" "identity" {
  metadata {
    name      = var.project_name
    namespace = var.namespace
    labels = {
      app = var.project_label
    }
  }

  spec {
    replicas = var.project_replicas_count
    selector {
      match_labels = {
        app = var.project_name
      }
    }
    template {
      metadata {
        labels = {
          app = var.project_name
        }
      }
      spec {
        image_pull_secrets {
          name = "${var.namespace}-do-registry"
        }
        container {
          image             = "${var.do_registry_name}/${var.project_name}:${var.project_image_tag}"
          name              = "${var.project_name}-container"
          image_pull_policy = "Always"
          resources {
            limits = {
              cpu    = "300m"
              memory = "2000Mi"
            }
            requests = {
              cpu    = "125m"
              memory = "50Mi"
            }
          }
          port {
            container_port = 80
            protocol       = "TCP"
          }
          env_from {
            secret_ref {
              name = var.project_secrets_name
            }
          }
        }
      }
    }
  }
}