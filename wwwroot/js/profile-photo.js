(() => {
    const input = document.getElementById('Photo');
    const preview = document.getElementById('profile-photo-preview');
    const initials = document.getElementById('profile-photo-initials');
    const error = document.getElementById('profile-photo-error');
    if (!input || !preview) return;
    const savedSource = preview.getAttribute('src');
    let previewUrl;
    const reset = () => {
        if (previewUrl) URL.revokeObjectURL(previewUrl);
        previewUrl = null;
        if (savedSource) preview.src = savedSource;
        else preview.removeAttribute('src');
        preview.hidden = !savedSource;
        initials.hidden = !!savedSource;
    };
    input.addEventListener('change', () => {
        reset();
        error.textContent = '';
        const file = input.files[0];
        if (!file) return;
        if (!['image/jpeg', 'image/png'].includes(file.type) || file.size > 2 * 1024 * 1024 || !file.size) {
            error.textContent = 'Choose a JPG or PNG photo smaller than 2 MB.';
            input.value = '';
            return;
        }
        previewUrl = URL.createObjectURL(file);
        const image = new Image();
        image.onload = () => {
            if (image.src !== previewUrl) return;
            if (image.naturalWidth > 4096 || image.naturalHeight > 4096) {
                error.textContent = 'Choose a photo up to 4096 pixels on each side.';
                input.value = '';
                reset();
                return;
            }
            preview.src = previewUrl;
            preview.hidden = false;
            initials.hidden = true;
        };
        image.onerror = () => {
            if (image.src !== previewUrl) return;
            error.textContent = 'This photo could not be opened. Choose a valid JPG or PNG.';
            input.value = '';
            reset();
        };
        image.src = previewUrl;
    });
})();
