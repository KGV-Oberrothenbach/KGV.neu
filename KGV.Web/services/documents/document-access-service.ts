import type { DocumentOwner } from "../../models/documents/document";

export type DocumentAccessContext = {
  role: "admin" | "vorstand" | "user";
  memberId: number | null;
  canReadDocuments: boolean;
  canManageDocuments: boolean;
  canSeeOwnDataOnly: boolean;
};

export type DocumentCapabilities = {
  canRead: boolean;
  canUpload: boolean;
  canArchive: boolean;
};

export function documentCapabilitiesForOwner(context: DocumentAccessContext, owner: DocumentOwner): DocumentCapabilities {
  const canReadOwnMemberDocuments = owner.kind === "member"
    && context.canSeeOwnDataOnly
    && context.memberId === owner.id;

  return {
    // Like the existing Core PermissionChecks, document management includes reading.
    canRead: context.canReadDocuments || context.canManageDocuments || canReadOwnMemberDocuments,
    canUpload: context.canManageDocuments,
    canArchive: context.role === "admin",
  };
}
