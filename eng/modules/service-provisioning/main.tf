locals {
  service_file_content = templatefile("${path.module}/service.tpl", {
    name    = var.service_name
    command = var.command
  })
  service_tmp_path = "/tmp/${var.service_name}.service"
}

resource "null_resource" "main" {
  connection {
    type        = "ssh"
    user        = var.username
    private_key = var.ssh_private_key
    host        = var.host_ip_address
  }

  provisioner "file" {
    content     = local.service_file_content
    destination = local.service_tmp_path
  }

  provisioner "remote-exec" {
    inline = [
      "wget https://packages.microsoft.com/config/ubuntu/18.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb",
      "sudo dpkg -i packages-microsoft-prod.deb",
      "rm packages-microsoft-prod.deb",
      "sudo apt-get update",
      "sudo apt-get install -y apt-transport-https",
      "sudo apt-get update",
      "sudo apt-get install -y aspnetcore-runtime-5.0",
      "sudo mkdir -p ${var.destination_directory}",
      "sudo chown 1000:1000 ${var.destination_directory}",
      "sudo mv ${local.service_tmp_path} /etc/systemd/system/"
    ]
  }

  provisioner "file" {
    source      = var.source_directory
    destination = var.destination_directory
  }

  provisioner "remote-exec" {
    inline = [
      "sudo chmod 755 ${var.destination_directory}/*",
      "sudo setcap CAP_NET_BIND_SERVICE=+eip ${var.destination_directory}/*",
      "sudo systemctl daemon-reload",
      "sudo systemctl enable ${var.service_name}.service",
      "sudo systemctl start ${var.service_name}.service"
    ]
  }
}
