export type BarcodeDetectorLike = { detect(source: ImageBitmapSource): Promise<Array<{ rawValue?: string }>> };
export type BarcodeDetectorConstructor = new (options?: { formats?: string[] }) => BarcodeDetectorLike;

export type NdefReaderLike = EventTarget & {
  scan(options?: { signal?: AbortSignal }): Promise<void>;
  onreading: ((event: Event & { serialNumber?: string }) => void) | null;
  onreadingerror: ((event: Event) => void) | null;
};
export type NdefReaderConstructor = new () => NdefReaderLike;

export function barcodeDetectorConstructor() {
  return (globalThis as typeof globalThis & { BarcodeDetector?: BarcodeDetectorConstructor }).BarcodeDetector ?? null;
}

export function ndefReaderConstructor() {
  return (globalThis as typeof globalThis & { NDEFReader?: NdefReaderConstructor }).NDEFReader ?? null;
}

export function supportsBarcodeDetection() {
  return Boolean(barcodeDetectorConstructor());
}

export function supportsCameraCapture() {
  return typeof navigator !== "undefined" && Boolean(navigator.mediaDevices?.getUserMedia);
}

export function supportsWebNfc() {
  return typeof window !== "undefined" && window.isSecureContext && Boolean(ndefReaderConstructor());
}
