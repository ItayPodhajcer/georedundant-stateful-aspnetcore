output "nodes" {
  value = {
    for region, value in module.services :
    region => value.public_ip
  }
}
