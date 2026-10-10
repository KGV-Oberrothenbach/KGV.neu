import DOMPurify from "dompurify";

const config = { ALLOWED_TAGS: ["p","br","strong","b","em","i","u","s","h1","h2","h3","h4","ul","ol","li","blockquote","a","table","thead","tbody","tr","th","td","hr","code","pre"], ALLOWED_ATTR: ["href","title","colspan","rowspan"], ALLOWED_URI_REGEXP: /^(?:(?:https?|mailto|tel):|#)/i, ALLOW_DATA_ATTR: false, ALLOW_ARIA_ATTR: false };
export function sanitizeAnnouncementHtml(html: string | null | undefined) { return typeof window === "undefined" ? "" : DOMPurify.sanitize(html ?? "", config); }
