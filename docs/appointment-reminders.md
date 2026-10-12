# Automatic appointment reminders

Approved consultations generate one Client notification within 24 hours of the UTC start time. The message uses the appointment display timezone (default Asia/Colombo). Pending, cancelled, completed, past, and more distant appointments do not generate automatic reminders.

A background worker synchronizes reminders at startup and every minute while the app is running. Notification pages, the bell/unread-count endpoint, and dashboard loads also synchronize them. No Admin broadcast is required. These are in-app alerts; the worker must run for reminders to be persisted while the Client is offline.

Notifications store an optional AppointmentId and AppointmentStartUtc. A filtered unique index prevents duplicate automatic reminders. Rescheduling refreshes the message and unread status; moving outside the reminder window, cancellation, completion, or passing the start time removes stale automatic reminders. Dismissing a reminder preserves an internal flag so refreshes do not recreate it. A subsequent reschedule can alert again. Manual Admin appointment notifications remain independent.

Mark-all-as-read affects only visible, non-dismissed notifications. Single-read requests also reject hidden future messages. Future scheduled Admin messages remain unread until due.

Provider workflows should retain cancelled appointments rather than physically deleting them, as defined by the existing consultation contract. Client deletion cascades its appointments and notifications. No Trainer management screen is introduced.

Run Tests/Run-ReminderTests.ps1 to validate worker generation, lifecycle handling, dismissal, ownership, and future-message handling against a disposable LocalDB database. The worker test can take just over one minute.
