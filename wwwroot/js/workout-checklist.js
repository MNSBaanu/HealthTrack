(() => {
    const list = document.querySelector('[data-workout-checklist]');
    if (!list) return;
    const error = list.querySelector('[data-checklist-error]');
    const announcement = list.querySelector('[data-checklist-status]');
    const refreshProgress = () => {
        const tasks = [...list.querySelectorAll('[data-task][data-counted="true"]')];
        const done = tasks.filter(task => task.dataset.completed === 'true').length;
        list.querySelector('[data-completed-count]').textContent = done;
        list.querySelector('[data-remaining-count]').textContent = tasks.length - done;
        list.querySelector('progress').value = done;
        list.querySelector('progress').textContent = `${tasks.length ? Math.floor(done * 100 / tasks.length) : 0}%`;
        list.querySelectorAll('.workout-checklist-day').forEach(day => {
            day.querySelector('[data-day-completed]').textContent = day.querySelectorAll('[data-task][data-completed="true"]').length;
        });
        list.querySelectorAll('[data-week-date]').forEach(day => {
            const dayTasks = tasks.filter(task => task.dataset.taskDate === day.dataset.weekDate);
            if (dayTasks.length) day.querySelector('[data-week-summary]').textContent = `${dayTasks.filter(task => task.dataset.completed === 'true').length}/${dayTasks.length} done`;
        });
    };
    list.querySelectorAll('.workout-checklist-form').forEach(form => {
        const checkbox = form.querySelector('input[type="checkbox"]');
        checkbox.addEventListener('change', () => form.requestSubmit());
        form.addEventListener('submit', async event => {
            event.preventDefault();
            const task = form.closest('[data-task]');
            if (task.getAttribute('aria-busy') === 'true') return;
            const previous = task.dataset.completed === 'true';
            const body = new FormData(form);
            checkbox.disabled = true;
            task.setAttribute('aria-busy', 'true');
            error.hidden = true;
            try {
                const response = await fetch(form.action, {
                    method: 'POST', body,
                    headers: { 'X-Requested-With': 'XMLHttpRequest', 'Accept': 'application/json' }
                });
                const result = response.headers.get('content-type')?.includes('application/json') ? await response.json() : null;
                if (!response.ok || typeof result?.completed !== 'boolean')
                    throw new Error(result?.message || 'Could not save your progress. Refresh the page and try again.');
                checkbox.checked = result.completed;
                task.dataset.completed = String(result.completed);
                task.classList.toggle('is-complete', result.completed);
                task.querySelector('[data-task-state]').textContent = result.completed ? 'Completed' : 'To do today';
                refreshProgress();
                announcement.textContent = `${task.querySelector('label').textContent.trim()}: ${result.completed ? 'completed' : 'marked as remaining'}. Progress saved.`;
            } catch (failure) {
                checkbox.checked = previous;
                error.textContent = failure.message || 'Could not save your progress. Try again.';
                error.hidden = false;
            } finally {
                checkbox.disabled = false;
                task.removeAttribute('aria-busy');
            }
        });
    });
})();
