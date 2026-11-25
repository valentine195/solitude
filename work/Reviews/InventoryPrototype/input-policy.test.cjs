const test = require('node:test');
const assert = require('node:assert/strict');
const input = require('./input-policy.js');

const inventory = { active: true, open: true, hasItem: true, moving: false };
const key = (name, extra = {}) => ({ key: name, ...extra });
const pad = (...pressed) => ({ buttons: Array.from({ length: 16 }, (_, i) => pressed.includes(i)), axes: [0, 0] });
function armedRouter() {
  const router = input.createGamepadRouter();
  router.next(inventory, pad(), 0);
  return router;
}

test('closed inventory ignores item shortcuts and navigation; explicit I reopens', () => {
  const closed = { ...inventory, open: false };
  for (const name of ['x', 'X', 'ArrowRight', 'Tab', 'Escape']) {
    assert.equal(input.keyboard(closed, key(name, { slotTarget: true })), null);
  }
  assert.equal(input.keyboard(closed, key('i')).type, 'reopen');
  assert.equal(input.keyboard(closed, key('i', { prototypeTarget: true })).type, 'reopen');
  assert.equal(input.keyboard(closed, key('i', { editable: true })), null);
  assert.equal(input.keyboard(inventory, key('x')).type, 'drop');
});

test('modal and destination steps cannot start another item operation', () => {
  for (const state of [{ ...inventory, dialog: 'quantity' }, { ...inventory, dialog: 'world' },
                       { ...inventory, prototype: true }, { ...inventory, moving: true }]) {
    assert.equal(input.keyboard(state, key('x')), null);
    assert.equal(input.keyboard(state, key('Escape')).type, 'back');
  }
});

test('held keys cannot cascade Back, reopen, drop, or confirm after a scope transition', () => {
  for (const name of ['Escape', 'Enter', ' ', 'x', 'i']) {
    const action = input.keyboard(inventory, key(name, { repeat: true }));
    assert.equal(action.type, null);
    assert.equal(action.consume, true);
  }
  assert.equal(input.keyboard(inventory, key('ArrowDown', { repeat: true })).type, 'navigate');
});

test('typing, composition, and platform shortcuts are not treated as inventory commands', () => {
  for (const extra of [{ editable: true }, { ctrlKey: true }, { metaKey: true },
                       { altKey: true }, { isComposing: true }, { prototypeTarget: true }]) {
    assert.equal(input.keyboard(inventory, key('x', extra)), null);
  }
});

test('closed controller X/Y/bumpers cannot trigger inventory actions', () => {
  for (const button of [2, 3, 4, 5]) {
    const router = armedRouter();
    assert.equal(router.next({ ...inventory, open: false }, pad(button), 200), null);
  }
});

test('controller dialogs accept only their own activation/back/navigation', () => {
  for (const dialog of ['quantity', 'world']) {
    const state = { ...inventory, dialog };
    for (const button of [2, 3, 4, 5]) {
      assert.equal(armedRouter().next(state, pad(button), 200), null);
    }
    assert.deepEqual(armedRouter().next(state, pad(0), 200), { type: 'activate', scope: dialog, consume: true });
    assert.equal(armedRouter().next(state, pad(1), 200).type, 'back');
  }
});

test('simultaneous Back wins and held buttons cannot act in the resulting screen', () => {
  const router = armedRouter();
  assert.equal(router.next(inventory, pad(0, 1, 2), 200).type, 'back');
  assert.equal(router.next({ ...inventory, open: false }, pad(0, 1, 2), 400), null);
  router.next({ ...inventory, open: false }, pad(), 600);
  assert.equal(router.next({ ...inventory, open: false }, pad(0), 800).type, 'reopen');
});

test('connection/focus recovery requires release before buttons can act', () => {
  const router = input.createGamepadRouter();
  assert.equal(router.next(inventory, pad(2), 0), null);
  assert.equal(router.next(inventory, pad(2), 200), null);
  router.next(inventory, pad(), 400);
  assert.equal(router.next(inventory, pad(2), 600).type, 'drop');
  router.next({ ...inventory, active: false }, pad(2), 800);
  assert.equal(router.next(inventory, pad(2), 1000), null);
  router.next(inventory, pad(), 1200);
  assert.equal(router.next(inventory, pad(2), 1400).type, 'drop');
});

test('holding controller Drop across inventory reopening cannot open a dialog', () => {
  const router = armedRouter();
  assert.equal(router.next({ ...inventory, open: false }, pad(2), 200), null);
  assert.equal(router.next(inventory, pad(2), 400), null);
  router.next(inventory, pad(), 600);
  assert.equal(router.next(inventory, pad(2), 800).type, 'drop');
});

test('destination selection permits scoped navigation but blocks a new controller Drop', () => {
  const moving = { ...inventory, moving: true };
  assert.equal(armedRouter().next(moving, pad(2), 200), null);
  assert.deepEqual(armedRouter().next(moving, pad(15), 200), {
    type: 'navigate', scope: 'inventory', consume: true, dx: 1, dy: 0
  });
});

test('quantity entry retains native Tab, Enter, and arrow handling inside its dialog', () => {
  const state = { ...inventory, dialog: 'quantity' };
  for (const name of ['Tab', 'Enter', 'ArrowRight', 'x', 'i']) {
    assert.equal(input.keyboard(state, key(name, { editable: true })), null);
  }
  assert.deepEqual(input.keyboard(state, key('Escape', { editable: true })), {
    type: 'back', scope: 'quantity', consume: true
  });
});
