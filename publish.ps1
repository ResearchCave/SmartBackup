param(
    # Runtime identifier, e.g. win-x64, linux-x64, osx-x64
    [string]$Runtime = "win-x64"
)

# Determine the repository root based on the script location
$repoRoot = Split-Path -Parent $PSCommandPath

# Compose the publish directory for the given runtime
$publishRoot = Join-Path $repoRoot "publish\$Runtime"

# Remove any existing publish output for a clean build
if (Test-Path $publishRoot) {
    Remove-Item $publishRoot -Recurse -Force
}

# Publish the main SmartBackup application
# Explicitly disable single-file publishing so that plugin DLLs remain separate
# and enable self-contained deployment to produce an executable for the target runtime
$mainProject = Join-Path $repoRoot "SmartBackup\SmartBackup.csproj"

dotnet publish $mainProject `
    -c Release `
    -r $Runtime `
    -p:PublishSingleFile=true `
    -p:SelfContained=true `
    -v:q `
    -o $publishRoot 

# Define the plugin projects that need to be built and copied
$pluginProjects = @(
    "SmartBackup.Archiver\SmartBackup.Archiver.csproj",
    "SmartBackup.HyperV\SmartBackup.HyperV.csproj",
    "SmartBackup.MongoDB\SmartBackup.MongoDB.csproj",
    "SmartBackup.MSSQL\SmartBackup.MSSQL.csproj",
    "SmartBackup.MySQL\SmartBackup.MySQL.csproj",
    "SmartBackup.PostgreSQL\SmartBackup.PostgreSQL.csproj",
    "SmartBackup.Redis\SmartBackup.Redis.csproj",
    "SmartBackup.MariaDB\SmartBackup.MariaDB.csproj"
)

# Build each plugin project in Release configuration
foreach ($proj in $pluginProjects) {
    $projPath = Join-Path $repoRoot $proj
    $projName = [System.IO.Path]::GetFileNameWithoutExtension($proj)

    $pluginPublishDir = Join-Path $publishRoot "_pluginbuild\$projName"

    dotnet publish $projPath `
        -c Release `
        -r $Runtime `
        -p:SelfContained=false `
        -p:PublishSingleFile=false `
        -v:q `
        -o $pluginPublishDir
}

# Create the plugins directory inside the publish root
$pluginsDir = Join-Path $publishRoot "plugins"
if (-not (Test-Path $pluginsDir)) {
    New-Item -ItemType Directory -Path $pluginsDir | Out-Null
}

# Copy the compiled plugin DLLs into the plugins folder
foreach ($proj in $pluginProjects) {
    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($proj)
    $dllSource = Join-Path $publishRoot "_pluginbuild\$projectName\$projectName.dll"

    if (Test-Path $dllSource) {
        Copy-Item $dllSource -Destination $pluginsDir -Force
    } else {
        Write-Error "Plugin DLL not found: $dllSource"
    }
}

# Cleanup temporary plugin publish outputs
$pluginBuildRoot = Join-Path $publishRoot "_pluginbuild"
if (Test-Path $pluginBuildRoot) {
    Remove-Item $pluginBuildRoot -Recurse -Force
}

Write-Host "SmartBackup published to $publishRoot with plugins in $pluginsDir"
