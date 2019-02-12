[cmdletbinding()]
param(
    [Parameter(Mandatory=$False)]
    [ValidateSet("Test", "Build", "Publish", "Run")]
    [string[]]
    $Task = 'Publish',

    [Parameter(Mandatory=$False)]
    [ValidateSet("Debug", "Release")]
    [string]
    $Configuration = 'Debug'
)

# Verify that we have PackageManagement module installed
if (!(Get-Command Install-Module)) {
    throw 'PackageManagement is not installed. You need PowerShell 5+ or https://www.microsoft.com/en-us/download/details.aspx?id=51451'
}

# Verify that our testing utilities are installed.
if (!(Get-Module -Name AzureRM -ListAvailable)) {
    Write-Verbose "Installing AzureRM PowerShell module"
    Install-Module -Name AzureRM -Force -Scope CurrentUser
}
if (!(Get-Module -Name psake -ListAvailable)) {
    Write-Verbose "Installing Psake PowerShell module"
    Install-Module -Name Psake -Force -Scope CurrentUser
}

# Ensure that all testing modules are at the required minimum version
if (((Get-Module -Name AzureRM -ListAvailable)[0].Version.Major) -lt 5 -or (((Get-Module -Name AzureRM -ListAvailable)[0].Version.Major) -eq 5 -and ((Get-Module -Name AzureRM -ListAvailable)[0].Version.Minor) -lt 2)) {
    Write-Verbose "Upgrading AzureRM PowerShell module"
    Install-Module -Name AzureRM -SkipPublisherCheck -Force -Scope CurrentUser
}
Import-Module Psake
if (((Get-Module -Name Psake).Version.Major) -lt 4) {
    Write-Verbose "Upgrading Psake PowerShell module"
    Install-Module -Name Psake -SkipPublisherCheck -Force -Scope CurrentUser
}

$azureRMVersion = (Get-Module -Name AzureRM -ListAvailable)[0].Version
$azureRMMajorVersion = $azureRMVersion.Major
$azureRMMinorVersion = $azureRMVersion.Minor
$azureRMBuildVersion = $azureRMVersion.Build

$psakeVersion = (Get-Module -Name Psake).Version
$psakeMajorVersion = $psakeVersion.Major
$psakeMinorVersion = $psakeVersion.Minor
$psakeBuildVersion = $psakeVersion.Build

Write-Verbose "Current tool versions:"
Write-Verbose "  AzureRM:           $azureRMMajorVersion.$azureRMMinorVersion.$azureRMBuildVersion"
Write-Verbose "  Psake:             $psakeMajorVersion.$psakeMinorVersion.$psakeBuildVersion"
Write-Verbose ""

Invoke-psake -buildFile "$PSScriptRoot\psakeBuild.ps1" -taskList $Task -parameters @{"Configuration"=$Configuration} -Verbose:$VerbosePreference -Debug:$DebugPreference

if (!$psake.build_success) {
  Write-Error "PSake build failed"
  exit 1
}