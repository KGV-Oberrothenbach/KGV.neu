import type { WebCapabilityContext } from "./capabilities";
export const canUseSerialLetters=(context:WebCapabilityContext)=>context?.role==="admin"||context?.role==="vorstand";
export const serienbriefAccessDenied="Für diese Funktion ist eine Berechtigung als Admin oder Vorstand erforderlich.";
