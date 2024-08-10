window.utilities = {
    hideElement: function (selector) {
        const element = document.querySelector(selector);

        if (!element) return;

        element.style.setProperty("display", "none", "important")
    },

    focusElement: function (selector) {
        const element = document.querySelector(selector);

        if (!element) return;

        element.focus();
    },

    getGeolocation: async function () {
        const position = await new Promise((resolve, reject) => {
            navigator.geolocation.getCurrentPosition(resolve, reject);
        });

        return position.coords;
    },

    updatePathAndQueryString: function (newUri) {
        const currentUrl = new URL(window.location.href);
        const newUrl = new URL(newUri, window.location.origin);

        if (currentUrl.pathname !== newUrl.pathname || currentUrl.search !== newUrl.search) {
            window.history.pushState({}, '', newUrl.pathname + newUrl.search);
        }
    }
};
