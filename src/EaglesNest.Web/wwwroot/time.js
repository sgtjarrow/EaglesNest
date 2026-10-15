window.eaglesNestTime = {
    localize: function () {
        document.querySelectorAll('time[data-utc]').forEach(function (element) {
            const value = new Date(element.dataset.utc);
            if (!Number.isNaN(value.getTime())) {
                element.textContent = value.toLocaleString(undefined, { dateStyle: 'medium', timeStyle: 'short' });
                element.title = element.dataset.utc;
            }
        });
    }
};
