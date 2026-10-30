export const serienbriefPlaceholder=(schluessel:string)=>`{{${schluessel}}}`;

export function insertSerienbriefPlaceholder(value:string,start:number,end:number,schluessel:string){
  const placeholder=serienbriefPlaceholder(schluessel);
  return `${value.slice(0,start)}${placeholder}${value.slice(end)}`;
}

export function normalizeSerienbriefPlaceholderHtml(html:string){
  return html.replace(/\{\{[\s\S]*?\}\}/g,placeholder=>placeholder.replace(/<[^>]*>/g,""));
}
