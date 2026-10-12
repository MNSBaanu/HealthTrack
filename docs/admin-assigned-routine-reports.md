# Admin assigned routine reports (B05)

Participation is assignment coverage: distinct Clients with at least one currently active assigned WorkoutPlan, divided by all Clients. This does not measure plan completion, attendance or adherence. No plan completion is inferred from free-text WorkoutLogs.

Each WorkoutPlan row represents one personalized assigned routine. Plans with identical titles are not combined. The report includes plan ID, Client, original Trainer, active dates, status and exercise count. Total assigned plans includes upcoming, ended and inactive plans; current active plans use inclusive start/end dates and IsActive. Today's date uses the configured appointment timezone. Preserved plans still count after Trainer reassignment or assignment removal. Current ClientTrainerAssignment counts are displayed separately.

Client performance and engagement calculations are preserved. Personal workout activities retain their top-ten-by-session summary, now accurately labelled. The HTML report, PDF and CSV exports use the same report builder and include assigned-plan counts, participation definition, coverage, current Trainer assignment counts and all routine rows. This change adds no Trainer code, schema changes or completion tracking.

Run Tests/Run-AdminReportTests.ps1 for disposable database verification. Pass -ServerName to select a working SQL Server instance; the application database is never used for fixtures.
