window.scrollFunctions = {
  saveScrollPosition: function () {
    sessionStorage.setItem('scrollPosition', window.scrollY);
  },
  loadScrollPosition: function () {
    setTimeout(function () {
      window.scrollTo(0, sessionStorage.getItem('scrollPosition'));
    }, 500);
  }
};
