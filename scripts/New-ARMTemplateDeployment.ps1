#Requires -Modules @{ ModuleName="AzureRM.Resources"; ModuleVersion="5.0.0" }

[CmdletBinding()]

param(
  [Parameter(Mandatory = $False)]
  [string]
  $Location = 'East US',

  [Parameter(Mandatory = $True)]
  [string]
  $WebAppName,

  [Parameter(Mandatory = $False)]
  [ValidateSet("F1 Free", "S1 Standard")]
  [string]
  $Sku = "F1 Free",

  [Parameter(Mandatory = $True)]
  [string]
  $GraphDomain,

  [Parameter(Mandatory = $True)]
  [string]
  $GraphApplicationId,

  [Parameter(Mandatory = $True)]
  [string]
  $GraphApplicationSecret,

  [Parameter(Mandatory = $True)]
  [string]
  $SlackSlashCommandToken,

  [switch]
  $TestOnly
)

Push-Location
Set-Location $PSScriptRoot

$applicationName = "who-here"
$resourceGroupName = "$applicationName-rg"
$templateFile = ".\$applicationName.template.json"

Write-Verbose "Deployment configuration:`n  applicationName: $applicationName`n  resourceGroupName: $resourceGroupName`n  templateFile: $templateFile"

Write-Verbose "Initializing Resource Group"
$resourceGroup = Get-AzureRmResourceGroup -Name $resourceGroupName -ErrorAction SilentlyContinue
if (!$resourceGroup) {
  New-AzureRmResourceGroup -Name $resourceGroupName -Location $location -Tags $tags
}

if ($TestOnly) {
  Write-Verbose "Testing ARM template"
  Test-AzureRmResourceGroupDeployment `
    -ResourceGroupName "$resourceGroupName" `
    -TemplateFile $templateFile `
    -webAppName $WebAppName `
    -sku $Sku `
    -graphDomain $GraphDomain `
    -graphApplicationId $GraphApplicationId `
    -graphApplicationSecret $GraphApplicationSecret `
    -slackSlashCommandToken $SlackSlashCommandToken
}
else {
  Write-Verbose "Deploying ARM template"
  $outputs = (New-AzureRmResourceGroupDeployment `
      -ResourceGroupName "$resourceGroupName" `
      -TemplateFile $templateFile `
      -webAppName $WebAppName `
      -sku $Sku `
      -graphDomain $GraphDomain `
      -graphApplicationId $GraphApplicationId `
      -graphApplicationSecret $GraphApplicationSecret `
      -slackSlashCommandToken $SlackSlashCommandToken
  ).Outputs
  Write-Verbose "ARM template deployment is complete"
}

Pop-Location
