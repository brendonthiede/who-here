properties {
  $Configuration,
  $Location,
  $ResourceGroupName,
  $WebAppName
}

$appProject = "$PSScriptRoot\..\WhoHere\WhoHere.csproj"
$testProject = "$PSScriptRoot\..\WhoHere.Tests\WhoHere.Tests.csproj"
$distFolder = "$PSScriptRoot\..\dist"
$zipName = "WhoHere.zip"

# Default task to run
task default -depends Publish

Task Requires.DotNet {
  $script:dotnetExe = (Get-Command dotnet -ErrorAction SilentlyContinue).Source
  $dotnetVersion = (Get-Command dotnet -ErrorAction SilentlyContinue).Version

  if ($dotnetExe -eq $null -or $dotnetVersion.Major -ne 2 -or $dotnetVersion.Minor -lt 1) {
    throw "Failed to find dotnet CLI 2.1"
  }

  Write-Verbose "Found dotnet version $($dotnetVersion.ToString()) here: $dotnetExe"
}

# Run unit tests
task Test -depends Requires.DotNet {
  Exec { & $dotnetExe test "$testProject" -c $Configuration }
}

# Build the application
task Build -depends Test {
  Exec { & $dotnetExe build "$appProject" -c $Configuration }
}

# Starts the dev server (it's actually preferable to do this from your IDE)
task Run -depends Test {
  Exec { & $dotnetExe run --project "$appProject" -c $Configuration }
}

# Creates the artifacts for publishing
task Publish -depends Test {
  if (Test-Path $distFolder) {
    Remove-Item -Recurse -Force $distFolder
  }
  Exec { & $dotnetExe publish "$appProject" -c $Configuration -o "$distFolder" }
  Exec { Compress-Archive -Path "$distFolder\*" -DestinationPath "$distFolder\$zipName" }
}

task DeployInfrastructure {
  Exec { & "$PSScriptRoot\New-ARMTemplateDeployment.ps1" -Location $Location -ResourceGroupName $ResourceGroupName }
}

task DeployWebApp -depends Publish {
  if (-not $WebAppName) {
    $WebAppName = (Get-AzureRmResource -ResourceGroupName $resourceGroupName -ResourceType 'Microsoft.Web/sites')[0].Name
  }
  $publishProfile = ([xml](Get-AzureRMWebAppPublishingProfile -ResourceGroupName $resourceGroupName  -Name $WebAppName)).publishData.publishProfile[0]

  $username = "$($publishProfile.userName)"
  $password = "$($publishProfile.userPWD)"
  $filePath = "$distFolder\$zipName"
  $apiUrl = "https://$WebAppName.scm.azurewebsites.net/api/zipdeploy"
  $base64AuthInfo = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes(("{0}:{1}" -f $username, $password)))
  $userAgent = "powershell/1.0"
  Invoke-RestMethod -Uri $apiUrl -Headers @{Authorization = ("Basic {0}" -f $base64AuthInfo)} -UserAgent $userAgent -Method POST -InFile $filePath -ContentType "multipart/form-data"
}
