window.scrollFunctions = {
    saveScrollPosition: function (prefix) {
        if (window.scrollY > 0) {
            sessionStorage.setItem(`${prefix}:scrollPosition`, window.scrollY);
        }
    },
    loadScrollPosition: function (prefix) {
        const scrollPosition = sessionStorage.getItem(`${prefix}:scrollPosition`);
        if (!scrollPosition) {
            return;
        }

        setTimeout(function () {
            window.scrollTo({
                top: scrollPosition,
                left: 0,
                behavior: 'instant' //  'auto', 'instant' or 'smooth' (default is 'auto')
            });
        }, 50); // The delay is required to ensure the scroll position is restored after the page has been rendered
    },
    scrollToBottomOfElement: function (selector) {
        const element = document.querySelector(selector);

        if (!element) return;

        element.scrollTop = element.scrollHeight;
    }
};
