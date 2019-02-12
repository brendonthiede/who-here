#Requires -Modules @{ ModuleName="AzureRM.Resources"; ModuleVersion="5.0.0" }

[CmdletBinding()]

param(
  [Parameter(Mandatory = $False)]
  [string]
  $Location = 'East US',

  [Parameter(Mandatory = $False)]
  [string]
  $ResourceGroupName = 'who-here-rg',

  [Parameter(ParameterSetName = 'ParametersFileOptions', Mandatory = $True)]
  [string]
  $ParametersFile,

  [Parameter(ParameterSetName = 'CommandLineOptions', Mandatory = $True)]
  [string]
  $WebAppName,

  [Parameter(ParameterSetName = 'CommandLineOptions', Mandatory = $False)]
  [ValidateSet("F1 Free", "S1 Standard")]
  [string]
  $Sku = "F1 Free",

  [Parameter(ParameterSetName = 'CommandLineOptions', Mandatory = $True)]
  [string]
  $GraphDomain,

  [Parameter(ParameterSetName = 'CommandLineOptions', Mandatory = $True)]
  [string]
  $GraphApplicationId,

  [Parameter(ParameterSetName = 'CommandLineOptions', Mandatory = $True)]
  [string]
  $GraphApplicationSecret,

  [Parameter(ParameterSetName = 'CommandLineOptions', Mandatory = $True)]
  [string]
  $SlackSlashCommandToken,

  [switch]
  $TestOnly
)

$templateFile = "$PSScriptRoot\who-here.template.json"

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

switch ($PsCmdlet.ParameterSetName) {
  "ParametersFileOptions" {
    & $ARMCommand `
      -ResourceGroupName "$ResourceGroupName" `
      -TemplateFile $templateFile `
      -TemplateParameterFile $ParametersFile
  }
  "CommandLineOptions" {
    & $ARMCommand `
      -ResourceGroupName "$ResourceGroupName" `
      -TemplateFile $templateFile `
      -webAppName $WebAppName `
      -sku $Sku `
      -graphDomain $GraphDomain `
      -graphApplicationId $GraphApplicationId `
      -graphApplicationSecret $GraphApplicationSecret `
      -slackSlashCommandToken $SlackSlashCommandToken
  }
}
