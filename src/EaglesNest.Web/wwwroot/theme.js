(function () {
    const storageKey = 'eaglesnest.theme';
    const availableThemes = new Set(['army']);
    const savedTheme = localStorage.getItem(storageKey);
    const theme = availableThemes.has(savedTheme) ? savedTheme : 'army';
    document.documentElement.dataset.theme = theme;

    window.eaglesNestTheme = {
        set: function (name) {
            if (!availableThemes.has(name)) return;
            localStorage.setItem(storageKey, name);
            document.documentElement.dataset.theme = name;
        }
    };
})();
