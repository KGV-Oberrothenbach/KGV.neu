import type { AnnualConsumptionSegment } from "../../repositories/readings/annual-consumption-repository";

export type JaeExportInput = { year: number; segments: AnnualConsumptionSegment[]; parcelAreas: Map<number, number | null> };
export const jaeExtendedHeaders = ["Garten", "Segment", "Zeitraum von", "Zeitraum bis", "Pächter", "Leerstand", "Vereinsgarten", "Medium", "Zähler", "Startstand", "Endstand", "Verbrauch", "Segmentgrund", "Berechenbar", "Fehlercode", "Fehlertext", "Umlagefähig", "Gartengröße"];
