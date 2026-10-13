const storageKey = "kgv.browser.client-instance.v1";
const channelName = "kgv-browser-client-instance-v1";
const collisionStorageKey = "kgv.browser.client-instance-collision.v1";
let clientInstanceId: Promise<string> | null = null;
let collisionChannel: BroadcastChannel | null = null;
let activeClientInstanceId: string | null = null;
const pendingStorageProbes = new Set<string>();
const collidedStorageProbes = new Set<string>();
let storageCollisionListenerInstalled = false;

function createId() {
  if (typeof crypto !== "undefined" && typeof crypto.randomUUID === "function") return crypto.randomUUID();
  return "xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx".replace(/[xy]/g, (character) => {
    const value = Math.floor(Math.random() * 16);
    return (character === "x" ? value : (value & 0x3) | 0x8).toString(16);
  });
}

function readStoredId() {
  try {
    const value = window.sessionStorage.getItem(storageKey);
    return value && /^[0-9a-f]{8}-[0-9a-f]{4}-4[0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(value) ? value : null;
  } catch { return null; }
}

function storeId(value: string) {
  try { window.sessionStorage.setItem(storageKey, value); } catch { /* The in-memory value remains usable. */ }
}

function sendStorageCollisionMessage(message: { type: "probe" | "claimed"; id: string; token: string }) {
  try {
    window.localStorage.setItem(collisionStorageKey, JSON.stringify(message));
    return true;
  } catch {
    return false;
  }
}

function installStorageCollisionListener() {
  if (storageCollisionListenerInstalled) return;
  storageCollisionListenerInstalled = true;
  window.addEventListener("storage", (event: StorageEvent) => {
    if (event.key !== collisionStorageKey || !event.newValue) return;
    let message: { type?: string; id?: string; token?: string };
    try {
      message = JSON.parse(event.newValue) as typeof message;
    } catch {
      return;
    }
    if (!message.id || !message.token || message.id !== activeClientInstanceId) return;
    if (message.type === "probe") {
      sendStorageCollisionMessage({ type: "claimed", id: message.id, token: message.token });
    } else if (message.type === "claimed" && pendingStorageProbes.has(message.token)) {
      collidedStorageProbes.add(message.token);
    }
  });
}

async function hasStorageCollision(value: string) {
  installStorageCollisionListener();
  const token = createId();
  pendingStorageProbes.add(token);
  if (!sendStorageCollisionMessage({ type: "probe", id: value, token })) {
    pendingStorageProbes.delete(token);
    return false;
  }
  await new Promise((resolve) => window.setTimeout(resolve, 100));
  pendingStorageProbes.delete(token);
  return collidedStorageProbes.delete(token);
}

export async function getBrowserClientInstanceId() {
  if (clientInstanceId) return clientInstanceId;
  clientInstanceId = (async () => {
    if (typeof window === "undefined") return createId();
    let value = readStoredId() ?? createId();
    storeId(value);
    activeClientInstanceId = value;
    installStorageCollisionListener();
    let collision = false;
    let channel: BroadcastChannel | null = null;
    try {
      if (typeof BroadcastChannel === "undefined") throw new Error("BroadcastChannel unavailable");
      collisionChannel?.close();
      const nextChannel = new BroadcastChannel(channelName);
      channel = nextChannel;
      collisionChannel = nextChannel;
      nextChannel.onmessage = (event: MessageEvent<{ type?: string; id?: string }>) => {
        if (event.data?.type === "probe" && event.data.id === activeClientInstanceId) nextChannel.postMessage({ type: "claimed", id: event.data.id });
        if (event.data?.type === "claimed" && event.data.id === value) collision = true;
      };
      nextChannel.postMessage({ type: "probe", id: value });
      await new Promise((resolve) => window.setTimeout(resolve, 50));
    } catch {
      channel?.close();
      if (collisionChannel === channel) collisionChannel = null;
      collision = await hasStorageCollision(value);
    }
    if (collision) {
      value = createId();
      storeId(value);
      activeClientInstanceId = value;
    }
    return value;
  })();
  return clientInstanceId;
}
