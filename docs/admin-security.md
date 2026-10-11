# Admin authorization and account sessions

HealthTrack uses one Admin role. All Admins have the same management permissions. The old `Users.AccessLevel` column is retained for database compatibility, but it is not an authorization setting and cannot be selected or bound through the Admin creation form. New Admin accounts use the default legacy value of 1.

The application cookie validates the Identity security stamp on every authenticated request. It also checks whether the current account exists and is locked out. Locking an account, updating its security stamp, or deleting it causes existing sessions to be rejected on their next request. Automatic lockouts are checked even when no security stamp changes. Unlocking permits a fresh login; it does not restore a rejected cookie.

This validation adds database work to authenticated requests. Keep the immediate checks when optimizing; increasing the validation interval would reintroduce delayed session revocation.

Admin notifications accept a specific Client recipient or a broadcast to all Clients. The server validates recipient IDs against the Client set. Admin and Trainer IDs cannot be substituted into this form. Undefined notification types display a field validation error and are not saved.

Run `Tests/Run-AdminSecurityTests.ps1` for focused regressions. The runner starts an isolated app on port 57631, generates a temporary LocalDB database, and cleans up its disposable accounts/database. It does not use the running preview database.
