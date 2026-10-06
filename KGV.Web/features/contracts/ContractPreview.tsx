"use client";
export function ContractPreview({ onBack, onContinue }: { onBack: () => void; onContinue: () => void }) { return <div className="editor-actions"><button type="button" className="secondary-action" onClick={onBack}>Zurück zur Bearbeitung</button><button type="button" onClick={onContinue}>Weiter zu Unterschriften</button></div>; }
