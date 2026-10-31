import type { SerienbriefEditorState } from "../../features/serienbrief/SerienbriefEditor";
export const vorlagenPlatzhalter=(state:SerienbriefEditorState)=>[...`${state.betreff}${state.htmlInhalt}`.matchAll(/\{\{([a-z][a-z0-9_]*)\}\}/g)].map(x=>x[1]);
export const validateVorlage=(state:SerienbriefEditorState,keys:string[])=>{const used=[...new Set(vorlagenPlatzhalter(state))],free=new Set(state.freieFelder.map(x=>x.schluessel));return {exportfelder:used.filter(x=>keys.includes(x)),unknown:used.filter(x=>!keys.includes(x)&&!free.has(x))};};
export const kompatibilitaet=(required:string[],current:string[])=>required.filter(x=>!current.includes(x));
