[CmdletBinding()]
param (
  [Parameter(Mandatory = $True)]
  [string]
  $WebAppName
)

dotnet test ..\WhoHere.Tests\WhoHere.Tests.csproj
