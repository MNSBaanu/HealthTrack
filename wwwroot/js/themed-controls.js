(() => {
    if (!window.flatpickr) return;
    const minimumFor = (input, type, format) => {
        let minimum = input.getAttribute('min') || '';
        if (input.dataset.notPast === 'true') {
            const now = new Date(Date.now() + (type === 'datetime-local' ? 60000 : 0));
            const parts = new Intl.DateTimeFormat('en-GB', {
                timeZone: input.dataset.futureZone || 'Asia/Colombo',
                year: 'numeric', month: '2-digit', day: '2-digit',
                hour: '2-digit', minute: '2-digit', hourCycle: 'h23'
            }).formatToParts(now);
            const part = name => parts.find(p => p.type === name).value;
            const today = `${part('year')}-${part('month')}-${part('day')}`;
            const current = type === 'date' ? today : `${today}T${part('hour')}:${part('minute')}`;
            if (current > minimum) minimum = current;
        }
        if (input.dataset.dateAfter) {
            const start = document.querySelector(input.dataset.dateAfter)?._flatpickr?.selectedDates[0];
            if (start) {
                const endMinimum = flatpickr.formatDate(new Date(start.getTime() + (type === 'datetime-local' ? 60000 : 0)), format);
                if (endMinimum > minimum) minimum = endMinimum;
            }
        }
        return minimum || undefined;
    };
    const enhance = root => root.querySelectorAll('input[type="date"], input[type="datetime-local"], input[type="time"]').forEach(input => {
        if (input._flatpickr) return;
        const type = input.type;
        const hasTime = type !== 'date';
        const format = type === 'date' ? 'Y-m-d' : type === 'time' ? 'H:i' : 'Y-m-d\\TH:i';
        const placeholder = type === 'date' ? 'Choose date · YYYY-MM-DD' : type === 'time' ? 'Choose time · HH:MM' : 'Choose date & time';
        const minimum = minimumFor(input, type, format);
        const maximum = input.getAttribute('max');
        input.placeholder ||= placeholder;
        input.classList.add('ht-calendar-input');
        input.dataset.calendarType = type;
        flatpickr(input, {
            dateFormat: format,
            enableTime: hasTime,
            noCalendar: type === 'time',
            time_24hr: true,
            minuteIncrement: input.step && Number(input.step) >= 60 ? Number(input.step) / 60 : 1,
            allowInput: true,
            disableMobile: true,
            static: !!input.closest('.modal'),
            minDate: type !== 'time' && minimum ? minimum : undefined,
            maxDate: type !== 'time' && maximum ? maximum : undefined,
            minTime: type === 'time' && minimum ? minimum : undefined,
            maxTime: type === 'time' && maximum ? maximum : undefined,
            onOpen: (_, __, picker) => {
                if (input.dataset.notPast || input.dataset.dateAfter) {
                    picker.set('minDate', minimumFor(input, type, format));
                }
            },
            onReady: (_, __, picker) => {
                const label = input.labels?.[0]?.textContent.trim() || input.getAttribute('aria-label') || 'Choose date';
                picker.calendarContainer.setAttribute('role', 'dialog');
                picker.calendarContainer.setAttribute('aria-label', label);
                const footer = document.createElement('div');
                footer.className = 'ht-calendar-footer';
                const clear = document.createElement('button');
                clear.type = 'button';
                clear.className = 'btn btn-sm btn-line';
                clear.textContent = 'Clear';
                clear.addEventListener('click', () => { picker.clear(); input.focus(); picker.close(); });
                const done = document.createElement('button');
                done.type = 'button';
                done.className = 'btn btn-sm btn-primary';
                done.textContent = 'Done';
                done.addEventListener('click', () => { input.focus(); picker.close(); });
                footer.append(clear, done);
                picker.calendarContainer.append(footer);
            },
            onChange: () => {
                input.dispatchEvent(new Event('input', { bubbles: true }));
                document.querySelectorAll('input[data-date-after]').forEach(end => {
                    if (end.dataset.dateAfter === `#${input.id}` && end._flatpickr) {
                        const endType = end.dataset.calendarType;
                        end._flatpickr.set('minDate', minimumFor(end, endType, endType === 'date' ? 'Y-m-d' : 'Y-m-d\\TH:i'));
                    }
                });
            }
        });
    });
    enhance(document);
    window.htEnhanceDates = enhance;
})();
