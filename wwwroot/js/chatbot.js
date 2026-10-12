(() => {
    const root = document.querySelector('[data-healthtrack-chat]');
    if (!root) return;
    const panel = root.querySelector('.ht-chat-panel');
    const launch = root.querySelector('.ht-chat-launch');
    const input = root.querySelector('input');
    const messages = root.querySelector('.ht-chat-messages');
    const admin = root.dataset.role === 'admin';
    const clientTopics = [
        [/workout plan|exercise plan|assigned.*workout|routine|exercise schedule/, 'Trainer-assigned workout plan views are not available yet. Personal workouts can still be recorded in your training journal.', '/WorkoutLogs', 'Open workout journal'],
        [/diet plan|meal plan|diet schedule|assigned.*diet/, 'Trainer-assigned diet schedule views are not available yet. Use Meals to record your food and calories separately.', '/MealLogs', 'Open meal journal'],
        [/remind|notification|alert/, 'Workout and meal reminders appear on your dashboard. Approved consultations generate an appointment reminder within 24 hours of the start time. The app refreshes reminders automatically while running.', '/Notifications', 'Open notifications'],
        [/appointment|consult|booking|book |healthcare|provider/, 'Open Appointments, choose Book a consultation, then select a provider and available time. Requests start Pending. You can track approval, rescheduling and cancellation in the appointment list. If there are no providers or times, availability has not been published yet.', '/Appointments', 'Open appointments'],
        [/trend|progress|chart|calories burned|calorie trend|export/, 'Progress shows weight, BMI, calories burned and calories consumed from your saved records. Add records on different days to see trends. Use the export controls for PDF or CSV.', '/Client/Progress', 'View progress'],
        [/weight|bmi|metric|measurement|weigh|height/, 'Use Metrics to record weight measurements. You can view, edit and delete your own measurements. BMI uses your weight and profile height; change height under Profile.', '/HealthMetrics', 'Open health metrics'],
        [/workout|exercise|training|running/, 'Use Workouts to log an activity, duration and calories burned. Your journal lets you view, edit and delete your saved workouts.', '/WorkoutLogs', 'Open workout journal'],
        [/meal|food|diet|calorie|nutrition/, 'Use Meals to record the meal type, food items and calories. You can view, edit and delete your meal logs.', '/MealLogs', 'Open meal journal'],
        [/trainer|assignment/, 'Your current Trainer appears on the dashboard. An Admin manages this assignment. Changing your assigned Trainer preserves existing appointments under their original provider.', '/Client', 'Open overview'],
        [/profile|name|goal|birthday/, 'Open Profile from your account menu to update your name, date of birth, height and fitness goal.', '/Client/Profile', 'Open profile']
    ];
    const adminTopics = [
        [/assign|trainer/, 'Assignments lists Clients and their current Trainer. Open Manage assignment to assign, reassign or remove a Trainer. Each Client has one current Trainer. Eligible, unlocked Trainer accounts are required. Existing plans and appointments are preserved.', '/Assignments', 'Manage assignments'],
        [/report|export|pdf|csv|engagement|popular|performance|progress/, 'Reports shows Client performance, logged activities and engagement. Use the PDF or CSV export controls. For one Client’s measurements and logs, open their details from Users. Assigned routine reporting still requires the shared plan module.', '/Admin/Reports', 'Open reports'],
        [/notification|broadcast|alert|remind|message/, 'Broadcast lets you send a message to a Client or all Clients, immediately or at a scheduled time. Appointment reminders for approved consultations are generated automatically.', '/Admin/SendNotification', 'Send a notification'],
        [/user|client|lock|delete|account|member/, 'Users lets you search and filter accounts, view Client progress, and lock or unlock accounts. Deletion permanently removes linked Client records. You cannot lock or delete your own account.', '/Admin/Users', 'Manage users'],
        [/overview|dashboard|usage|activity|stat/, 'The Admin console shows user activity, a 14-day usage chart, Trainer counts and assigned/unassigned Client summaries.', '/Admin', 'Open console']
    ];
    let lastTopic = null;
    function add(text, user = false, link = null) {
        const bubble = document.createElement('div');
        bubble.className = 'ht-chat-message' + (user ? ' user' : '');
        bubble.textContent = text;
        if (link) {
            const anchor = document.createElement('a');
            anchor.href = link[0];
            anchor.textContent = link[1];
            bubble.append(anchor);
        }
        messages.append(bubble);
        messages.scrollTop = messages.scrollHeight;
    }
    function greet() {
        lastTopic = null;
        messages.replaceChildren();
        add('Hi! I’m the built-in HealthTrack guide. Ask how to use ' + (admin ? 'Users, assignments, reports or notifications.' : 'workouts, meals, metrics, progress or appointments.'));
    }
    function ask(raw) {
        const question = raw.trim().slice(0, 300);
        if (!question) return;
        add(question, true);
        const normalized = question.toLowerCase().replace(/[^a-z0-9\s]/g, ' ');
        if (/diagnos|medicine|medication|symptom|treatment|medical advice|what should i eat|should i take/.test(normalized)) {
            add('I can explain HealthTrack features. For personal health advice, speak with a healthcare professional.');
            return;
        }
        const topic = (admin ? adminTopics : clientTopics).find(item => item[0].test(normalized))
            || (/^(where|how|can i).*(it|that|this)/.test(normalized) ? lastTopic : null);
        if (topic) {
            lastTopic = topic;
            add(topic[1], false, [topic[2], topic[3]]);
        } else if (/^(hi|hello|hey|thanks|thank you)\b/.test(normalized)) {
            add('Happy to help. Choose a suggested question or ask about a HealthTrack feature.');
        } else {
            add('I don’t have an answer for that yet. Try one of the suggested questions, or ask about ' + (admin ? 'Clients, assignments, reports or notifications.' : 'workouts, meals, metrics, progress or appointments.'));
        }
    }
    function close() { panel.hidden = true; launch.setAttribute('aria-expanded', 'false'); launch.focus(); }
    launch.addEventListener('click', () => {
        if (!panel.hidden) { close(); return; }
        panel.hidden = false; launch.setAttribute('aria-expanded', 'true'); input.focus();
    });
    root.querySelector('.ht-chat-close').addEventListener('click', close);
    root.querySelector('.ht-chat-clear').addEventListener('click', () => { greet(); input.value = ''; input.focus(); });
    root.addEventListener('keydown', event => { if (event.key === 'Escape' && !panel.hidden) { event.preventDefault(); close(); } });
    root.querySelector('form').addEventListener('submit', event => { event.preventDefault(); ask(input.value); input.value = ''; input.focus(); });
    root.querySelectorAll('.ht-chat-suggestions button').forEach(button => button.addEventListener('click', () => ask(button.textContent)));
    greet();
})();
