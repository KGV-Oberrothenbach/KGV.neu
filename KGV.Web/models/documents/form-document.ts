export type FormDocumentType = "mitgliedsantrag" | "mitgliedsvertrag" | "pachtvertrag";
export type FormDocumentMetadataStatus = "unsigniert" | "signiert";

export type FormDocumentMetadata = {
  type: FormDocumentType;
  status: FormDocumentMetadataStatus;
};
