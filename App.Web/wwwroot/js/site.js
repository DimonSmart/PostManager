window.postManager = window.postManager || {};
window.postManager.scrollToElementById = (id) => {
  if (!id) {
    return;
  }

  const element = document.getElementById(id);
  if (!element) {
    return;
  }

  element.scrollIntoView({ block: "center", behavior: "smooth" });
};

window.postManager.scrollToSelector = (selector) => {
  if (!selector) {
    return;
  }

  const element = document.querySelector(selector);
  if (!element) {
    return;
  }

  element.scrollIntoView({ block: "center", behavior: "smooth" });
};
