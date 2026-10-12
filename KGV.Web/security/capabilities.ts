import { getEffectivePermissions, hasPermission, Permission, type AppUserRole } from "./permissions";

export type WebCapabilityContext = {
  role: AppUserRole;
  effectivePermissions: number;
} | null;

export function createWebCapabilityContext(role: AppUserRole, grants = 0, revocations = 0): WebCapabilityContext {
  return { role, effectivePermissions: getEffectivePermissions(role, grants, revocations) };
}

export function canReadDocuments(context: WebCapabilityContext): boolean {
  return context !== null && (hasPermission(context.effectivePermissions, Permission.CanReadDocuments)
    || hasPermission(context.effectivePermissions, Permission.CanManageDocuments));
}

export function canManageDocuments(context: WebCapabilityContext): boolean {
  return context !== null && hasPermission(context.effectivePermissions, Permission.CanManageDocuments);
}

export function canReadRoleManagement(context: WebCapabilityContext): boolean {
  return context !== null && (hasPermission(context.effectivePermissions, Permission.CanReadRoles)
    || hasPermission(context.effectivePermissions, Permission.CanManageRoles));
}

export function canManageRoleManagement(context: WebCapabilityContext): boolean {
  return context !== null && hasPermission(context.effectivePermissions, Permission.CanManageRoles);
}

export function canPerformYearEndClosing(context: WebCapabilityContext): boolean {
  return context !== null && hasPermission(context.effectivePermissions, Permission.CanPerformYearEndClosing);
}

export function canArchiveDocuments(context: WebCapabilityContext): boolean {
  return context?.role === "admin";
}

export function canManageClubSeasonConfiguration(context: WebCapabilityContext): boolean {
  return context?.role === "admin";
}

export function canConfigureUserMeterReadingSubmissions(context: WebCapabilityContext): boolean {
  return context?.role === "admin" || context?.role === "vorstand";
}
