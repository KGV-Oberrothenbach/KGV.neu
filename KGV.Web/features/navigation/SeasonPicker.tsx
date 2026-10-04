"use client";

export type SeasonOption = { id: number; jahr: number };

export default function SeasonPicker({ seasons, selectedSeasonId, seasonError, onChange }: { seasons: SeasonOption[]; selectedSeasonId: number | null | undefined; seasonError: string | null | undefined; onChange: (saisonId: number) => void }) {
  return (
    <label className="season-picker" htmlFor="season">
      <span>Saison</span>
      <select id="season" value={selectedSeasonId ?? ""} onChange={(event) => onChange(Number(event.target.value))} disabled={seasons.length === 0}>
        <option value="">{seasonError || "Saison wählen"}</option>
        {seasons.map((item) => <option key={item.id} value={item.id}>{item.jahr}</option>)}
      </select>
    </label>
  );
}
