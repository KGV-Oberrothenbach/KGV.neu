"use client";

export function LeaseContractPreview({ previewUrl, onBack, onContinue, onCancel }: { previewUrl: string; onBack: () => void; onContinue: () => void; onCancel: () => void }) {
  return <section className="contract-preview">
    <h4>PDF-Vorschau</h4>
    <p className="detail-hint">Bitte das vollständige Dokument prüfen, bevor die Unterschriften erfasst werden.</p>
    <iframe title="PDF-Vorschau Pachtvertrag" src={previewUrl} className="contract-preview-frame" />
    <div className="editor-actions"><button type="button" className="secondary-action" onClick={onBack}>Zurück zur Bearbeitung</button><button type="button" className="secondary-action" onClick={onCancel}>Abbrechen</button><button type="button" onClick={onContinue}>Weiter zu Unterschriften</button></div>
  </section>;
}
