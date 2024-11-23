window._registerPage = function (type, page) {
  window._pagesToInvoke ??= {};

  window._pagesToInvoke[type] = page;
};

window._unregisterPage = function (type) {
  if (window._pagesToInvoke) {
    ref = window._pagesToInvoke[type];
    delete window._pagesToInvoke[type];

    return ref;
  }
};

window.getPageRef = function (type) {
  if (window._pagesToInvoke) {
    return window._pagesToInvoke[type];
  }
};

window._unregisterAll = function () {
  if (window._pagesToInvoke) {
    refs = Object.values(window._pagesToInvoke);
    window._pagesToInvoke = {};

    return refs;
  } else {
    return [];
  }
};
