# devDeploy.ps1
#
# Deploys the extension into a VS experimental instance
#

param ($hive = $(throw "VS experimental hive must be specified"), $version = $(throw "Version must be specified"), $config = $(throw "Configuration must be specified"))

echo "Deploying AgentMulder $version ($config) to VS hive $hive";

# resetting working dir to the directory of the script
# this is necesary as we are using relative paths later on
$dir = Split-Path $MyInvocation.MyCommand.Path;
Push-Location $dir;
[Environment]::CurrentDirectory = $PWD;

# copy the main binaries and PDBs
$targetDir = "$env:LOCALAPPDATA\JetBrains\plugins\ERNI.AgentMulder.$version\dotFiles";
$binSourcePath = "..\output\$config\$version\AgentMulder.*.dll";
$pdbSourcePath = "..\output\$config\$version\AgentMulder.*.pdb";

Copy-Item $binSourcePath $targetDir;
Copy-Item $pdbSourcePath $targetDir;

# copy container-specific implementations
$targetDir = "$env:LOCALAPPDATA\JetBrains\plugins\ERNI.AgentMulder.$version\dotFiles\Containers";
New-Item $targetDir -type directory -force | Out-Null

$containerBinSourcePath = "..\output\$config\$version\Containers\AgentMulder.*.dll";
$containerPdbSourcePath = "..\output\$config\$version\Containers\AgentMulder.*.pdb";

Copy-Item $containerBinSourcePath $targetDir;
Copy-Item $containerPdbSourcePath $targetDir;

# done
echo "AgentMulder $version ($config) copied to hive $hive";