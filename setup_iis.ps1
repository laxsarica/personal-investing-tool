# Run this script as Administrator

$apiPath = "d:\MyStockMarketTool\ScreenEdge.Api\bin\Release\net8.0\publish\"
$webPath = "d:\MyStockMarketTool\ScreenEdge.Web\ClientApp\dist\ClientApp\browser\"

# 1. Enable IIS Features
Write-Host "Enabling IIS Features (This might take a few minutes)..."
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServer
Enable-WindowsOptionalFeature -Online -FeatureName IIS-CommonHttpFeatures
Enable-WindowsOptionalFeature -Online -FeatureName IIS-StaticContent
Enable-WindowsOptionalFeature -Online -FeatureName IIS-DefaultDocument
Enable-WindowsOptionalFeature -Online -FeatureName IIS-HttpErrors
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ApplicationDevelopment
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ISAPIExtensions
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ISAPIFilter
Enable-WindowsOptionalFeature -Online -FeatureName IIS-NetFxExtensibility45
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ASPNET45
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ManagementConsole

# 2. Build and Publish the Projects
Write-Host "Publishing .NET API..."
dotnet publish d:\MyStockMarketTool\ScreenEdge.Api\ScreenEdge.Api.csproj -c Release

Write-Host "Building Angular App..."
Set-Location d:\MyStockMarketTool\ScreenEdge.Web\ClientApp
npm run build
Set-Location d:\MyStockMarketTool

# 3. Import WebAdministration module to manage IIS
Import-Module WebAdministration

# 4. Setup API Site (Port 5100)
$apiPoolName = "ScreenEdgeApiPool"
$apiSiteName = "ScreenEdgeApi"

if (!(Test-Path "IIS:\AppPools\$apiPoolName")) {
    New-WebAppPool -Name $apiPoolName
    Set-ItemProperty "IIS:\AppPools\$apiPoolName" -Name "managedRuntimeVersion" -Value ""
    Set-ItemProperty "IIS:\AppPools\$apiPoolName" -Name "processModel.idleTimeout" -Value "00:00:00" # Prevent AppPool from sleeping (Crucial for Hangfire)
}

if (Test-Path "IIS:\Sites\$apiSiteName") {
    Remove-WebSite -Name $apiSiteName
}
New-WebSite -Name $apiSiteName -Port 5100 -PhysicalPath $apiPath -ApplicationPool $apiPoolName
Write-Host "API Site created on http://localhost:5100"

# 5. Setup Web Site (Port 4200)
$webPoolName = "ScreenEdgeWebPool"
$webSiteName = "ScreenEdgeWeb"

if (!(Test-Path "IIS:\AppPools\$webPoolName")) {
    New-WebAppPool -Name $webPoolName
}

if (Test-Path "IIS:\Sites\$webSiteName") {
    Remove-WebSite -Name $webSiteName
}
New-WebSite -Name $webSiteName -Port 4200 -PhysicalPath $webPath -ApplicationPool $webPoolName
Write-Host "Web Site created on http://localhost:4200"

Write-Host "IIS Setup Complete!"
Write-Host "Please ensure you have installed the .NET 8 Hosting Bundle from Microsoft."
Write-Host "If things don't load immediately, run 'iisreset' in this terminal."
