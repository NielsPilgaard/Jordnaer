function onWindowScroll(callback) {
    window.onscroll = function () {
        if (window.innerHeight + window.scrollY >= document.body.offsetHeight) {
            callback.invokeMethodAsync("SetAppBarVisibility", true);
        } else {
            callback.invokeMethodAsync("SetAppBarVisibility", false);
        }
    };
}
