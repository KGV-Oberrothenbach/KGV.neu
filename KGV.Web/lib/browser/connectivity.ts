type NetworkInformationLike = EventTarget & {
  type?: string;
};

export type ConnectivitySnapshot = {
  online: boolean;
  networkType: string | null;
  wifi: boolean | null;
};

function networkInformation() {
  if (typeof window === "undefined" || typeof navigator === "undefined") return null;

  const browserNavigator = navigator as Navigator & {
    connection?: NetworkInformationLike;
    mozConnection?: NetworkInformationLike;
    webkitConnection?: NetworkInformationLike;
  };
  return browserNavigator.connection ?? browserNavigator.mozConnection ?? browserNavigator.webkitConnection ?? null;
}

export function getConnectivitySnapshot(): ConnectivitySnapshot {
  if (typeof window === "undefined" || typeof navigator === "undefined") return { online: false, networkType: null, wifi: null };

  const networkType = networkInformation()?.type ?? null;
  return {
    online: navigator.onLine,
    networkType,
    wifi: networkType === "wifi" ? true : networkType ? false : null,
  };
}

export function subscribeConnectivityChanges(callback: (snapshot: ConnectivitySnapshot) => void) {
  if (typeof window === "undefined") return () => {};

  const notify = () => callback(getConnectivitySnapshot());
  const connection = networkInformation();
  window.addEventListener("online", notify);
  window.addEventListener("offline", notify);
  connection?.addEventListener("change", notify);

  return () => {
    window.removeEventListener("online", notify);
    window.removeEventListener("offline", notify);
    connection?.removeEventListener("change", notify);
  };
}
