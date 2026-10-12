param([string]$PythonPath = (Join-Path $env:USERPROFILE '.cache/codex-runtimes/codex-primary-runtime/dependencies/python/python.exe'))
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$outputRoot = Join-Path $projectRoot 'artifacts/reminders'
$runtimeRoot = Join-Path $outputRoot 'runtime'
$databaseName = 'HealthTrack_Reminders_Test_' + [Guid]::NewGuid().ToString('N')
$testProcess = $null
$testExit = 1
$originalEnvironment = $env:ASPNETCORE_ENVIRONMENT
$serverName = '(localdb)\MSSQLLocalDB'
if (-not (Test-Path -LiteralPath $PythonPath)) { throw 'Set -PythonPath to a Python 3 executable.' }
if ([Net.NetworkInformation.IPGlobalProperties]::GetIPGlobalProperties().GetActiveTcpListeners().Port -contains 57627) {
    throw 'Port 57627 is already in use.'
}
Push-Location $projectRoot
try {
    dotnet build HealthTrack.csproj --nologo --no-restore
    if ($LASTEXITCODE -ne 0) { throw 'HealthTrack build failed.' }
    New-Item -ItemType Directory -Force -Path $runtimeRoot | Out-Null
    $configuration = Get-Content -LiteralPath 'appsettings.json' -Raw | ConvertFrom-Json
    $configuration.ConnectionStrings.DefaultConnection = "Server=$serverName;Database=$databaseName;Trusted_Connection=True;TrustServerCertificate=True;"
    $configuration.AdminAccount.Email = 'admin.reminders.test@healthtrack.example'
    $configuration.AdminAccount.Password = 'TestOnly!Pass2026'
    $configuration | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath (Join-Path $runtimeRoot 'appsettings.json')
    Copy-Item -LiteralPath (Join-Path $projectRoot 'wwwroot') -Destination $runtimeRoot -Recurse -Force
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $dll = Join-Path $projectRoot 'bin/Debug/net8.0/HealthTrack.dll'
    $testProcess = Start-Process -FilePath 'dotnet' -WindowStyle Hidden -PassThru -WorkingDirectory $projectRoot `
        -ArgumentList @('"' + $dll + '"', '--contentRoot', '"' + $runtimeRoot + '"', '--urls', 'http://localhost:57627') `
        -RedirectStandardOutput (Join-Path $outputRoot 'server.log') -RedirectStandardError (Join-Path $outputRoot 'server-errors.log')
    $ready = $false
    for ($attempt = 0; $attempt -lt 45; $attempt++) {
        if ($testProcess.HasExited) { throw 'Test server exited. Inspect artifacts/reminders/server-errors.log.' }
        try {
            $response = Invoke-WebRequest -Uri 'http://localhost:57627/Account/Login' -TimeoutSec 2
            if ($response.StatusCode -eq 200) { $ready = $true; break }
        } catch { Start-Sleep -Milliseconds 500 }
    }
    if (-not $ready) { throw 'Test server did not become ready.' }
    & $PythonPath (Join-Path $PSScriptRoot 'reminders_functional.py') --database $databaseName --server $serverName
    $testExit = $LASTEXITCODE
} finally {
    if ($null -ne $testProcess -and -not $testProcess.HasExited) {
        Stop-Process -Id $testProcess.Id
        $testProcess.WaitForExit()
    }
    $env:ASPNETCORE_ENVIRONMENT = $originalEnvironment
    if ($databaseName -match '^HealthTrack_Reminders_Test_[a-f0-9]{32}$') {
        $cleanupConnection = [System.Data.SqlClient.SqlConnection]::new("Server=$serverName;Database=master;Trusted_Connection=True;")
        try {
            $cleanupConnection.Open()
            $command = $cleanupConnection.CreateCommand()
            $command.CommandText = "IF DB_ID(N'$databaseName') IS NOT NULL BEGIN ALTER DATABASE [$databaseName] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$databaseName]; END"
            $command.ExecuteNonQuery() | Out-Null
        } finally { $cleanupConnection.Dispose() }
    }
    Pop-Location
}
exit $testExit
