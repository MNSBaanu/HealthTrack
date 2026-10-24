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

document.querySelectorAll('form.ht-filter, form[data-autofilter]').forEach(form => {
    const key = 'ht.filterFocus.' + location.pathname;
    let timer;
    form.addEventListener('input', event => {
        if (event.target.type !== 'search') return;
        clearTimeout(timer);
        timer = setTimeout(() => {
            try { sessionStorage.setItem(key, event.target.name); } catch { }
            form.requestSubmit();
        }, 400);
    });
    form.addEventListener('change', event => {
        if (event.target.type !== 'search') form.requestSubmit();
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
