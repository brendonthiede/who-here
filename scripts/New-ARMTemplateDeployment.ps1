#Requires -Modules @{ ModuleName="AzureRM.Resources"; ModuleVersion="5.0.0" }

[CmdletBinding()]

param(
  [Parameter(Mandatory = $False)]
  [string]
  $Location = 'East US',

  [Parameter(Mandatory = $False)]
  [string]
  $ResourceGroupName = 'who-here-rg',

  [Parameter(Mandatory = $False)]
  [string]
  $ParametersFile = "$PSScriptRoot\who-here.parameters.json",

  [switch]
  $TestOnly
)

$templateFile = "$PSScriptRoot\who-here.template.json"
if (-not (Test-Path $ParametersFile)) {
  throw "Parameters file $ParametersFile does not exist"
}

Write-Verbose "Deployment configuration:`n  resourceGroupName: $ResourceGroupName`n  templateFile: $templateFile`n  ParametersFile: $ParametersFile"

Write-Verbose "Initializing Resource Group"
$resourceGroup = Get-AzureRmResourceGroup -Name $ResourceGroupName -ErrorAction SilentlyContinue
if (!$resourceGroup) {
  New-AzureRmResourceGroup -Name $ResourceGroupName -Location $location -Tags $tags
}

if ($TestOnly) {
  $ARMCommand = "Test-AzureRmResourceGroupDeployment"
}
else {
  $ARMCommand = "New-AzureRmResourceGroupDeployment"
}

& $ARMCommand `
  -ResourceGroupName "$ResourceGroupName" `
  -TemplateFile $templateFile `
  -TemplateParameterFile $ParametersFile
