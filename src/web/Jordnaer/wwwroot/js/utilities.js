window.utilities = {
    hideElement: function (selector) {
        const element = document.querySelector(selector);

        if (!element) return;

        element.style.setProperty("display", "none", "important");
    },
    showElement: function (selector) {
        const element = document.querySelector(selector);

        if (!element) return;

        element.style.removeProperty("display");
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
    },

    copyToClipboard: async function (text) {
        try {
            await navigator.clipboard.writeText(text);
            return true;
        } catch {
            return false;
        }
    },

    openShareWindow: function (url, windowName) {
        const width = 600;
        const height = 400;
        const left = (screen.width - width) / 2;
        const top = (screen.height - height) / 2;
        window.open(url, windowName, `width=${width},height=${height},left=${left},top=${top}`);
    },

    canShare: function () {
        return typeof navigator.share === 'function';
    },

    nativeShare: async function (title, text, url) {
        try {
            await navigator.share({ title, text, url });
            return true;
        } catch {
            return false;
        }
    }
};
