<#
.SYNOPSIS
  Creates and deploys the SWA -> linked APIM -> App Service POC with the Azure CLI.

.NOTES
  Review before running. Every step can also be done in the Azure portal (see README.md).
  Run from the repo root:  ./infra/deploy.ps1 -Prefix aitopoc -TenantId <guid> -ClientId <guid> -PublisherEmail you@company.com
  Prerequisites: az login, .NET 9 SDK, Node 20+.
#>
param(
    [Parameter(Mandatory)] [string] $Prefix,            # lowercase letters/numbers, used in resource names
    [Parameter(Mandatory)] [string] $TenantId,
    [Parameter(Mandatory)] [string] $ClientId,          # the existing app registration
    [Parameter(Mandatory)] [string] $PublisherEmail,    # required when creating a new APIM
    [string] $ResourceGroup = "rg-$Prefix",
    [string] $Location = "eastus2",                     # SWA Standard regions: westus2, centralus, eastus2, westeurope, eastasia
    [string] $ExistingApimName = "",                    # set to reuse an existing APIM instead of creating a Consumption one
    [string] $ExistingApimResourceGroup = ""
)

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent

$planName   = "plan-$Prefix"
$webAppName = "app-$Prefix-api"
$swaName    = "swa-$Prefix"
$apiId      = "swa-apim-poc"

Write-Host "1/9 Resource group" -ForegroundColor Cyan
az group create -n $ResourceGroup -l $Location -o none

Write-Host "2/9 App Service (Linux, .NET 9)" -ForegroundColor Cyan
az appservice plan create -g $ResourceGroup -n $planName --sku B1 --is-linux -o none
az webapp create -g $ResourceGroup -p $planName -n $webAppName --runtime "DOTNETCORE:9.0" -o none
az webapp update -g $ResourceGroup -n $webAppName --https-only true -o none

# Shared secret: APIM adds it to every request, the API rejects requests without it.
$gatewaySecret = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(32))
az webapp config appsettings set -g $ResourceGroup -n $webAppName --settings "Gateway__Secret=$gatewaySecret" -o none

Write-Host "3/9 API Management" -ForegroundColor Cyan
if ($ExistingApimName) {
    $apimName = $ExistingApimName
    $apimRg = $ExistingApimResourceGroup
} else {
    $apimName = "apim-$Prefix"
    $apimRg = $ResourceGroup
    az apim create -g $apimRg -n $apimName -l $Location --sku-name Consumption `
        --publisher-email $PublisherEmail --publisher-name "$Prefix POC" -o none
}

az apim nv create -g $apimRg --service-name $apimName --named-value-id poc-gateway-secret `
    --display-name poc-gateway-secret --value $gatewaySecret --secret true -o none

Write-Host "4/9 APIM API (suffix 'api', wildcard GET/POST)" -ForegroundColor Cyan
az apim api create -g $apimRg --service-name $apimName --api-id $apiId --path api `
    --display-name "SWA APIM POC" --service-url "https://$webAppName.azurewebsites.net/api" `
    --protocols https --subscription-required true -o none
az apim api operation create -g $apimRg --service-name $apimName --api-id $apiId `
    --operation-id get-all --display-name "GET all" --method GET --url-template "/*" -o none
az apim api operation create -g $apimRg --service-name $apimName --api-id $apiId `
    --operation-id post-all --display-name "POST all" --method POST --url-template "/*" -o none

$apimId = az apim show -g $apimRg -n $apimName --query id -o tsv
$policyXml = Get-Content (Join-Path $root "apim/api-policy.xml") -Raw
$policyBody = @{ properties = @{ format = "rawxml"; value = $policyXml } } | ConvertTo-Json -Depth 5 -Compress
$policyFile = New-TemporaryFile
Set-Content $policyFile $policyBody -Encoding utf8
az rest --method put `
    --url "https://management.azure.com$apimId/apis/$apiId/policies/policy?api-version=2022-08-01" `
    --body "@$policyFile" -o none
Remove-Item $policyFile

Write-Host "5/9 Static Web App (Standard)" -ForegroundColor Cyan
az staticwebapp create -g $ResourceGroup -n $swaName -l $Location --sku Standard -o none
$swaHost = az staticwebapp show -g $ResourceGroup -n $swaName --query defaultHostname -o tsv

$clientSecret = Read-Host "Paste the client secret created for this POC on the app registration" -AsSecureString
$clientSecretPlain = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [Runtime.InteropServices.Marshal]::SecureStringToBSTR($clientSecret))
az staticwebapp appsettings set -g $ResourceGroup -n $swaName `
    --setting-names "AZURE_CLIENT_ID=$ClientId" "AZURE_CLIENT_SECRET=$clientSecretPlain" -o none
$clientSecretPlain = $null

Write-Host "6/9 Link APIM to the Static Web App" -ForegroundColor Cyan
az staticwebapp backends link -g $ResourceGroup -n $swaName `
    --backend-resource-id $apimId --backend-region $Location -o none

Write-Host "7/9 Add the API to the product that linking created" -ForegroundColor Cyan
$productId = az apim product list -g $apimRg --service-name $apimName `
    --query "[?contains(displayName, '$swaHost')].name | [0]" -o tsv
if (-not $productId) { throw "Linked product not found. Check APIM > Products, then add the API manually." }
az apim product api add -g $apimRg --service-name $apimName --product-id $productId --api-id $apiId -o none

Write-Host "8/9 Deploy the .NET API" -ForegroundColor Cyan
$publishDir = Join-Path $root "backend/publish"
dotnet publish (Join-Path $root "backend/src/SwaApimPoc.Api") -c Release -o $publishDir
$zip = Join-Path $root "backend/publish.zip"
if (Test-Path $zip) { Remove-Item $zip }
Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zip
az webapp deploy -g $ResourceGroup -n $webAppName --src-path $zip --type zip -o none

Write-Host "9/9 Build and deploy the frontend" -ForegroundColor Cyan
$frontend = Join-Path $root "frontend"
@"
VITE_AZURE_CLIENT_ID=$ClientId
VITE_AZURE_TENANT_ID=$TenantId
"@ | Set-Content (Join-Path $frontend ".env.production") -Encoding utf8
(Get-Content (Join-Path $frontend "public/staticwebapp.config.json") -Raw).Replace("<TENANT_ID>", $TenantId) |
    Set-Content (Join-Path $frontend "public/staticwebapp.config.json") -Encoding utf8
Push-Location $frontend
npm ci
npm run build
$token = az staticwebapp secrets list -g $ResourceGroup -n $swaName --query properties.apiKey -o tsv
npx swa deploy ./dist --deployment-token $token --env production
Pop-Location

Write-Host ""
Write-Host "Done." -ForegroundColor Green
Write-Host "  SWA:         https://$swaHost"
Write-Host "  APIM:        https://$apimName.azure-api.net/api/health"
Write-Host "  App Service: https://$webAppName.azurewebsites.net/api/health"
Write-Host ""
Write-Host "Add these to the app registration if you haven't yet:" -ForegroundColor Yellow
Write-Host "  Web redirect URI: https://$swaHost/.auth/login/aad/callback"
Write-Host "  SPA redirect URI: https://$swaHost/redirect.html"
