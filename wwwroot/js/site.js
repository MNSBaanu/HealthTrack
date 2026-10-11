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
