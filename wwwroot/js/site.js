function askConfirm(message, destructive) {
    return new Promise(resolve => {
        const previousFocus = document.activeElement;
        const overlay = document.createElement("div");
        overlay.className = "ht-confirm-overlay";
        overlay.innerHTML =
            '<div class="ht-confirm" role="alertdialog" aria-modal="true" aria-labelledby="ht-confirm-text">' +
            '<p id="ht-confirm-text"></p>' +
            '<div class="ht-confirm-actions">' +
            '<button type="button" class="btn btn-line" data-answer="no">Cancel</button>' +
            '<button type="button" class="btn ' + (destructive ? "btn-danger" : "btn-primary") + '" data-answer="yes">Confirm</button>' +
            '</div></div>';
        overlay.querySelector("p").textContent = message;
        const close = answer => {
            document.removeEventListener("keydown", onKey, true);
            overlay.remove();
            previousFocus?.focus?.();
            resolve(answer);
        };
        const onKey = event => {
            if (event.key === "Escape") {
                event.preventDefault();
                close(false);
            }
        };
        overlay.addEventListener("click", event => {
            if (event.target === overlay) {
                close(false);
            } else if (event.target.closest("[data-answer]")) {
                close(event.target.closest("[data-answer]").dataset.answer === "yes");
            }
        });
        document.addEventListener("keydown", onKey, true);
        document.body.appendChild(overlay);
        overlay.querySelector('[data-answer="yes"]').focus();
    });
}

document.addEventListener("submit", function (event) {
    const form = event.target;
    const message = form.getAttribute("data-confirm");
    if (!message || form.dataset.confirmed) {
        return;
    }
    event.preventDefault();
    const submitter = event.submitter;
    askConfirm(message, /delete|remove|lock/i.test(message) && !/unlock/i.test(message)).then(accepted => {
        if (accepted) {
            form.dataset.confirmed = "true";
            form.requestSubmit(submitter);
        }
    });
});

document.addEventListener("input", function (event) {
    const field = event.target;
    const counterId = field.getAttribute("data-counter");
    if (counterId) {
        const counter = document.getElementById(counterId);
        if (counter) {
            counter.textContent = field.value.length + " / " + field.getAttribute("maxlength");
        }
    }
});

// Keep the complete active pill visible when the navigation needs to scroll.
const navigation = document.querySelector('.ht-tabs');
const activeTab = navigation?.querySelector('.ht-tab.active');
if (navigation && activeTab) {
    const revealActiveTab = () => {
        const navBounds = navigation.getBoundingClientRect();
        const tabBounds = activeTab.getBoundingClientRect();
        const inset = 4;
        if (tabBounds.left < navBounds.left + inset) {
            navigation.scrollLeft += tabBounds.left - navBounds.left - inset;
        } else if (tabBounds.right > navBounds.right - inset) {
            navigation.scrollLeft += tabBounds.right - navBounds.right + inset;
        }
    };
    revealActiveTab();
    document.fonts.ready.then(revealActiveTab);
    new ResizeObserver(revealActiveTab).observe(navigation);
}
// Remember the page the user came from so back links return there (with its filters) instead of a fixed list.
try {
    const isFormPage = !!document.querySelector('.ht-form-page');
    const here = location.pathname + location.search;
    let current = sessionStorage.getItem('ht.current');
    let previous = sessionStorage.getItem('ht.previous');
    if (!isFormPage) {
        if (current && current !== here) {
            previous = current;
            sessionStorage.setItem('ht.previous', previous);
        }
        sessionStorage.setItem('ht.current', here);
        current = here;
    }
    const target = isFormPage ? current : previous;
    if (target && target !== here) {
        document.querySelectorAll('a.ht-back, .ht-form-actions a.btn-line').forEach(link => link.setAttribute('href', target));
    }
} catch { }

document.querySelectorAll('form.ht-filter, form[data-autofilter], form[data-journal-filter]').forEach(form => {
    const key = 'ht.filterFocus.' + location.pathname;
    const journal = form.hasAttribute('data-journal-filter');
    let timer;
    let submitting = false;
    const ready = field => {
        const type = field.dataset.calendarType || field.type;
        return !journal || type !== 'date' || !field.value || /^\d{4}-\d{2}-\d{2}$/.test(field.value);
    };
    const apply = field => {
        clearTimeout(timer);
        if (submitting || !ready(field)) return;
        if (field.type === 'search') {
            try { sessionStorage.setItem(key, field.name); } catch { }
        }
        form.requestSubmit();
    };
    form.addEventListener('submit', () => {
        clearTimeout(timer);
        submitting = true;
    });
    form.addEventListener('input', event => {
        const field = event.target;
        if (field.type !== 'search' && !journal) return;
        clearTimeout(timer);
        if (ready(field)) timer = setTimeout(() => apply(field), 400);
    });
    form.addEventListener('change', event => {
        if (event.target.type !== 'search') apply(event.target);
    });
    try {
        const name = sessionStorage.getItem(key);
        sessionStorage.removeItem(key);
        const field = name && form.querySelector('input[type="search"][name="' + name + '"]');
        if (field) {
            field.focus();
            field.setSelectionRange(field.value.length, field.value.length);
        }
    } catch { }
});

(function () {
    const serverFiltered = !!document.querySelector('form.ht-filter, form[data-autofilter], form[data-journal-filter]');
    const pageSize = 10;
    const text = el => el.textContent.replace(/\s+/g, ' ').trim();
    const lists = [];
    document.querySelectorAll('table.ht-table, table.table').forEach(table => {
        if (table.hasAttribute('data-nofilter')) return;
        const rows = [...table.tBodies].flatMap(body => [...body.rows]).filter(row => row.cells.length > 1);
        if (rows.length >= 5) lists.push({ anchor: table, place: table.closest('.table-responsive') || table, rows, headers: [...table.querySelectorAll('thead th')].map(text) });
    });
    document.querySelectorAll('ul.ht-journal, ul.ht-notes, [data-filterable]').forEach(box => {
        const rows = [...box.querySelectorAll(box.matches('ul') ? ':scope > li' : ':scope > article')];
        if (rows.length >= 5) lists.push({ anchor: box, place: box, rows, headers: [] });
    });
    lists.forEach(({ anchor, place, rows, headers }) => {
        const filterable = !serverFiltered;
        const paged = rows.length > pageSize;
        if (!filterable && !paged) return;
        let matches = rows;
        let page = 1;
        const pager = document.createElement('nav');
        pager.className = 'ht-pager';
        pager.setAttribute('aria-label', 'Pagination');
        const render = () => {
            const pages = Math.max(1, Math.ceil(matches.length / pageSize));
            page = Math.min(page, pages);
            const first = (page - 1) * pageSize;
            const visible = new Set(matches.slice(first, first + pageSize));
            rows.forEach(row => { row.hidden = !visible.has(row); });
            pager.hidden = matches.length <= pageSize;
            pager.innerHTML = '<span class="small text-muted">Showing ' + (first + 1) + '–' + Math.min(first + pageSize, matches.length) + ' of ' + matches.length + '</span>' +
                '<div class="d-flex align-items-center gap-2"><button type="button" class="btn btn-line btn-sm" data-page="-1"' + (page === 1 ? ' disabled' : '') + '>Previous</button>' +
                '<span class="small">Page ' + page + ' of ' + pages + '</span>' +
                '<button type="button" class="btn btn-line btn-sm" data-page="1"' + (page === pages ? ' disabled' : '') + '>Next</button></div>';
        };
        pager.addEventListener('click', event => {
            const button = event.target.closest('button[data-page]');
            if (!button) return;
            page += Number(button.dataset.page);
            render();
        });
        if (anchor.matches('[data-filterable]')) anchor.append(pager);
        else place.after(pager);
        if (filterable) {
            const bar = document.createElement('div');
            bar.className = 'ht-card ht-filter mb-3';
            bar.innerHTML = '<div class="ht-input-icon flex-grow-1"><i class="icon-search"></i><input type="search" class="form-control" placeholder="Search" aria-label="Search list" /></div>';
            const search = bar.querySelector('input');
            const selects = [];
            headers.forEach((title, column) => {
                if (!title) return;
                const values = [...new Set(rows.map(row => row.cells[column] ? text(row.cells[column]) : '').filter(value => value && value.length <= 30))];
                if (values.length < 2 || values.length > Math.min(8, rows.length / 2)) return;
                const select = document.createElement('select');
                select.className = 'form-select';
                select.setAttribute('aria-label', 'Filter by ' + title);
                select.innerHTML = '<option value="">All ' + title.toLowerCase() + '</option>';
                values.sort().forEach(value => select.add(new Option(value, value)));
                select.dataset.column = column;
                bar.appendChild(select);
                selects.push(select);
            });
            const apply = () => {
                const query = search.value.trim().toLowerCase();
                matches = rows.filter(row => (!query || text(row).toLowerCase().includes(query)) &&
                    selects.every(select => !select.value || (row.cells[select.dataset.column] && text(row.cells[select.dataset.column]) === select.value)));
                page = 1;
                render();
            };
            bar.addEventListener('input', apply);
            bar.addEventListener('change', apply);
            (anchor.closest('.ht-card') || anchor).before(bar);
        }
        render();
    });
})();
