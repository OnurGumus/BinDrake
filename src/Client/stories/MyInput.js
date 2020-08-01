import { init } from "./MyInput.fs"
class MyInput extends HTMLInputElement {
    constructor(...args) {
      const self = super(...args);
      self.min = this.getAttribute('min');
      self.max = this.getAttribute('max');
      init(self);
      return self;
    }
  }
  if (!customElements.get("my-input"))
    customElements.define('my-input', MyInput, { extends: 'input' });