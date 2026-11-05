(() => {
    const root = document.querySelector('[data-healthtrack-chat]');
    if (!root) return;
    const panel = root.querySelector('.ht-chat-panel');
    const launch = root.querySelector('.ht-chat-launch');
    const input = root.querySelector('input');
    const messages = root.querySelector('.ht-chat-messages');
    const admin = root.dataset.role === 'admin';
    const clientTopics = [
        [['workout plan', 'exercise plan', 'training plan', 'assigned workout', 'routine', 'exercise schedule', 'plan', 'plans'], 'Open Plans to view your Trainer-assigned workouts, exercises and weekly schedule. Record completed workouts separately in your journal.', '/ClientPlans', 'Open assigned plans'],
        [['diet plan', 'meal plan', 'diet schedule', 'assigned diet', 'daily target', 'targets'], 'Open Plans to view your assigned diet, recommended meals and daily targets. Use Meals separately to record what you eat.', '/ClientPlans', 'Open assigned plans'],
        [['appointment', 'consultation', 'consult', 'booking', 'book', 'reschedule', 'cancel', 'healthcare', 'provider', 'doctor', 'session', 'slot'], 'Open Appointments, choose Book a consultation, then select a provider and available time. Requests start Pending. You can track approval, rescheduling and cancellation in the appointment list. If there are no providers or times, availability has not been published yet.', '/Appointments', 'Open appointments'],
        [['progress', 'trend', 'chart', 'graph', 'history', 'calories burned', 'report', 'export', 'pdf', 'csv', 'excel'], 'Progress shows weight, BMI, calories burned and calories consumed from your saved records. Add records on different days to see trends. Use Export for a PDF, CSV or Excel copy.', '/Client/Progress', 'View progress'],
        [['weight', 'bmi', 'metric', 'metrics', 'measurement', 'weigh', 'height', 'kg', 'body'], 'Use Metrics to record weight measurements. You can view, edit and delete your own measurements. BMI uses your weight and profile height; change height under Profile.', '/HealthMetrics', 'Open health metrics'],
        [['workout', 'exercise', 'training', 'run', 'running', 'gym', 'activity', 'cardio', 'walk', 'log workout'], 'Use Workouts to log an activity, duration and calories burned. Your journal lets you view, edit and delete your saved workouts.', '/WorkoutLogs', 'Open workout journal'],
        [['meal', 'food', 'diet', 'calorie', 'nutrition', 'eat', 'breakfast', 'lunch', 'dinner', 'snack', 'log meal'], 'Use Meals to record the meal type, food items and calories. You can view, edit and delete your meal logs.', '/MealLogs', 'Open meal journal'],
        [['trainer', 'coach', 'assignment', 'assigned'], 'Your current Trainer appears on the dashboard. An Admin manages this assignment. Changing your assigned Trainer preserves existing appointments under their original provider.', '/Client', 'Open overview'],
        [['feedback', 'comment', 'reply', 'check in', 'checkin'], 'Open Feedback to read comments from your Trainer and reply to them.', '/ClientFeedback', 'Open feedback'],
        [['password', 'sign in', 'log in', 'login', 'locked', 'security', 'account details', 'change password'], 'Open Account from your account menu to see your account details and change your password. Accounts lock for a short time after several failed sign-in attempts.', '/Account/ChangePassword', 'Open account'],
        [['delete', 'delete account', 'delete my account', 'close my account', 'close account', 'remove account', 'my data', 'download data', 'all my data', 'privacy'], 'Download everything we hold about you from Progress, then Export, then All my data. You can delete your account and data from Profile.', '/Client/Progress', 'Open progress'],
        [['notification', 'bell', 'alert', 'remind', 'reminder', 'message'], 'The bell in the top bar lists your notifications, including Trainer messages and reminders. Workout and meal reminders appear on your dashboard; choose reminder times under Profile.', '/Notifications', 'Open notifications'],
        [['profile', 'name', 'goal', 'birthday', 'date of birth', 'photo', 'target weight'], 'Open Profile from your account menu to update your name, date of birth, height, fitness goal and photo.', '/Client/Profile', 'Open profile']
    ];
    const adminTopics = [
        [['bulk', 'multiple', 'several', 'many users', 'select all', 'at once', 'checkbox'], 'In Users, tick the checkboxes next to members (or the header box to select the page). A bar appears with Assign trainer, Send reminder, Sign out everywhere, Lock, Unlock, Export CSV and Delete.', '/Admin/Users', 'Open users'],
        [['assign', 'assignment', 'reassign', 'trainer', 'coach', 'unassigned', 'workload', 'least busy'], 'Assignments lists Clients and their current Trainer. Open Manage assignment to assign, reassign or remove a Trainer; the list shows each Trainer’s client count and suggests the least busy one. Existing plans and appointments are preserved.', '/Assignments', 'Manage assignments'],
        [['new user', 'new admin', 'new trainer', 'create', 'add user', 'add admin', 'add trainer', 'register'], 'In Users, use New user for an administrator, New trainer for a Trainer, or New provider for a healthcare professional. New admins are also available from your account menu.', '/Admin/Users', 'Open users'],
        [['import', 'upload', 'template', 'csv import'], 'In Users, choose Import CSV to add many Clients and Trainers at once. Download the template first; leave Password empty to generate temporary passwords.', '/Admin/Users', 'Open users'],
        [['reset password', 'temporary password', 'temp password', 'forgot password', 'password'], 'In Users, open a member’s ⋮ menu and choose Reset temporary password. They must change it at next sign-in. Set how long temporary passwords last in Settings.', '/Admin/Users', 'Open users'],
        [['lock', 'unlock', 'delete', 'remove user', 'ban', 'disable', 'suspend', 'sign out', 'logout', 'force'], 'In Users, use the lock icon to lock or unlock an account and the bin icon to delete it. Sign out everywhere is in the ⋮ menu. You cannot lock or delete your own account.', '/Admin/Users', 'Manage users'],
        [['inactive', 'reminder', 'remind', 'not active', 'idle'], 'In Users, set the status filter to Inactive clients to see Clients who have not signed in for 14 days, then use Send reminder from the ⋮ menu or the bulk bar.', '/Admin/Users?status=inactive', 'Show inactive clients'],
        [['user', 'users', 'client', 'member', 'account', 'edit', 'rename', 'email', 'search', 'download data', 'user data'], 'Users lets you search and filter by role and status, edit a member’s details with the pencil icon, and open the ⋮ menu for login history, password reset, sign out and Download data.', '/Admin/Users', 'Manage users'],
        [['provider', 'providers', 'healthcare', 'professional', 'doctor', 'nutritionist', 'availability', 'slot'], 'Providers are listed in Users (filter Role to Providers). Use New provider to add one, the calendar icon to publish availability, and the toggle to turn them on or off.', '/Admin/Users?role=Provider', 'Show providers'],
        [['appointment', 'appointments', 'booking', 'consultation', 'cancel', 'session'], 'Appointments lists every booking across Trainers and providers. Filter by client, provider, status or date, and cancel pending or approved bookings; the Client is notified.', '/Admin/Appointments', 'Open appointments'],
        [['rating', 'ratings', 'review', 'stars', 'hide', 'abusive', 'moderate'], 'Open Reports, then Filter, then Ratings. Each rating has Hide or Restore; hidden ratings no longer count towards a provider’s average.', '/Admin/Ratings', 'Open ratings'],
        [['audit', 'audit log', 'who changed', 'activity log', 'actions'], 'Open Reports, then Filter, then Audit log to see who changed what. Filter by person, action or date and export the result.', '/Admin/AuditLog', 'Open audit log'],
        [['system health', 'health', 'failed', 'login', 'failed login', 'sign ins', 'security', 'locked accounts'], 'Open Reports, then Filter, then System health for sign-in activity, failed sign-ins, locked accounts and pending work.', '/Admin/SystemHealth', 'Open system health'],
        [['chatbot', 'chat', 'faq', 'unanswered', 'saved answer', 'bot', 'question'], 'Open Reports, then Filter, then Chat questions to see what the chatbot could not answer. Use the answer button to save a reply; the chatbot uses saved answers from then on.', '/Admin/ChatQuestions', 'Open chat questions'],
        [['setting', 'settings', 'banner', 'maintenance', 'lockout', 'expiry', 'import limit', 'configure', 'weekly report', 'email report'], 'Settings (in your account menu) controls the site-wide banner, lockout rules, temporary password expiry, the import row limit and the weekly PDF report emailed to admins.', '/Admin/Settings', 'Open settings'],
        [['report', 'reports', 'export', 'pdf', 'excel', 'csv', 'engagement', 'popular', 'performance', 'statistics', 'analytics', 'date range'], 'Reports shows Client performance, popular routines and top trainer plans, filtered by date range. Use Filter to switch report and Export for PDF, Excel or CSV. The General export includes every report.', '/Admin/Reports', 'Open reports'],
        [['notification', 'notify', 'broadcast', 'message', 'alert', 'send', 'announce'], 'Broadcast lets you send a message to a Client, all Clients or all Trainers, immediately or at a scheduled time. For a message on every page, use the banner in Settings.', '/Admin/SendNotification', 'Send a notification'],
        [['console', 'dashboard', 'overview', 'usage', 'stat', 'stats'], 'The Admin console shows user activity for the week and a 14-day usage chart of client and trainer activity.', '/Admin', 'Open console'],
        [['my account', 'my password', 'change my password', 'account details'], 'Open Account from your account menu to see your details and change your password.', '/Account/ChangePassword', 'Open account']
    ];
    const stopWords = new Set(['how', 'do', 'does', 'did', 'i', 'the', 'a', 'an', 'to', 'can', 'could', 'where', 'what', 'which', 'when', 'is', 'are', 'was', 'me', 'of', 'in', 'on', 'for', 'and', 'or', 'it', 'this', 'that', 'you', 'your', 'with', 'be', 'should', 'please', 'want', 'need', 'help', 'use', 'find', 'see', 'get', 'there', 'way', 'any', 'some', 'from', 'about', 'at', 'by', 'tell', 'show']);
    const stem = w => w.length > 5 && w.endsWith('ing') ? w.slice(0, -3)
        : w.length > 4 && w.endsWith('ed') ? w.slice(0, -2)
        : w.length > 3 && w.endsWith('s') && !w.endsWith('ss') ? w.slice(0, -1) : w;
    const nearly = (a, b) => {
        if (Math.abs(a.length - b.length) > 1) return false;
        let i = 0, j = 0, edits = 0;
        while (i < a.length && j < b.length) {
            if (a[i] === b[j]) { i++; j++; continue; }
            if (++edits > 1) return false;
            if (a.length > b.length) i++; else if (b.length > a.length) j++; else { i++; j++; }
        }
        return edits + (a.length - i) + (b.length - j) <= 1;
    };
    function matchTopic(topics, normalized) {
        const text = ' ' + normalized.replace(/\s+/g, ' ').trim() + ' ';
        const words = text.trim().split(' ').filter(w => w && !stopWords.has(w));
        let best = null, bestScore = 0;
        topics.forEach(topic => {
            let score = 0;
            const counted = new Set();
            topic[0].forEach(key => {
                if (key.includes(' ')) { if (text.includes(' ' + key + ' ')) score += 3; return; }
                if (counted.has(stem(key))) return;
                if (words.some(w => w === key || stem(w) === stem(key) || (key.length >= 5 && w.length >= 5 && nearly(w, key)))) { score += 1; counted.add(stem(key)); }
            });
            if (score > bestScore) { best = topic; bestScore = score; }
        });
        return best;
    }
    let lastTopic = null;
    // History is kept per user so the next person signing in on this tab can't see it.
    const storeKey = 'healthtrack-chat:' + (root.dataset.user || '');
    try { Object.keys(sessionStorage).filter(k => k.startsWith('healthtrack-chat') && k !== storeKey).forEach(k => sessionStorage.removeItem(k)); } catch { }
    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    const ownData = /\b(my|me|am i|do i|have i|when is|when s|next)\b/;
    let history = [];
    try { history = JSON.parse(sessionStorage.getItem(storeKey) || '[]'); } catch { history = []; }
    function save() { try { sessionStorage.setItem(storeKey, JSON.stringify(history.slice(-40))); } catch { } }
    async function post(path, question) {
        try {
            const response = await fetch(path, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': token || '' },
                body: JSON.stringify({ question })
            });
            return response.ok ? await response.json() : null;
        } catch { return null; }
    }
    function add(text, user = false, link = null) {
        history.push({ text, user, link }); save();
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
        history = [];
        save();
        add('Hi! I’m the built-in HealthTrack guide. Ask how to use ' + (admin ? 'Users, assignments, reports or notifications.' : 'workouts, meals, metrics, progress or appointments.'));
    }
    async function ask(raw) {
        const question = raw.trim().slice(0, 300);
        if (!question) return;
        add(question, true);
        const normalized = question.toLowerCase().replace(/[^a-z0-9\s]/g, ' ');
        if (/diagnos|medicine|medication|symptom|treatment|medical advice|what should i eat|should i take/.test(normalized)) {
            add('I can explain HealthTrack features. For personal health advice, speak with a healthcare professional.');
            return;
        }
        if (!admin && ownData.test(normalized)) {
            const own = await post('/Chat/Own', question);
            if (own && own.answer) {
                add(own.answer, false, own.link ? [own.link, own.linkText] : null);
                return;
            }
        }
        const topic = matchTopic(admin ? adminTopics : clientTopics, normalized)
            || (/^(where|how|can i).*(it|that|this)/.test(normalized) ? lastTopic : null);
        if (topic) {
            lastTopic = topic;
            add(topic[1], false, [topic[2], topic[3]]);
        } else if (/^(hi|hello|hey|thanks|thank you)\b/.test(normalized)) {
            add('Happy to help. Choose a suggested question or ask about a HealthTrack feature.');
        } else {
            const fallback = await post('/Chat/Fallback', question);
            if (fallback && fallback.answer) { add(fallback.answer); return; }
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
    if (history.length) {
        const saved = history;
        history = [];
        messages.replaceChildren();
        saved.forEach(item => add(item.text, item.user, item.link));
    } else {
        greet();
    }
})();
