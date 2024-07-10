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

    getGeolocation: async function() {
        const position = await new Promise((resolve, reject) => {
            navigator.geolocation.getCurrentPosition(resolve, reject);
        });

        return position.coords;
    }
};
