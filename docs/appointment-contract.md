# Shared consultation contract

The Client module reads these shared tables. Provider management and Trainer approval/update/cancellation screens are outside this change.

- `ConsultationProvider`: display name, specialty, kind (`Trainer = 0`, `HealthcareProfessional = 1`), active flag, optional unique `PersonId` referencing the existing Identity account. Link `PersonId` when integrating authenticated Trainer workflows. No additional Identity role or Trainer dashboard is introduced here.
- `AvailabilitySlot`: provider ID, UTC start/end timestamps, availability flag. End must follow start. Providers publish slots through their module. The Client cannot create or edit slots.
- `Appointment`: Client ID from authentication, slot ID, status, UTC request timestamp, optional notes (2,000 characters maximum).

Persisted statuses: `Pending = 0`, `Approved = 1`, `Cancelled = 2`, `Completed = 3`. Client requests start Pending. All non-cancelled appointments reserve their slot. Adjacent appointments are permitted; overlapping appointments for either the Client or provider are rejected. The booking transaction uses serializable isolation; a filtered unique index also guards against duplicate active slot bookings.

Store all appointment timestamps in UTC. Display uses `Appointments:TimeZoneId`, default `Asia/Colombo`. The setting applies consistently to booking, list, details, and dashboard views.

Trainer workflows should identify their provider by authenticated `PersonId`, query appointments through `Appointment.Slot.Provider`, and perform their own authorization and validation. To reschedule one appointment, create/select an available slot and update its `SlotId` after rechecking provider/Client conflicts inside a transaction. Do not change a used slot's provider or times: that would also change historical and cancelled appointments referencing it. For cancellation, retain the row and set Status to Cancelled. For approval, set Status to Approved. Client views load current shared records on each request.

Dashboard summaries show the next three future Pending/Approved appointments. The appointment list retains past, cancelled, and completed records. Detail URLs enforce Client ownership and return 404 for records belonging to another Client.

The migration creates the tables without demo providers or slots. Until real providers publish availability, the booking screen shows an empty state. The functional test runner seeds fixtures only in a uniquely named disposable database.

## Verification

Run `Tests/Run-AppointmentTests.ps1` on Windows with SQL Server LocalDB, the .NET SDK, `sqlcmd`, and Python 3 available. Use `-PythonPath` to supply a Python executable. The runner builds the app, starts it on an unused local port with a disposable database, exercises booking and authorization, then stops the app and removes only that generated database.
