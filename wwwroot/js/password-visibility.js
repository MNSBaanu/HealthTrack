(() => {
    const controls = [];
    let count = 0;
    const enhance = root => root.querySelectorAll('input[type="password"]').forEach(input => {
        if (input.closest('.ht-password-field')) return;
        input.id ||= `ht-password-${++count}`;
        const fieldName = input.labels?.[0]?.textContent.trim().toLowerCase() || 'password';
        const wrapper = document.createElement('div');
        wrapper.className = 'ht-password-field';
        input.before(wrapper);
        wrapper.append(input);
        const toggle = document.createElement('button');
        toggle.type = 'button';
        toggle.className = 'ht-password-toggle';
        toggle.disabled = input.disabled;
        toggle.setAttribute('aria-controls', input.id);
        const icon = document.createElement('i');
        icon.setAttribute('aria-hidden', 'true');
        toggle.append(icon);
        wrapper.append(toggle);
        const setVisible = visible => {
            input.type = visible ? 'text' : 'password';
            const label = `${visible ? 'Hide' : 'Show'} ${fieldName}`;
            toggle.setAttribute('aria-label', label);
            toggle.setAttribute('aria-pressed', String(visible));
            toggle.title = label;
            icon.className = visible ? 'icon-eye-off' : 'icon-eye';
        };
        setVisible(false);
        toggle.addEventListener('click', () => setVisible(input.type === 'password'));
        input.form?.addEventListener('submit', () => setVisible(false));
        input.form?.addEventListener('reset', () => setVisible(false));
        controls.push(() => setVisible(false));
    });
    enhance(document);
    window.htEnhancePasswords = enhance;
    window.addEventListener('pagehide', () => controls.forEach(hide => hide()));
})();
