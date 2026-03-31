#!/usr/bin/env bash
set -euo pipefail

# VPS onboarding script for Jordnaer (Debian)
# Run as a user with sudo access.

# ── Firewall ─────────────────────────────────────────────────────────────────

sudo apt install -y ufw

sudo ufw allow 22
sudo ufw allow 80
sudo ufw allow 443
sudo ufw --force enable

# ── Unattended upgrades ───────────────────────────────────────────────────────

sudo apt update
sudo apt install -y unattended-upgrades
sudo dpkg-reconfigure --priority=low unattended-upgrades

# Schedule: Wednesday 03:00 ±30 min (mid-week, low traffic, visible by morning)
sudo mkdir -p /etc/systemd/system/apt-daily-upgrade.timer.d
sudo tee /etc/systemd/system/apt-daily-upgrade.timer.d/override.conf << 'EOF'
[Timer]
OnCalendar=
OnCalendar=Wed *-*-* 03:00:00
RandomizedDelaySec=30min
EOF

sudo systemctl daemon-reload
sudo systemctl restart apt-daily-upgrade.timer

# ── Dokploy ───────────────────────────────────────────────────────────────────

curl -sSL https://dokploy.com/install.sh -o /tmp/dokploy-install.sh
echo "Review the installer before proceeding (press q to continue):"
echo "  less /tmp/dokploy-install.sh"
echo ""
read -rp "Run Dokploy installer? [y/N] " confirm
if [[ "${confirm,,}" == "y" ]]; then
    sh /tmp/dokploy-install.sh
    echo ""
    echo "Dokploy installed. Access it at http://$(hostname -I | awk '{print $1}'):3000"
    echo "Next: create admin account, then add GHCR registry (ghcr.io) with a GitHub PAT (read:packages)."
else
    echo "Skipped. Run manually: sh /tmp/dokploy-install.sh"
fi
