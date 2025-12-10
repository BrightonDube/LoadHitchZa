# LoadHitch Azure Deployment Script
# This script deploys the LoadHitch application to Azure App Service

# Exit on any error
$ErrorActionPreference = "Stop"

Write-Host "=== LoadHitch Azure Deployment ===" -ForegroundColor Cyan

# Configuration
$appName = "loadhitch"
$resourceGroup = "LoadHitch"

# Step 1: Verify Azure CLI is installed and logged in
Write-Host "`n[1/6] Verifying Azure CLI..." -ForegroundColor Yellow
try {
    $account = az account show 2>&1 | ConvertFrom-Json
    Write-Host "✓ Logged in as: $($account.user.name)" -ForegroundColor Green
} catch {
    Write-Host "✗ Not logged in to Azure. Running 'az login'..." -ForegroundColor Red
    az login
    if ($LASTEXITCODE -ne 0) {
        Write-Host "✗ Azure login failed. Please try again." -ForegroundColor Red
        exit 1
    }
}

# Step 2: Clean previous build artifacts
Write-Host "`n[2/6] Cleaning previous build artifacts..." -ForegroundColor Yellow
if (Test-Path "./publish") {
    Remove-Item "./publish" -Recurse -Force
    Write-Host "✓ Removed old publish folder" -ForegroundColor Green
}
if (Test-Path "./deploy.zip") {
    Remove-Item "./deploy.zip" -Force
    Write-Host "✓ Removed old deploy.zip" -ForegroundColor Green
}

# Step 3: Build and publish the application
Write-Host "`n[3/6] Building and publishing application..." -ForegroundColor Yellow
dotnet publish -c Release -o ./publish
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Build failed. Please fix errors and try again." -ForegroundColor Red
    exit 1
}
Write-Host "✓ Build completed successfully" -ForegroundColor Green

# Step 4: Remove test projects and unnecessary files
Write-Host "`n[4/6] Cleaning publish directory..." -ForegroundColor Yellow
$itemsToRemove = @(
    "./publish/t12Project.Tests",
    "./publish/t12Project.Playwright",
    "./publish/.playwright",
    "./publish/CodeCoverage",
    "./publish/latestlogs"
)

foreach ($item in $itemsToRemove) {
    if (Test-Path $item) {
        Remove-Item $item -Recurse -Force -ErrorAction SilentlyContinue
        Write-Host "✓ Removed $item" -ForegroundColor Green
    }
}

# Step 5: Create deployment zip
Write-Host "`n[5/6] Creating deployment package..." -ForegroundColor Yellow
Compress-Archive -Path ./publish/* -DestinationPath ./deploy.zip -Force
if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Failed to create deployment zip" -ForegroundColor Red
    exit 1
}
$zipSize = (Get-Item ./deploy.zip).Length / 1MB
Write-Host "✓ Created deploy.zip ($([math]::Round($zipSize, 2)) MB)" -ForegroundColor Green

# Step 6: Deploy to Azure
Write-Host "`n[6/6] Deploying to Azure Web App..." -ForegroundColor Yellow
Write-Host "App Name: $appName" -ForegroundColor Cyan
Write-Host "Resource Group: $resourceGroup" -ForegroundColor Cyan

az webapp deploy --name $appName --resource-group $resourceGroup --src-path ./deploy.zip --type zip --async true

if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Deployment failed" -ForegroundColor Red
    exit 1
}

Write-Host "`n✓ Deployment completed successfully!" -ForegroundColor Green

# Step 7: Restart the app to ensure changes are loaded
Write-Host "`n[7/7] Restarting web app..." -ForegroundColor Yellow
az webapp restart --name $appName --resource-group $resourceGroup
Write-Host "✓ Web app restarted" -ForegroundColor Green

# Display success message
Write-Host "`n=== Deployment Complete ===" -ForegroundColor Cyan
Write-Host "Your app is live at: https://$appName.azurewebsites.net" -ForegroundColor Green
Write-Host "`nCredentials:" -ForegroundColor Yellow
Write-Host "  Admin:    admin@loadhitch.com / Admin@123456!" -ForegroundColor White
Write-Host "  Driver:   driver1@loadhitch.com / Driver@123456!" -ForegroundColor White
Write-Host "  Customer: customer1@loadhitch.com / Customer@123456!" -ForegroundColor White

Write-Host "`nTo view logs, run:" -ForegroundColor Yellow
Write-Host "  az webapp log tail --name $appName --resource-group $resourceGroup" -ForegroundColor White
