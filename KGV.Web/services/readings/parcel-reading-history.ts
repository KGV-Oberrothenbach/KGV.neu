export type ParcelReadingHistoryAssignment = {
  parzelle_id: number;
  von_datum: string | null;
  bis_datum: string | null;
};

export type ParcelReadingHistoryMeter = {
  id: number;
  parzelle_id: number;
  medium: string;
  zaehlernummer: string;
};

export type ParcelReadingHistoryReading = {
  id?: number;
  zaehler_id: number;
  ablesedatum: string;
};

export type ParcelReadingHistoryEntry<TReading extends ParcelReadingHistoryReading> = {
  meter: ParcelReadingHistoryMeter;
  reading: TReading;
};

export function filterParcelAssignmentReadings<TReading extends ParcelReadingHistoryReading>(
  assignment: ParcelReadingHistoryAssignment,
  meters: ParcelReadingHistoryMeter[],
  readings: TReading[],
): ParcelReadingHistoryEntry<TReading>[] {
  const metersById = new Map(
    meters
      .filter((meter) => meter.parzelle_id === assignment.parzelle_id)
      .map((meter) => [meter.id, meter]),
  );

  return readings
    .flatMap((reading) => {
      const meter = metersById.get(reading.zaehler_id);
      const date = reading.ablesedatum.slice(0, 10);
      if (!meter || (assignment.von_datum && date < assignment.von_datum) || (assignment.bis_datum && date > assignment.bis_datum)) return [];
      return [{ meter, reading }];
    })
    .sort((left, right) =>
      right.reading.ablesedatum.localeCompare(left.reading.ablesedatum)
      || (right.reading.id ?? 0) - (left.reading.id ?? 0),
    );
}
