# Admin regression checks

Run from the repository root:

```powershell
dotnet run --project Tests/AdminRegressionChecks
```

Requires SQL Server LocalDB. The runner creates a uniquely named `HealthTrack_AdminChecks_*` database and removes that database in `finally`. It never reads application configuration or connects to HealthTrackDB. Schema is created from the current EF model, so these are relational workflow checks rather than migration-chain tests.

No emails are sent. A fake sender verifies scheduling, retries, duplicate prevention and recipient preferences. Authentication checks use ephemeral data-protection keys and disposable test accounts.

To keep a running development app untouched, build and run against a preview output:

```powershell
dotnet build HealthTrack.csproj --no-restore --output obj/admin-preview
$previewPath = Join-Path (Get-Location) 'obj/admin-preview'
dotnet run --project Tests/AdminRegressionChecks -p:BuildProjectReferences=false "-p:OutputPath=$previewPath"
```
