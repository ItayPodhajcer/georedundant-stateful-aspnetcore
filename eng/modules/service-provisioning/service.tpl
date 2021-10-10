[Unit]
Description=${name}

[Service]
Type=simple
ExecStart=${command}
User=1000
Group=1000

[Service]
Environment="DOTNET_BUNDLE_EXTRACT_BASE_DIR=%h/.net"
Restart=on-failure
RestartSec=5s

[Install]
WantedBy=default.target