const storageKey = "kgv.browser.client-instance.v1";
const channelName = "kgv-browser-client-instance-v1";
let clientInstanceId: Promise<string> | null = null;
let collisionChannel: BroadcastChannel | null = null;

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

export async function getBrowserClientInstanceId() {
  if (clientInstanceId) return clientInstanceId;
  clientInstanceId = (async () => {
    if (typeof window === "undefined") return createId();
    let value = readStoredId() ?? createId();
    storeId(value);
    if (typeof BroadcastChannel === "undefined") return value;

  collisionChannel?.close();
  const channel = new BroadcastChannel(channelName);
  collisionChannel = channel;
    let collision = false;
    channel.onmessage = (event: MessageEvent<{ type?: string; id?: string }>) => {
      if (event.data?.type === "probe" && event.data.id === value) channel.postMessage({ type: "claimed", id: value });
      if (event.data?.type === "claimed" && event.data.id === value) collision = true;
    };
    channel.postMessage({ type: "probe", id: value });
    await new Promise((resolve) => window.setTimeout(resolve, 50));
    if (collision) {
      value = createId();
      storeId(value);
    }
    return value;
  })();
  return clientInstanceId;
}
