const inactivityMs = 15 * 60 * 1000;

export function startInactivityMonitor({ vereinId, userId, onTimeout }: { vereinId?: string; userId: string; onTimeout: () => void }) {
  const activityKey = `kgv.browser.lastActivity.v1.${vereinId ?? "unknown"}.${userId}`;
  let lastActivity = Date.now();
  let lastStored = 0;
  let timer = 0;

  const schedule = () => {
    window.clearTimeout(timer);
    const remaining = Math.max(0, inactivityMs - (Date.now() - lastActivity));
    timer = window.setTimeout(onTimeout, remaining);
  };
  const activity = () => {
    const now = Date.now();
    lastActivity = now;
    if (now - lastStored >= 1000) {
      lastStored = now;
      window.localStorage.setItem(activityKey, String(now));
    }
    schedule();
  };
  const sharedActivity = (event: StorageEvent) => {
    if (event.key !== activityKey || !event.newValue) return;
    const value = Number(event.newValue);
    if (Number.isFinite(value) && value > lastActivity) { lastActivity = value; schedule(); }
  };
  const events: Array<keyof WindowEventMap> = ["pointerdown", "pointermove", "keydown", "touchstart", "scroll"];
  events.forEach((name) => window.addEventListener(name, activity, { passive: true }));
  window.addEventListener("storage", sharedActivity);
  activity();
  return () => {
    window.clearTimeout(timer);
    events.forEach((name) => window.removeEventListener(name, activity));
    window.removeEventListener("storage", sharedActivity);
  };
}
