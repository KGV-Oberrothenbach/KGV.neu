import { Permission, hasPermission } from "./permissions";
import type { WebCapabilityContext } from "./capabilities";
export const canUseSerialLetters=(context:WebCapabilityContext)=>context !== null && hasPermission(context.effectivePermissions, Permission.CanCreateSerialLetters);
export const serienbriefAccessDenied="Für diese Funktion ist das Fachrecht Serienbriefe erstellen erforderlich.";
