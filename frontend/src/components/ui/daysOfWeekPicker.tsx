// seleção de dias da semana (vazio = todos os dias).
import type { DayOfWeek } from "../../types/common";
import { allDays, dayLabels } from "../../utils/labels";

export default function DaysOfWeekPicker({ value, onChange }: { value: DayOfWeek[]; onChange: (days: DayOfWeek[]) => void }) {
  const toggle = (d: DayOfWeek) => onChange(value.includes(d) ? value.filter((x) => x !== d) : [...value, d]);
  return (
    <fieldset>
      <legend className="mb-2 block text-xs font-semibold uppercase tracking-[0.06em] text-mist-500">Dias da semana</legend>
      <div className="flex flex-wrap gap-1.5">
        {allDays.map((d) => (
          <button key={d} type="button" onClick={() => toggle(d)} aria-pressed={value.includes(d)}
            className={`rounded-lg px-3 py-1.5 text-xs font-semibold transition ${value.includes(d) ? "bg-lavender-600 text-white" : "bg-mist-100 text-mist-500 hover:bg-lavender-50"}`}>
            {dayLabels[d]}
          </button>
        ))}
      </div>
      <p className="mt-1.5 text-xs text-mist-500">{value.length === 0 ? "Todos os dias." : "Somente nos dias selecionados."}</p>
    </fieldset>
  );
}
