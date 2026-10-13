import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";

const globalNames = ["BarcodeDetector", "NDEFReader", "window", "navigator"];
const originals = new Map(globalNames.map((name) => [name, Object.getOwnPropertyDescriptor(globalThis, name)]));

function setGlobal(name, value) {
  Object.defineProperty(globalThis, name, { configurable: true, writable: true, value });
}

function restoreGlobals() {
  for (const name of globalNames) {
    const descriptor = originals.get(name);
    if (descriptor) Object.defineProperty(globalThis, name, descriptor);
    else delete globalThis[name];
  }
}

try {
  setGlobal("window", { isSecureContext: false });
  setGlobal("navigator", {});
  delete globalThis.BarcodeDetector;
  delete globalThis.NDEFReader;

  const capabilities = await import("../lib/browser/media-capabilities.ts");
  assert.equal(capabilities.supportsBarcodeDetection(), false);
  assert.equal(capabilities.supportsCameraCapture(), false);
  assert.equal(capabilities.supportsWebNfc(), false);

  class Detector { async detect() { return []; } }
  class Reader extends EventTarget { async scan() {} }
  setGlobal("BarcodeDetector", Detector);
  setGlobal("NDEFReader", Reader);
  setGlobal("navigator", { mediaDevices: { async getUserMedia() { return new MediaStream(); } } });
  setGlobal("window", { isSecureContext: true });

  assert.equal(capabilities.supportsBarcodeDetection(), true);
  assert.equal(capabilities.supportsCameraCapture(), true);
  assert.equal(capabilities.supportsWebNfc(), true);

  const [nfcScanner, rfidScanner, page, qrScanner] = await Promise.all([
    readFile(new URL("../components/scanner/NfcScanner.tsx", import.meta.url), "utf8"),
    readFile(new URL("../features/meters/RfidScanner.tsx", import.meta.url), "utf8"),
    readFile(new URL("../app/page.tsx", import.meta.url), "utf8"),
    readFile(new URL("../components/scanner/QrScanner.tsx", import.meta.url), "utf8"),
  ]);
  assert.doesNotMatch(nfcScanner, /supabase-auth|resolveRfidScanContext|RfidScanResolution/);
  assert.match(rfidScanner, /resolveRfidScanContext/);
  assert.doesNotMatch(page, /ndefReaderConstructor|NDEFReader|serialNumber/);
  assert.match(qrScanner, /getTracks\(\)\.forEach\(\(track\) => track\.stop\(\)\)/);
  assert.match(nfcScanner, /AbortController/);
  console.log("browser scanner capability and architecture contract: ok");
} finally {
  restoreGlobals();
}
