document.addEventListener("submit", function (event) {
    const message = event.target.getAttribute("data-confirm");
    if (message && !window.confirm(message)) {
        event.preventDefault();
    }
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
