param([ValidateSet('chrome', 'msedge')][string]$Browser = 'chrome')

$ErrorActionPreference = 'Stop'
$cache = Join-Path $PSScriptRoot 'obj/local-check'
$temporaryFiles = Join-Path $cache 'temp'
New-Item -ItemType Directory -Path $temporaryFiles -Force | Out-Null
$existingPackages = $env:NUGET_PACKAGES
if (-not $existingPackages) { $existingPackages = Join-Path $env:USERPROFILE '.nuget/packages' }

# Giữ package và tệp tạm trên cùng ổ với project, khôi phục môi trường khi kết thúc.
$settings = @{
    TEMP = $temporaryFiles
    TMP = $temporaryFiles
    ASPNETCORE_TEMP = $temporaryFiles
    NUGET_PACKAGES = (Join-Path $cache 'packages')
    NUGET_HTTP_CACHE_PATH = (Join-Path $cache 'http')
    NUGET_SCRATCH = (Join-Path $cache 'scratch')
    npm_config_cache = (Join-Path $cache 'npm')
    SWAGGER_BROWSER_CHANNEL = $Browser
}
$previous = @{}
Push-Location -LiteralPath $PSScriptRoot
try {
    foreach ($name in $settings.Keys) {
        $previous[$name] = [Environment]::GetEnvironmentVariable($name)
        [Environment]::SetEnvironmentVariable($name, $settings[$name])
    }
    npm.cmd ci --no-fund --no-audit
    if ($LASTEXITCODE -ne 0) { throw 'Không cài được playwright-core cho Swagger UI.' }

    $restoreArgs = @('restore', 'Recruitment.Tests.csproj')
    if ((Test-Path -LiteralPath $existingPackages) -and $existingPackages -ne $env:NUGET_PACKAGES) {
        $restoreArgs += "-p:RestoreFallbackFolders=$existingPackages"
    }
    dotnet @restoreArgs
    if ($LASTEXITCODE -ne 0) { throw 'Không khôi phục được package kiểm thử.' }

    dotnet test Recruitment.Tests.csproj -c Integration --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'Bộ kiểm thử chưa đạt. Xem tên và thông báo của ca lỗi phía trên.' }
}
finally {
    foreach ($name in $previous.Keys) {
        [Environment]::SetEnvironmentVariable($name, $previous[$name])
    }
    Pop-Location
}
