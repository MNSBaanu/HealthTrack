# Client regression checks

Run from the repository root:

```powershell
dotnet run --project Tests/ClientRegressionChecks
```

Requires SQL Server LocalDB. The runner creates a uniquely named `HealthTrack_ClientChecks_*` database and removes it in `finally`. It never reads application configuration or connects to HealthTrackDB. Schema is created from the current EF model.

No emails are sent (a fake sender is used) and the clock is fixed at 2026-10-29 14:00 UTC (19:30 in Asia/Colombo), so the checks do not depend on the machine's time zone.

Covered: atomic registration, time-zone-correct "not in the future" validation, blood-pressure validation, duplicate planned workout/meal prevention, daily reminder rules, single-answer weekly check-ins and feedback replies, the dashboard's daily calorie total, and account deletion.
