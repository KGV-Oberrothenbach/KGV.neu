const wifiOnlyPreferenceKey = "kgv-meter-photo-wifi-only";

export function readBooleanStorage(key: string, defaultValue = false) {
  if (typeof window === "undefined") return defaultValue;

  try {
    const value = window.localStorage.getItem(key);
    return value === null ? defaultValue : value === "true" ? true : value === "false" ? false : defaultValue;
  } catch {
    return defaultValue;
  }
}

export function writeBooleanStorage(key: string, value: boolean) {
  if (typeof window === "undefined") return;

  try {
    window.localStorage.setItem(key, String(value));
  } catch {
    // Browser storage can be unavailable, for example in private browsing modes.
  }
}

export function getWifiOnlyPreference() {
  return readBooleanStorage(wifiOnlyPreferenceKey);
}

export function setWifiOnlyPreference(value: boolean) {
  writeBooleanStorage(wifiOnlyPreferenceKey, value);
}
