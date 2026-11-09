import { Permission, hasPermission } from "./permissions";
import type { WebCapabilityContext } from "./capabilities";
export const canUseSerialLetters=(context:WebCapabilityContext)=>context !== null && hasPermission(context.effectivePermissions, Permission.CanManageCommunicationWorkflows);
export const serienbriefAccessDenied="Für diese Funktion ist eine Berechtigung für Kommunikationsvorgänge erforderlich.";
