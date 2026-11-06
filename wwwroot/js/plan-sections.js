(() => {
    const tiles = [...document.querySelectorAll('a.workout-week-day[href^="#day-"]')];
    if (!tiles.length) return;
    const toggleAll = document.querySelector('[data-toggle-all-days]');
    const toggleLabel = toggleAll?.querySelector('[data-toggle-all-label]');
    const emptyMessage = document.querySelector('[data-no-days]');
    const idOf = tile => tile.getAttribute('href').slice(1);
    const selected = new Set(tiles.map(idOf));

    const render = () => {
        document.querySelectorAll('details.workout-checklist-day').forEach(section => { section.hidden = !selected.has(section.id); });
        tiles.forEach(tile => tile.setAttribute('aria-pressed', String(selected.has(idOf(tile)))));
        const all = selected.size === tiles.length;
        if (toggleAll) {
            toggleAll.setAttribute('aria-pressed', String(all));
            toggleLabel.textContent = all ? 'Deselect all days' : 'Select all days';
        }
        if (emptyMessage) emptyMessage.hidden = selected.size > 0;
    };

    tiles.forEach(tile => tile.addEventListener('click', event => {
        event.preventDefault();
        const id = idOf(tile);
        if (selected.size === 1 && selected.has(id)) {
            tiles.forEach(other => selected.add(idOf(other)));
        } else {
            selected.clear();
            selected.add(id);
            const section = document.getElementById(id);
            if (section) section.open = true;
        }
        render();
    }));

    toggleAll?.addEventListener('click', () => {
        if (selected.size === tiles.length) selected.clear();
        else tiles.forEach(tile => selected.add(idOf(tile)));
        render();
    });

    const showHashDay = () => {
        const id = location.hash.slice(1);
        if (!id.startsWith('day-') || !tiles.some(tile => idOf(tile) === id)) return;
        selected.clear();
        selected.add(id);
        const section = document.getElementById(id);
        if (section) section.open = true;
        render();
    };
    window.addEventListener('hashchange', showHashDay);
    showHashDay();
    render();
})();
