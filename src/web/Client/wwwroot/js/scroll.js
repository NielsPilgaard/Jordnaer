window.scrollFunctions = {
  saveScrollPosition: function () {
    if (window.scrollY > 0) {
      sessionStorage.setItem('scrollPosition', window.scrollY);
    }
  },
  loadScrollPosition: function () {
    setTimeout(function () {
      window.scrollTo(0, sessionStorage.getItem('scrollPosition'));
    }, 500);
  }
};
