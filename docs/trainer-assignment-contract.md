# Shared Client–Trainer assignment contract

Each Client has at most one current Trainer. Reassignment updates only `ClientTrainerAssignment`; removal deletes only that assignment. Existing appointments and plans remain linked to their original provider/Trainer and are not transferred or cancelled. The current code has no plan tables, so plan preservation is an integration rule for the separate plan module.

`Trainer : Person` uses the existing Identity database and `Users.Role = Trainer` discriminator. `AppRoles.Trainer` is seeded on startup. The Trainer module must create a `Trainer` entity and grant the Trainer Identity role; do not register Trainers as Client entities or maintain a separate login database. Public Client registration is unchanged. There is no Trainer creation/editing/dashboard feature in this change; signing in as a Trainer shows a temporary workspace message until that module is integrated.

`ClientTrainerAssignment` has ClientId as its primary key, TrainerId as a restricted foreign key, UTC assignment timestamp, and SQL rowversion. Client deletion cascades its current assignment. Trainer deletion is blocked while Clients are assigned; reassign or remove those assignments first. Duplicate assignments, wrong account types, missing Trainer roles, locked Trainers, and stale edit/removal requests are rejected.

Admin uses `/Assignments` and `/Assignments/Edit/{clientId}`. Mutation actions are Admin-only POSTs with antiforgery protection. The Client dashboard reads the current assignment on every request. Admin counts and lists query the same shared table.

Trainer integration can call `TrainerAssignmentQueries.ForTrainerAsync(authenticatedTrainerId)` or GET `/TrainerAssignments`, which derives the Trainer ID from authentication and returns only that Trainer's current Clients. Reassignment immediately removes a Client from the previous Trainer's feed and adds them to the new Trainer's feed. The endpoint does not grant access to historical plans/appointments; those modules must authorize their own records separately.

For consultations, link `ConsultationProvider.PersonId` to the Trainer's existing Identity ID and keep its kind as Trainer. Client–Trainer assignment is independent of consultation bookings; Clients can continue using the existing consultation flow.

Run `Tests/Run-AssignmentTests.ps1` to exercise the Admin workflow in a disposable LocalDB database. The runner also requires Python 3 and `sqlcmd`; pass `-PythonPath` if needed. Fixtures promote test accounts to Trainer only inside that generated database and are removed after the run.
