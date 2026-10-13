import assert from "node:assert/strict";
import test from "node:test";

test("client instance IDs remain tab-specific when BroadcastChannel is unavailable", async () => {
  const globalDescriptors = new Map(["window", "BroadcastChannel"].map((name) => [name, Object.getOwnPropertyDescriptor(globalThis, name)]));
  const listeners = new Set();
  const writes = [];
  const originalId = "123e4567-e89b-42d3-a456-426614174000";
  const storage = new Map();
  let collided = false;
  const fakeWindow = {
    sessionStorage: {
      getItem: (key) => key === "kgv.browser.client-instance.v1" ? originalId : storage.get(key) ?? null,
      setItem: (key, value) => storage.set(key, value),
    },
    localStorage: {
      setItem(key, value) {
        writes.push({ key, message: JSON.parse(value) });
        if (!collided && key === "kgv.browser.client-instance-collision.v1") {
          const message = JSON.parse(value);
          if (message.type === "probe") {
            collided = true;
            queueMicrotask(() => {
              for (const listener of listeners) listener({ key, newValue: JSON.stringify({ type: "claimed", id: message.id, token: message.token }) });
            });
          }
        }
      },
    },
    addEventListener: (name, listener) => { if (name === "storage") listeners.add(listener); },
    setTimeout,
  };

  Object.defineProperty(globalThis, "window", { configurable: true, writable: true, value: fakeWindow });
  Object.defineProperty(globalThis, "BroadcastChannel", { configurable: true, writable: true, value: undefined });
  try {
    const { getBrowserClientInstanceId } = await import(`../lib/browser/client-instance.ts?fallback=${Date.now()}`);
    const id = await getBrowserClientInstanceId();
    assert.notEqual(id, originalId);
    assert.equal(storage.get("kgv.browser.client-instance.v1"), id);
    assert.equal(await getBrowserClientInstanceId(), id);

    const remoteToken = "remote-probe";
    for (const listener of listeners) {
      listener({ key: "kgv.browser.client-instance-collision.v1", newValue: JSON.stringify({ type: "probe", id, token: remoteToken }) });
    }
    assert.ok(writes.some(({ message }) => message.type === "claimed" && message.id === id && message.token === remoteToken));
  } finally {
    for (const [name, descriptor] of globalDescriptors) {
      if (descriptor) Object.defineProperty(globalThis, name, descriptor);
      else delete globalThis[name];
    }
  }
});
