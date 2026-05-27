param(
    # Runtime identifier, e.g. win-x64, linux-x64, osx-x64
    [string]$Runtime = "win-x64",
    # Optional: remove previous publish output before publishing
    [switch]$Clean,
    # Optional: skip bundling mssql-scripter CLI into MSSQL plugin
    [switch]$SkipMssqlScripterInstall
)

$ErrorActionPreference = "Stop"

# Determine the repository root based on the script location
$repoRoot = Split-Path -Parent $PSCommandPath

# Compose the publish directory for the given runtime
$publishRoot = Join-Path $repoRoot "publish\$Runtime"
$pluginBuildRoot = Join-Path $publishRoot "_pluginbuild"
$pluginsDir = Join-Path $publishRoot "plugins"

# Remove existing publish output only when explicitly requested.
# Windows can temporarily hold the publish root directory open; if so, clear and reuse it.
if ($Clean -and (Test-Path $publishRoot)) {
    Remove-Item $publishRoot -Recurse -Force -ErrorAction SilentlyContinue
    if (Test-Path $publishRoot) {
        Get-ChildItem $publishRoot -Force | Remove-Item -Recurse -Force
    }
}

# Publish the main SmartBackup application as a single-file executable.
# Plugin assemblies stay external under plugins/<ProjectName>/.
$mainProject = Join-Path $repoRoot "SmartBackup\SmartBackup.csproj"

dotnet publish $mainProject `
    -c Release `
    -r $Runtime `
    -p:PublishSingleFile=true `
    -p:SelfContained=true `
    -v:q `
    -o $publishRoot

# Define the plugin projects that need to be published into plugins/<ProjectName>/.
$pluginProjects = @(
    "SmartBackup.Archiver\SmartBackup.Archiver.csproj",
    "SmartBackup.HyperV\SmartBackup.HyperV.csproj",
    "SmartBackup.MongoDB\SmartBackup.MongoDB.csproj",
    "SmartBackup.MSSQL\SmartBackup.MSSQL.csproj",
    "SmartBackup.MySQL\SmartBackup.MySQL.csproj",
    "SmartBackup.PostgreSQL\SmartBackup.PostgreSQL.csproj",
    "SmartBackup.Redis\SmartBackup.Redis.csproj",
    "SmartBackup.MariaDB\SmartBackup.MariaDB.csproj",
    "SmartBackup.Elasticsearch\SmartBackup.Elasticsearch.csproj"
)

New-Item -ItemType Directory -Path $pluginsDir -Force | Out-Null

if (Test-Path $pluginBuildRoot) {
    Remove-Item $pluginBuildRoot -Recurse -Force -ErrorAction SilentlyContinue
}

foreach ($proj in $pluginProjects) {
    $projPath = Join-Path $repoRoot $proj
    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($proj)
    $pluginPublishDir = Join-Path $pluginBuildRoot $projectName
    $pluginTargetDir = Join-Path $pluginsDir $projectName

    dotnet publish $projPath `
        -c Release `
        -r $Runtime `
        -p:SelfContained=false `
        -p:PublishSingleFile=false `
        -v:q `
        -o $pluginPublishDir

    New-Item -ItemType Directory -Path $pluginTargetDir -Force | Out-Null
    Copy-Item (Join-Path $pluginPublishDir "*") -Destination $pluginTargetDir -Recurse -Force

    # The host owns the common contract assembly. Keep plugin-specific dependencies only.
    Remove-Item (Join-Path $pluginTargetDir "SmartBackup.Common.dll") -Force -ErrorAction SilentlyContinue
    Remove-Item (Join-Path $pluginTargetDir "SmartBackup.Common.pdb") -Force -ErrorAction SilentlyContinue
    Remove-Item (Join-Path $pluginTargetDir "SmartBackup.Common.xml") -Force -ErrorAction SilentlyContinue

    if ($projectName -eq "SmartBackup.MSSQL" -and -not $SkipMssqlScripterInstall) {
        $toolRoot = Join-Path $pluginTargetDir "tools\mssql-scripter"
        $venvDir = Join-Path $toolRoot "venv"
        $venvPython = Join-Path $venvDir "Scripts\python.exe"
        $venvScripterExe = Join-Path $venvDir "Scripts\mssql-scripter.exe"
        $venvScripterCmd = Join-Path $venvDir "Scripts\mssql-scripter.cmd"

        if (-not (Test-Path $venvScripterExe) -and -not (Test-Path $venvScripterCmd)) {
            New-Item -ItemType Directory -Path $toolRoot -Force | Out-Null

            $pythonLauncher = Get-Command py -ErrorAction SilentlyContinue
            if (-not $pythonLauncher) { $pythonLauncher = Get-Command python -ErrorAction SilentlyContinue }
            if (-not $pythonLauncher) {
                throw "Python was not found. Install Python or run publish with -SkipMssqlScripterInstall."
            }

            if (Test-Path $venvDir) {
                Remove-Item $venvDir -Recurse -Force
            }

            if ($pythonLauncher.Name -eq "py") {
                & $pythonLauncher.Source -3 -m venv $venvDir
            }
            else {
                & $pythonLauncher.Source -m venv $venvDir
            }

            & $venvPython -m pip install --upgrade pip
            & $venvPython -m pip install mssql-scripter==1.0.0a23
        }
    }
}

if (Test-Path $pluginBuildRoot) {
    Remove-Item $pluginBuildRoot -Recurse -Force
}

Write-Host "SmartBackup published to $publishRoot with plugins in $pluginsDir"
