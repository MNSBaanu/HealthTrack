(() => {
    const jq = window.jQuery;

    const init = form => {
        const button = form.querySelector('button[type="submit"]');
        if (!button) return;
        const hint = form.querySelector('[data-submit-hint]');
        const checkboxes = Array.from(form.querySelectorAll('input[type="checkbox"][data-require-checked]'));
        const dates = Array.from(form.querySelectorAll('input[type="date"], input[data-calendar-type="date"]'));
        const required = Array.from(form.querySelectorAll('input[data-val-required]')).filter(input => input.type !== 'checkbox');
        const getValidator = () => (jq ? jq(form).data('validator') : null);

        const datesInRange = () => dates.every(input =>
            !input.value || ((!input.max || input.value <= input.max) && (!input.min || input.value >= input.min)));

        const isReady = validator => {
            const fieldsValid = validator.checkForm();
            validator.reset();
            return fieldsValid && required.every(input => input.value.trim() !== '')
                && checkboxes.every(box => box.checked) && datesInRange();
        };

        const update = () => {
            const validator = getValidator();
            if (!validator) return;
            const ready = isReady(validator);
            button.disabled = !ready;
            if (hint) hint.hidden = ready;
        };

        const syncCheckbox = box => {
            const message = form.querySelector(`[data-valmsg-for="${box.name}"]`);
            if (!message) return;
            message.textContent = box.checked ? '' : (box.dataset.valRequired || '');
            message.classList.toggle('field-validation-error', !box.checked);
            message.classList.toggle('field-validation-valid', box.checked);
        };

        checkboxes.forEach(box => box.addEventListener('change', () => syncCheckbox(box)));
        form.addEventListener('input', update);
        form.addEventListener('change', update);
        window.addEventListener('pageshow', update);
        window.addEventListener('load', () => {
            if (!getValidator()) {
                button.disabled = false;
                if (hint) hint.hidden = true;
                return;
            }
            update();
        });

        if (!jq) {
            button.disabled = false;
            if (hint) hint.hidden = true;
            return;
        }
        jq(update);
    };

    document.querySelectorAll('form[data-submit-when-valid]').forEach(init);
})();
