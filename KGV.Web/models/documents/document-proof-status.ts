export type DocumentProofStatusCode = "unterschrieben" | "nur_unsigniert" | "kein_nachweis";

export type DocumentProofStatus = {
  status: DocumentProofStatusCode;
  relevantesDatum: string | null;
  dokumentId: number | null;
};
