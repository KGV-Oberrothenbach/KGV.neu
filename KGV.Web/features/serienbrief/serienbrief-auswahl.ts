import type { SerienbriefExportDatensatz } from "../../models/serienbrief/serienbrief-export-context";

const canonical=(wert:unknown):string=>{if(wert===null||typeof wert!=="object")return JSON.stringify(wert);if(Array.isArray(wert))return `[${wert.map(canonical).join(",")}]`;const record=wert as Record<string,unknown>;return `{${Object.keys(record).sort().map(key=>`${JSON.stringify(key)}:${canonical(record[key])}`).join(",")}}`;};
export const serienbriefDatensatzSchluessel=(datensatz:SerienbriefExportDatensatz)=>canonical({mitgliedId:datensatz.mitgliedId,parzelleId:datensatz.parzelleId,empfaengerStatus:datensatz.empfaengerStatus,werte:datensatz.werte});
export const serienbriefDatensatzGueltig=(datensatz:SerienbriefExportDatensatz)=>datensatz.mitgliedId!==null;
export const initialeSerienbriefAuswahl=(datensaetze:SerienbriefExportDatensatz[])=>new Set(datensaetze.filter(serienbriefDatensatzGueltig).map(serienbriefDatensatzSchluessel));
export const ausgewaehlteSerienbriefDatensaetze=(datensaetze:SerienbriefExportDatensatz[],auswahl:ReadonlySet<string>)=>datensaetze.filter(datensatz=>serienbriefDatensatzGueltig(datensatz)&&auswahl.has(serienbriefDatensatzSchluessel(datensatz)));
export const serienbriefAuswahlZaehler=(datensaetze:SerienbriefExportDatensatz[],auswahl:ReadonlySet<string>)=>{const gueltig=datensaetze.filter(serienbriefDatensatzGueltig);return {gesamt:datensaetze.length,gueltig:gueltig.length,ausgewaehlt:ausgewaehlteSerienbriefDatensaetze(datensaetze,auswahl).length,konflikte:datensaetze.length-gueltig.length};};
