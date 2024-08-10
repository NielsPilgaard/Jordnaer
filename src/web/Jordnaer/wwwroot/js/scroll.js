window.scrollFunctions = {
    saveScrollPosition: function (prefix) {
        if (window.scrollY > 0) {
            sessionStorage.setItem(`${prefix}:scrollPosition`, window.scrollY);
        }
    },
    loadScrollPosition: function (prefix) {
            window.scrollTo(0, sessionStorage.getItem(`${prefix}:scrollPosition`));
    },
    scrollToBottomOfElement: function (selector) {
        const element = document.querySelector(selector);

        if (!element) return;
        
        element.scrollTop = element.scrollHeight;
    }
};
