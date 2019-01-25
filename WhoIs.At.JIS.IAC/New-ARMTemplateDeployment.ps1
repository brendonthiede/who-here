[CmdletBinding()]

param(
  [string] $JisEnvironment = (Get-CurrentJISEnvironment),
  [switch] $TestOnly
)

Import-Module JISReleaseTools

Push-Location
Set-Location $PSScriptRoot

$applicationName = "whoisatjis"
$resourceGroupShortName = "$applicationName"
$templateFile = ".\$applicationName.template.json"
$templateParameterFile = ".\$applicationName.parameters.$JisEnvironment.json"

Write-Verbose "Deployment configuration:`n  applicationName: $applicationName`n  resourceGroupShortName: $resourceGroupShortName`n  templateFile: $templateFile`n  templateParameterFile: $templateParameterFile"

Write-Verbose "Initializing Resource Groups"
Initialize-JISResourceGroup -ResourceGroupShortName $resourceGroupShortName -ResourceGroupType "persistence"
Initialize-JISResourceGroup -ResourceGroupShortName $resourceGroupShortName
$mainResourceGroupName = (Get-JISResourceGroupName -ResourceGroupShortName $resourceGroupShortName)

if ($TestOnly) {
  Write-Verbose "Testing ARM template"
  Test-AzureRmResourceGroupDeployment `
    -ResourceGroupName "$mainResourceGroupName" `
    -TemplateFile $templateFile `
    -TemplateParameterFile $templateParameterFile
}
else {
  Write-Verbose "Deploying ARM template"
  $outputs = (New-AzureRmResourceGroupDeployment `
      -ResourceGroupName "$mainResourceGroupName" `
      -TemplateFile $templateFile `
      -TemplateParameterFile $templateParameterFile `
  ).Outputs
  Write-Verbose "ARM template deployment is complete"
}

Pop-Location
