terraform {
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "=2.80.0"
    }
  }
}

provider "azurerm" {
  features {}
}

locals {
  deployment_name               = "statefulsvc"
  locations                     = ["eastus", "westus", "centralus"]
  admin_username                = "${local.deployment_name}user"
  local_service_directory       = "../src/bin/Release/net5.0/linux-x64/publish/"
  destination_service_directory = "/usr/bin/${local.deployment_name}"
  executable_Path               = "${local.destination_service_directory}/StatefulService"
  service_ips_args              = formatlist("--members:%s http://%s:80", range(0, 3), [for k, v in module.services : v.public_ip])
  service_startup_command       = "${local.executable_Path} --urls http://0.0.0.0:80 ${join(" ", local.service_ips_args)}"
}

module "ssh_key" {
  source = "./modules/ssh_key"
}

module "services" {
  for_each = toset(local.locations)

  source = "./modules/service-vm"

  location        = each.key
  deployment_name = local.deployment_name
  admin_username  = local.admin_username
  ssh_public_key  = module.ssh_key.public_key
}

module "provisioning" {
  for_each = module.services

  source = "./modules/service-provisioning"

  host_ip_address       = each.value.public_ip
  service_name          = local.deployment_name
  username              = local.admin_username
  ssh_private_key       = module.ssh_key.private_key
  source_directory      = local.local_service_directory
  destination_directory = local.destination_service_directory
  command               = "${local.service_startup_command} --hostAddressHint ${each.value.public_ip}"
}
