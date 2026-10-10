"use client";
import { useMemo } from "react";
import { sanitizeAnnouncementHtml } from "./announcement-html";
export function SafeAnnouncementHtml({ html }: { html: string | null | undefined }) { const safe = useMemo(() => sanitizeAnnouncementHtml(html), [html]); return safe ? <div className="announcement-html" dangerouslySetInnerHTML={{ __html: safe }} /> : <p>Kein Inhalt hinterlegt.</p>; }
