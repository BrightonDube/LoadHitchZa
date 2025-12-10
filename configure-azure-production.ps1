# Azure App Service Configuration Script
# Run this script to configure optimal production settings for LoadHitch

$appName = "loadhitch"
$resourceGroup = "LoadHitch"

Write-Host "=== Configuring Azure App Service for Production ===" -ForegroundColor Cyan

# 1. Enable Always On (prevents app from sleeping)
Write-Host "`n[1/10] Enabling Always On..." -ForegroundColor Yellow
az webapp config set --name $appName --resource-group $resourceGroup --always-on true
Write-Host "✓ Always On enabled" -ForegroundColor Green

# 2. Disable ARR Affinity (better for stateless apps and SignalR scale-out)
Write-Host "`n[2/10] Disabling ARR Affinity..." -ForegroundColor Yellow
az webapp update --name $appName --resource-group $resourceGroup --client-affinity-enabled false
Write-Host "✓ ARR Affinity disabled" -ForegroundColor Green

# 3. Enable HTTPS Only
Write-Host "`n[3/10] Enabling HTTPS Only..." -ForegroundColor Yellow
az webapp update --name $appName --resource-group $resourceGroup --https-only true
Write-Host "✓ HTTPS Only enabled" -ForegroundColor Green

# 4. Set minimum TLS version to 1.2
Write-Host "`n[4/10] Setting minimum TLS version to 1.2..." -ForegroundColor Yellow
az webapp config set --name $appName --resource-group $resourceGroup --min-tls-version 1.2
Write-Host "✓ Minimum TLS 1.2 configured" -ForegroundColor Green

# 5. Enable HTTP/2
Write-Host "`n[5/10] Enabling HTTP/2..." -ForegroundColor Yellow
az webapp config set --name $appName --resource-group $resourceGroup --http20-enabled true
Write-Host "✓ HTTP/2 enabled" -ForegroundColor Green

# 6. Enable WebSocket for SignalR
Write-Host "`n[6/10] Enabling WebSockets..." -ForegroundColor Yellow
az webapp config set --name $appName --resource-group $resourceGroup --web-sockets-enabled true
Write-Host "✓ WebSockets enabled" -ForegroundColor Green

# 7. Disable FTP
Write-Host "`n[7/10] Disabling FTP..." -ForegroundColor Yellow
az webapp config set --name $appName --resource-group $resourceGroup --ftps-state Disabled
Write-Host "✓ FTP disabled" -ForegroundColor Green

# 8. Enable detailed error pages (production-safe)
Write-Host "`n[8/10] Configuring error handling..." -ForegroundColor Yellow
az webapp config set --name $appName --resource-group $resourceGroup --detailed-error-logging-enabled false
az webapp config set --name $appName --resource-group $resourceGroup --http-logging-enabled true
Write-Host "✓ Error handling configured" -ForegroundColor Green

# 9. Configure health check
Write-Host "`n[9/10] Configuring health check..." -ForegroundColor Yellow
az webapp config set --name $appName --resource-group $resourceGroup --health-check-path "/health"
Write-Host "✓ Health check configured at /health" -ForegroundColor Green

# 10. Set environment to Production
Write-Host "`n[10/10] Setting environment to Production..." -ForegroundColor Yellow
az webapp config appsettings set --name $appName --resource-group $resourceGroup --settings ASPNETCORE_ENVIRONMENT=Production
Write-Host "✓ Production environment set" -ForegroundColor Green

Write-Host "`n=== Configuration Complete ===" -ForegroundColor Cyan
Write-Host "Your app is now optimized for production!" -ForegroundColor Green
Write-Host "`nNext steps:" -ForegroundColor Yellow
Write-Host "1. Configure Application Insights for monitoring" -ForegroundColor White
Write-Host "2. Set up automated backups" -ForegroundColor White
Write-Host "3. Configure custom domain (optional)" -ForegroundColor White
Write-Host "4. Enable Azure CDN for static files (optional)" -ForegroundColor White
Write-Host "`nApp URL: https://$appName.azurewebsites.net" -ForegroundColor Cyan
Write-Host "Health Check: https://$appName.azurewebsites.net/health" -ForegroundColor Cyan
