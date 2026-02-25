# current

- azure web app (container) (20$/month) 4gb ram poor cpu
- azure sql (5$/month)
- azure blob storage (close to free)
- email communication service (free)

# requirements

- must offer automatic deployment somehow (dokploy opensource could be an option, more options would be good)
- must be european vps/cloud provider (hetzner, scaleway)
- must be cheaper than 25$/month
- ssl certificate
- domain name (we have mini-moeder.dk we want to use that)
- comparable performance
- automated system updates (can we do this ourselves automatically?)

## product of task: is it viable?

**Yes, it is viable.** Estimated monthly cost: ~$10-11 (saving ~$14/month, ~56% reduction) with zero application code changes required.

---

## analysis

### what runs where today

| service | azure resource | monthly cost |
|---|---|---|
| app (blazor server + signalr) | azure web app for containers | ~$20 |
| database | azure sql (sql server) | ~$5 |
| images / data protection keys | azure blob storage | ~$1 |
| transactional email | azure communication services | free |

### key technical constraints that shape the decision

1. **SQL Server stays in Azure** — Azure SQL at $5/month is cheap and risk-free to keep. No database migration needed at all.

2. **Azure Blob Storage stores data protection keys** — `AddAzureBlobStorageDataProtection()` stores ASP.NET Core data protection keys in blob. If we keep Azure Blob, auth cookies continue to work across restarts. Keeping Azure Blob is essentially free (~$0.50-1/month) and avoids any code change.

3. **ImageService uses Azure SDK directly** — swapping blob providers requires a code change. Not worth it.

4. **Blazor Server is stateful** — each user has a persistent SignalR circuit. This is fine on a single VPS instance; sticky sessions are only needed when load-balancing multiple instances, which we are not doing.

5. **Already Dockerized** — the app has a Dockerfile and docker-compose.yml. Moving to a VPS with Docker is a near-zero-friction deployment model change.

### recommended setup

**Hetzner Cloud CAX11 ARM** (Falkenstein, Germany or Helsinki, Finland)

| spec | value |
|---|---|
| vCPU | 2 (Ampere ARM64) |
| RAM | 4 GB |
| Disk | 40 GB NVMe |
| Network | 20 TB/month |
| Price | €3.79/month (~$4.20) |
| Location | EU (GDPR compliant) |

The CAX11 matches the current Azure plan's 4 GB RAM but has significantly better CPU. Since the database stays in Azure SQL, the VPS only runs the app container — 4 GB is comfortable. Upgrade to CX32 (4 vCPU, 8 GB, €6.80/mo) later if needed.

ARM binaries: .NET 10 has first-class ARM64 support. The official `mcr.microsoft.com/dotnet/aspnet:10.0-azurelinux3.0` image builds and runs fine on ARM64. The existing Dockerfile works as-is, but the CD workflow's `docker/build-push-action` step needs `platforms: linux/arm64` added to produce an ARM64 image (see risk table below).

**Keep Azure SQL** — $5/month, zero migration effort, no risk.

**Keep Azure Blob Storage** — ~$1/month, zero code changes, data protection keys continue to work.

**Keep Azure Communication Email** — free, zero changes.

### deployment: Dokploy

Dokploy (https://dokploy.com) is an open-source self-hosted PaaS that runs on any VPS. It manages Docker/Docker Compose deployments, Traefik-based SSL (Let's Encrypt), environment variables, and deployment webhooks.

The existing GitHub Actions CD workflow already:
1. Runs tests
2. Builds and pushes the Docker image to `ghcr.io/nielspilgaard/minimoeder-website`
3. Deploys to Azure

Step 3 just gets replaced with a Dokploy webhook call. The image path and all other steps stay identical.

### ssl certificates

Dokploy uses Traefik with automatic Let's Encrypt certificate provisioning and renewal. No manual management needed.

### domain

Point `mini-moeder.dk` (and `www.mini-moeder.dk`) DNS A record to the Hetzner server IP. Dokploy configures Traefik to route requests to the app container.

### automated system updates

Install `unattended-upgrades` on Ubuntu 24.04 — handles automatic security patches for the OS:

```bash
apt install unattended-upgrades
dpkg-reconfigure --priority=low unattended-upgrades
```

App container updates are handled by the existing GitHub Actions CD pipeline (tag a release → pipeline runs → Dokploy pulls and redeploys the new image).

### observability

The app currently uses `openTelemetryBuilder.UseGrafana()` in production. Grafana Cloud has a free tier that covers a small app. No changes needed there.

### cost summary

| item | new cost |
|---|---|
| Hetzner CAX11 ARM (app only) | ~$4.20/month |
| Azure SQL (keep) | ~$5/month |
| Azure Blob Storage (keep) | ~$1/month |
| Azure Communication Email (keep) | $0 |
| Dokploy | $0 (open source) |
| SSL (Let's Encrypt via Dokploy) | $0 |
| **total** | **~$10-11/month** |

Current: ~$25/month → New: ~$10-11/month. **Savings: ~$14/month, ~56% reduction.**

---

## migration plan

### prerequisites

- [ ] Hetzner account
- [ ] SSH key added to Hetzner
- [ ] Note all production environment variables from Azure app settings (Azure SQL connection string stays the same)

### step 1: provision the server

1. Create Hetzner CAX11 ARM server (Helsinki or Falkenstein) running Ubuntu 24.04
2. Add firewall rules: allow 22 (SSH), 80 (HTTP), 443 (HTTPS)
3. Set up unattended-upgrades (security patches only, runs at 3am):
   ```bash
   apt update && apt install -y unattended-upgrades
   dpkg-reconfigure --priority=low unattended-upgrades

   # Schedule upgrades at 3:00am instead of the default ~6am
   mkdir -p /etc/systemd/system/apt-daily-upgrade.timer.d
   cat > /etc/systemd/system/apt-daily-upgrade.timer.d/override.conf << 'EOF'
   [Timer]
   OnCalendar=03:00
   RandomizedDelaySec=0
   EOF
   systemctl daemon-reload && systemctl restart apt-daily-upgrade.timer
   ```

   Safe: by default this only installs `security` and `security-updates` packages, never major upgrades. Auto-reboot is off unless you uncomment `Unattended-Upgrade::Automatic-Reboot "true"` in `/etc/apt/apt.conf.d/50unattended-upgrades` (kernel patches need a reboot to take effect, so worth enabling at 3am).
4. Install Dokploy:
   ```bash
   # Download the installer first, verify it, then execute
   curl -sSL https://dokploy.com/install.sh -o dokploy-install.sh
   # Verify SHA256 checksum matches the value published on https://dokploy.com/docs/get-started/installation
   sha256sum dokploy-install.sh
   # Inspect the script before running
   less dokploy-install.sh
   sh dokploy-install.sh
   ```
5. Access Dokploy at `http://<hetzner-ip>:3000`, create admin account

### step 2: configure the app service in Dokploy

**Prerequisite — GHCR registry credentials in Dokploy:**
Before creating the service, go to Dokploy → **Registry** and add a custom registry:
- Registry URL: `ghcr.io`
- Username: your GitHub username
- Password: a GitHub Personal Access Token (PAT) with `read:packages` scope

This is required so Dokploy can pull the image even if the GHCR package visibility is set to private. Do this before the first deployment attempt.

Create a new project with one service:

**Jordnaer app**
- Image: `ghcr.io/nielspilgaard/minimoeder-website:production`
- Domain: `mini-moeder.dk` and `www.mini-moeder.dk`
- SSL: enable Let's Encrypt in Dokploy
- Environment variables: copy all from current Azure app settings as-is
  - `ConnectionStrings__JordnaerDbContext` stays pointing to Azure SQL — no change
  - `ConnectionStrings__AzureBlobStorage` stays pointing to Azure Blob — no change

### step 3: wire up automated deployment

In the current [website_cd.yml](.github/workflows/website_cd.yml), replace the Azure deployment steps with a Dokploy webhook call:

```yaml
# Replace this block:
- name: Login to Azure
  uses: azure/login@v2
  with:
    creds: ${{ secrets.AZURE_CREDENTIALS }}

- name: Deploy to Azure Web App for Containers
  uses: azure/webapps-deploy@v3
  with:
    app-name: ${{ env.AZURE_WEBAPP_NAME }}
    images: ${{ env.REGISTRY }}/${{ env.IMAGE_NAME }}:production

# With this:
- name: Deploy via Dokploy API
  run: |
    curl --fail --show-error --silent \
      -X POST "https://<dokploy-host>/api/application.deploy" \
      -H "x-api-key: ${{ secrets.DOKPLOY_API_KEY }}" \
      -H "Content-Type: application/json" \
      -d '{"applicationId":"${{ secrets.DOKPLOY_APPLICATION_ID }}"}'
```

Add `DOKPLOY_API_KEY` and `DOKPLOY_APPLICATION_ID` to GitHub repository secrets. Remove `AZURE_CREDENTIALS` secret after decommission.

### step 4: DNS cutover

1. Reduce mini-moeder.dk TTL to 300 seconds (5 minutes) 24 hours before cutover
2. Verify the app works correctly on the Hetzner server (test via `/etc/hosts` override or Dokploy preview URL)
3. Update mini-moeder.dk DNS A record to point to the Hetzner server IP
4. Monitor for ~15 minutes to confirm traffic is routing correctly and SSL cert is issued
5. Restore TTL to a longer value (e.g., 3600)

### step 5: decommission Azure resources

After 48 hours of stable operation:
- Delete Azure Web App (mini-moeder)
- Keep Azure SQL (still in use)
- Keep Azure Blob Storage (still used for images + data protection keys)
- Keep Azure Communication Email (still used for transactional mail)

---

## risk mitigation

| risk | likelihood | mitigation |
|---|---|---|
| App memory pressure on VPS | low | CAX11 has 4 GB — same as current Azure plan but better CPU. Upgrade to CX32 (8 GB, €6.80/mo) if needed |
| SignalR circuit drops during deploy | mitigated | Rolling deploy requires Docker Swarm mode. **Step 2 sub-steps:** (1) On the server run `docker swarm init` to enable Swarm. (2) In Dokploy → Advanced → Cluster Settings → Swarm Settings, paste the following update_config and health-check JSON (adjust port if needed): `{"updateConfig":{"parallelism":1,"delay":"10s","order":"start-first"},"healthCheck":{"test":["CMD","curl","-f","http://localhost:8080/alive"],"interval":"10s","timeout":"5s","retries":3,"startPeriod":"30s"}}`. The new container must pass `/alive` before the old receives SIGTERM. The old container drains existing SignalR circuits for 30 s — this aligns with `HostOptions.ShutdownTimeout = TimeSpan.FromSeconds(30)` already set in `Program.cs`. `boot.js` retries every 5 s; if `Blazor.reconnect()` returns false, the page reloads and the user lands on the new container with the auth cookie intact. |
| ARM compatibility issue with .NET app | low | .NET 10 has first-class ARM64 support. The existing Dockerfile works as-is |
| OAuth callback URLs break | low | Domain stays the same (mini-moeder.dk) so OAuth redirect URIs don't change |
| Let's Encrypt rate limit | low | Keep port 80 open so ACME HTTP-01 challenge works |
| VPS outage (no redundancy) | low/medium | Hetzner SLA is 99.9%. Same single-point-of-failure model as current Azure Web App |
| Docker image ARM build | low | Add `platforms: linux/arm64` to the `docker/build-push-action` step in `website_cd.yml`. Or skip ARM and use Hetzner CX22 x86 (€4.49/mo, negligible cost difference) |
| Azure SQL latency from Hetzner | low | Azure SQL is in West Europe; Hetzner Helsinki/Falkenstein are both low latency to that region. Same as today since the app already calls out to Azure SQL over the internet |

### safest rollback path

Azure Web App stays running until the cutover is confirmed stable. If something goes wrong, DNS can be pointed back to Azure within the TTL window (~5 minutes after TTL reduction). Azure costs nothing extra during the parallel period (it was already paid for the month). Azure SQL is untouched throughout — no rollback needed for the database.

---

## alternatives considered

| option | verdict |
|---|---|
| **Scaleway** | More expensive than Hetzner for equivalent specs. DEV1-S (2 vCPU, 2 GB) is ~€8/mo for less RAM. Hetzner wins on price/performance |
| **Hetzner CX22 x86** | €4.49/mo vs €3.79/mo for CAX11 ARM. Avoids any ARM build concern. Either works; ARM is cheaper and .NET 10 supports it well |
| **Migrate from SQL Server to PostgreSQL** | Would eliminate the $5/month Azure SQL cost. But requires rewriting all EF Core migrations, switching providers, and retesting spatial queries. Not worth it |
| **Switch blob storage to Hetzner Object Storage** | €3/month vs ~$1/month Azure. Costs more and requires code changes in ImageService + data protection configuration. Skip |
| **Coolify** (alternative to Dokploy) | Also open source and mature. More feature-rich but heavier. Either works; Dokploy is simpler for this use case |
