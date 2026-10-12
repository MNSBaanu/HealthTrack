# Client assigned plans (B01)

Clients use `/ClientPlans` to view their own assigned workout and diet plans.
Workout details are at `/ClientPlans/Workout/{id}`; diet details at `/ClientPlans/Diet/{id}`.
Every query filters by the authenticated Client ID, including detail requests. Other owners and unknown IDs return 404. These are read-only endpoints restricted to the Client role.

The dashboard shows up to three currently active plans of each type, with a link to all plans. Active means IsActive is true, the start date has arrived, and the end date is absent or has not passed. Start/end dates are inclusive; today's date uses the configured appointment timezone (Asia/Colombo by default). Upcoming, ended and inactive plans remain in the full list.

Plans retain their original Trainer after Client reassignment, matching the existing preservation rule. Saved plan changes are read on each request. Personal workout and meal logs remain independent.

Workout items display their weekly day, sets, reps, duration, rest and instructions. Diet meals display meal type, food recommendations, estimated calories and notes, with daily calorie/macronutrient targets. The shared diet model has no exact meal times or weekdays; those require a Trainer-owned model change if needed. No Trainer controllers, services, models or migrations were changed for B01.

Run `Tests/Run-ClientPlanTests.ps1` for isolated HTTP/SQL acceptance checks. The runner creates and removes its own temporary LocalDB database and does not alter application data.
