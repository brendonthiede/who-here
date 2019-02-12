properties {
  $Configuration
}

# Default task to run
task default -depends Publish

Task Requires.DotNet {
  $script:dotnetExe = (Get-Command dotnet).Source
  $dotnetVersion = (Get-Command dotnet).Version

  if ($dotnetExe -eq $null -or $dotnetVersion.Major -ne 2 -or $dotnetVersion.Minor -lt 1) {
    throw "Failed to find dotnet CLI 2.1"
  }

  Write-Verbose "Found dotnet version $($dotnetVersion.ToString()) here: $dotnetExe"
}

# Run unit tests
task Test -depends Requires.DotNet {
  Exec { & $dotnetExe test "$PSScriptRoot\..\WhoHere.Tests\WhoHere.Tests.csproj" -c $Configuration }
}

# Build the application
task Build -depends Test {
  Exec { & $dotnetExe build "$PSScriptRoot\..\WhoHere\WhoHere.csproj" -c $Configuration }
}

# Starts the dev server (it's actually preferable to do this from your IDE)
task Run -depends Test {
  Exec { & $dotnetExe run --project "$PSScriptRoot\..\WhoHere\WhoHere.csproj" -c $Configuration }
}

# Creates the artifacts for publishing
task Publish -depends Test {
  Exec { & $dotnetExe publish "$PSScriptRoot\..\WhoHere\WhoHere.csproj" -c $Configuration -o "$PSScriptRoot\..\dist" }
}
