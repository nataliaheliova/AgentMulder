# BuildNuget.ps1
#
# Builds the AgentMulder nuget package
#

param ($version = $(throw "Version must be specified"), $waveVersion = $(throw "Wave version must be specified"), $config = "Release")

echo "Building AgentMulder version $version for Wave $waveVersion.0, Configuration: $config";

# resetting working dir to the directory of the script
# this is necesary as we are using relative paths later on
$dir = Split-Path $MyInvocation.MyCommand.Path;
Push-Location $dir;
[Environment]::CurrentDirectory = $PWD

$nugetPath = ".nuget\NuGet.exe";
$nugetParams = "pack AgentMulder.nuspec -nopackageanalysis -outputdirectory nuget -properties 'version=$version;waveVersion=$waveVersion;config=$config'";

echo "Running NuGet: ", "$nugetPath $nugetParams"
iex "$nugetPath $nugetParams";

echo "AgentMulder $version built successfully";