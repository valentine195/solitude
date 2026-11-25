(function () {
  'use strict';

  function scope(state) {
    if (!state.active) return 'suspended';
    if (state.dialog) return state.dialog;
    if (state.prototype) return 'prototype';
    return state.open ? 'inventory' : 'closed';
  }

  function intent(type, current, extra) {
    return { type, scope: current, consume: true, ...extra };
  }

  function keyboard(state, event) {
    const current = scope(state);
    if (current === 'suspended' || event.defaultPrevented || event.isComposing ||
        event.ctrlKey || event.altKey || event.metaKey) return null;
    const key = event.key.toLowerCase();

    // A held Back/confirm/shortcut cannot cascade into the next interface.
    if (event.repeat && ['escape', 'enter', ' ', 'x', 'i'].includes(key)) {
      return intent(null, current);
    }
    if (key === 'escape') {
      return current === 'closed' ? null : intent('back', current);
    }
    if (event.editable) return null;
    if (current === 'quantity' || current === 'world' || current === 'prototype') return null;
    if (current === 'closed') return key === 'i' ? intent('reopen', current) : null;
    if (event.prototypeTarget) return null;

    const directions = {
      arrowleft: [-1, 0], arrowright: [1, 0], arrowup: [0, -1], arrowdown: [0, 1]
    };
    if (directions[key]) {
      const [dx, dy] = directions[key];
      return intent('navigate', current, { dx, dy });
    }
    if (key === 'tab' && !event.shiftKey && event.slotTarget && !state.moving) {
      return intent('actions', current);
    }
    if (key === 'x' && state.hasItem && !state.moving) return intent('drop', current);
    return null;
  }

  function createGamepadRouter() {
    let previous = [], armed = false, lastNavigation = -Infinity;
    function reset() {
      previous = [];
      armed = false;
      lastNavigation = -Infinity;
    }
    function next(state, pad, now) {
      if (!pad) { reset(); return null; }
      const current = scope(state);
      const buttons = pad.buttons.map(button => typeof button === 'boolean' ? button : button.pressed);
      const axes = pad.axes || [];
      const neutral = !buttons.some(Boolean) && axes.every(value => Math.abs(value) < 0.4);
      if (current === 'suspended') { reset(); return null; }
      if (!armed) {
        previous = buttons;
        armed = neutral;
        return null;
      }
      const pressed = index => !!buttons[index] && !previous[index];
      const edges = [0, 1, 2, 3, 4, 5].map(pressed);
      previous = buttons;

      // One intent per frame. Back takes precedence over simultaneous presses.
      if (edges[1]) return current === 'closed' ? null : intent('back', current);
      if (edges[0]) return intent(current === 'closed' ? 'reopen' : 'activate', current);
      if (current === 'inventory') {
        if (edges[2] && state.hasItem && !state.moving) return intent('drop', current);
        if (edges[3]) return intent('actions', current);
        if (edges[4] || edges[5]) return intent('slots', current, { direction: edges[4] ? -1 : 1 });
      }

      const dx = buttons[14] || axes[0] < -0.5 ? -1 : buttons[15] || axes[0] > 0.5 ? 1 : 0;
      const dy = buttons[12] || axes[1] < -0.5 ? -1 : buttons[13] || axes[1] > 0.5 ? 1 : 0;
      if ((dx || dy) && now - lastNavigation >= 180) {
        lastNavigation = now;
        return intent('navigate', current, { dx, dy });
      }
      return null;
    }
    return { next, reset };
  }

  const policy = { scope, keyboard, createGamepadRouter };
  if (typeof module !== 'undefined' && module.exports) module.exports = policy;
  else window.InventoryInput = policy;
})();
