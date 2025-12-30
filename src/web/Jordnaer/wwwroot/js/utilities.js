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
        // Check if mobile device
        const isMobile = /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent);

        if (isMobile) {
            // On mobile, open in a new tab (full screen)
            window.open(url, '_blank');
        } else {
            // On desktop, use a centered popup with better dimensions
            const width = Math.min(1200, screen.width * 0.8);  // Max 1200px or 80% of screen width
            const height = Math.min(1000, screen.height * 0.8); // Max 1000px or 80% of screen height
            const left = (screen.width - width) / 2;
            const top = (screen.height - height) / 2;

            const features = [
                `width=${width}`,
                `height=${height}`,
                `left=${left}`,
                `top=${top}`,
                'menubar=no',
                'toolbar=no',
                'location=yes',
                'status=no',
                'scrollbars=yes',
                'resizable=yes'
            ].join(',');

            window.open(url, windowName, features);
        }
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
