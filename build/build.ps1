[CmdletBinding(PositionalBinding=$false)]
Param(
  [string] $configuration = "Debug",
  [string] $solution = "",
  [string] $verbosity = "minimal",
  [switch] $restore,
  [switch] $deployDeps,
  [switch] $build,
  [switch] $rebuild,
  [switch] $deploy,
  [switch] $test,
  [switch] $integrationTest,
  [switch] $sign,
  [switch] $pack,
  [switch] $ci,
  [switch] $clearCaches,
  [Parameter(ValueFromRemainingArguments=$true)][String[]]$properties
)

set-strictmode -version 2.0
$ErrorActionPreference = "Stop"

$RepoRoot = Join-Path $PSScriptRoot "..\"
$ToolsRoot = Join-Path $RepoRoot ".tools"
$BuildProj = Join-Path $PSScriptRoot "build.proj"
$DependenciesProps = Join-Path $PSScriptRoot "Versions.props"
$ArtifactsDir = Join-Path $RepoRoot "artifacts"
$LogDir = Join-Path (Join-Path $ArtifactsDir $configuration) "log"
$TempDir = Join-Path (Join-Path $ArtifactsDir $configuration) "tmp"

function Create-Directory([string[]] $path) {
  if (!(Test-Path -path $path)) {
    New-Item -path $path -force -itemType "Directory" | Out-Null
  }
}

function GetVSWhereVersion {
  [xml]$xml = Get-Content $DependenciesProps
  return $xml.Project.PropertyGroup.VSWhereVersion
}

function LocateMsbuild {
  
  $vswhereVersion = GetVSWhereVersion
  $vsWhereDir = Join-Path $ToolsRoot "vswhere\$vswhereVersion"
  $vsWhereExe = Join-Path $vsWhereDir "vswhere.exe"
  
  if (!(Test-Path $vsWhereExe)) {
    Create-Directory $vsWhereDir   
    Invoke-WebRequest "http://github.com/Microsoft/vswhere/releases/download/$vswhereVersion/vswhere.exe" -OutFile $vswhereExe
  }
  
  $vsInstallDir = & $vsWhereExe -latest -property installationPath -requires Microsoft.Component.MSBuild -requires Microsoft.VisualStudio.Component.VSSDK -requires Microsoft.Net.Component.4.6.TargetingPack -requires Microsoft.VisualStudio.Component.Roslyn.Compiler -requires Microsoft.VisualStudio.Component.VSSDK
  $msbuildExe = Join-Path $vsInstallDir "MSBuild\15.0\Bin\msbuild.exe"
  
  if (!(Test-Path $msbuildExe)) {
    throw "Failed to locate msbuild (exit code '$lastExitCode')."
  }

  return $msbuildExe
}

function Build {
  $msbuildExe = LocateMsbuild
  
  if ($ci) {
    Create-Directory($logDir)
    $binlog = "/bl:" + (Join-Path $LogDir "Build.binlog")
  } else {
    $binlog = ""
  }

  & $msbuildExe $BuildProj /m /v:$verbosity $binlog /p:Configuration=$configuration /p:SolutionPath=$solution /p:Restore=$restore /p:DeployDeps=$deployDeps /p:Build=$build /p:Rebuild=$rebuild /p:Deploy=$deploy /p:Test=$test /p:IntegrationTest=$integrationTest /p:Sign=$sign /p:Pack=$pack /p:CIBuild=$ci $properties

  if ($lastExitCode -ne 0) {
    throw "Build failed (exit code '$lastExitCode')."
  }
}

if ($ci) {
  Create-Directory $TempDir
  $env:TEMP = $TempDir
  $env:TMP = $TempDir
}

# clean nuget packages -- necessary to avoid mismatching versions of swix microbuild build plugin and VSSDK on Jenkins
$nugetRoot = (Join-Path $env:USERPROFILE ".nuget\packages")
if ($clearCaches -and (Test-Path $nugetRoot)) {
  Remove-Item $nugetRoot -Recurse -Force
}

Build
