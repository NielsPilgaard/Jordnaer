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

        if (!element) {
            return;
        }

        // Ensure the element is fully rendered before scrolling
        setTimeout(function () {
            element.scrollTo({
                top: element.scrollHeight,
                left: 0,
                behavior: 'instant' //  'auto', 'instant' or 'smooth' (default is 'auto')
            });
        }, 50); // Adjust the delay as needed
    }
};

// Improved scroll restoration for user search using IntersectionObserver
window.userSearchScroll = {
    // Store the ID of the most visible element and scroll position
    saveScrollPosition: function () {
        const scrollY = window.scrollY;
        if (scrollY === 0) {
            sessionStorage.removeItem('userSearch:scrollY');
            sessionStorage.removeItem('userSearch:visibleItemId');
            return;
        }

        // Save scroll position as fallback
        sessionStorage.setItem('userSearch:scrollY', scrollY.toString());

        // Find the most visible search result item
        const items = document.querySelectorAll('[id^="search-item-"]');
        if (items.length === 0) {
            return;
        }

        let mostVisibleItem = null;
        let maxVisibleArea = 0;

        items.forEach(item => {
            const rect = item.getBoundingClientRect();
            const viewportHeight = window.innerHeight;

            // Calculate visible area
            const visibleTop = Math.max(0, rect.top);
            const visibleBottom = Math.min(viewportHeight, rect.bottom);
            const visibleHeight = Math.max(0, visibleBottom - visibleTop);
            const visibleArea = visibleHeight * rect.width;

            if (visibleArea > maxVisibleArea) {
                maxVisibleArea = visibleArea;
                mostVisibleItem = item;
            }
        });

        if (mostVisibleItem) {
            sessionStorage.setItem('userSearch:visibleItemId', mostVisibleItem.id);
        }
    },

    // Restore scroll position using IntersectionObserver for reliability
    restoreScrollPosition: function () {
        const visibleItemId = sessionStorage.getItem('userSearch:visibleItemId');
        const scrollY = sessionStorage.getItem('userSearch:scrollY');

        if (!visibleItemId && !scrollY) {
            return;
        }

        // Wait for DOM to be ready
        requestAnimationFrame(() => {
            requestAnimationFrame(() => {
                let restored = false;

                // Try to restore using visible item ID (more reliable across screen sizes)
                if (visibleItemId) {
                    const element = document.getElementById(visibleItemId);
                    if (element) {
                        element.scrollIntoView({ behavior: 'instant', block: 'start' });
                        restored = true;
                    }
                }

                // Fallback to pixel position if item restoration failed
                if (!restored && scrollY) {
                    window.scrollTo({
                        top: parseInt(scrollY, 10),
                        left: 0,
                        behavior: 'instant'
                    });
                }
            });
        });
    }
};

// Group search scroll restoration (same pattern as user search)
window.groupSearchScroll = {
    saveScrollPosition: function () {
        const scrollY = window.scrollY;
        if (scrollY === 0) {
            sessionStorage.removeItem('groupSearch:scrollY');
            sessionStorage.removeItem('groupSearch:visibleItemId');
            return;
        }

        sessionStorage.setItem('groupSearch:scrollY', scrollY.toString());

        const items = document.querySelectorAll('[id^="group-search-item-"]');
        if (items.length === 0) {
            return;
        }

        let mostVisibleItem = null;
        let maxVisibleArea = 0;

        items.forEach(item => {
            const rect = item.getBoundingClientRect();
            const viewportHeight = window.innerHeight;
            const visibleTop = Math.max(0, rect.top);
            const visibleBottom = Math.min(viewportHeight, rect.bottom);
            const visibleHeight = Math.max(0, visibleBottom - visibleTop);
            const visibleArea = visibleHeight * rect.width;

            if (visibleArea > maxVisibleArea) {
                maxVisibleArea = visibleArea;
                mostVisibleItem = item;
            }
        });

        if (mostVisibleItem) {
            sessionStorage.setItem('groupSearch:visibleItemId', mostVisibleItem.id);
        }
    },

    restoreScrollPosition: function () {
        const visibleItemId = sessionStorage.getItem('groupSearch:visibleItemId');
        const scrollY = sessionStorage.getItem('groupSearch:scrollY');

        if (!visibleItemId && !scrollY) {
            return;
        }

        requestAnimationFrame(() => {
            requestAnimationFrame(() => {
                let restored = false;

                if (visibleItemId) {
                    const element = document.getElementById(visibleItemId);
                    if (element) {
                        element.scrollIntoView({ behavior: 'instant', block: 'start' });
                        restored = true;
                    }
                }

                if (!restored && scrollY) {
                    window.scrollTo({
                        top: parseInt(scrollY, 10),
                        left: 0,
                        behavior: 'instant'
                    });
                }
            });
        });
    }
};
