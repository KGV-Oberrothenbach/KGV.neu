"use client";
import { useEffect, useState } from "react";
import { sanitizeAnnouncementHtml } from "./announcement-html";
export function SafeAnnouncementHtml({ html }: { html: string | null | undefined }) { const [safe, setSafe] = useState(""); useEffect(() => setSafe(sanitizeAnnouncementHtml(html)), [html]); return safe ? <div className="announcement-html" dangerouslySetInnerHTML={{ __html: safe }} /> : <p>Kein Inhalt hinterlegt.</p>; }
