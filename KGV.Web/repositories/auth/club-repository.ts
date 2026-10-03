type ClubResolutionRecord = {
  verein_id?: string;
  vereins_code?: string;
  vereinsname?: string;
  kurzname?: string;
  supabase_url?: string;
  supabase_publishable_key?: string;
};

export async function resolveClubRecord(code: string): Promise<ClubResolutionRecord[]> {
  const url = process.env.NEXT_PUBLIC_VEREINSREGISTER_URL?.trim().replace(/\/$/, "");
  const key = process.env.NEXT_PUBLIC_VEREINSREGISTER_PUBLISHABLE_KEY?.trim();
  if (!url || !key) throw new Error("Das Vereinsregister ist in dieser Browser-App nicht konfiguriert.");

  const response = await fetch(`${url}/rest/v1/rpc/resolve_vereinscode`, {
    method: "POST",
    headers: { apikey: key, Authorization: `Bearer ${key}`, "Content-Type": "application/json" },
    body: JSON.stringify({ p_code: code }),
  });
  if (!response.ok) throw new Error("Die Vereins-ID konnte nicht geprüft werden.");
  return await response.json() as ClubResolutionRecord[];
}
