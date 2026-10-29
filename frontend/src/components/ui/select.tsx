// seletor compacto (filtros e trocas rápidas, ex.: paciente selecionado, status).
import type { SelectHTMLAttributes } from "react";
import type { SelectOption } from "./input";

interface SelectProps extends SelectHTMLAttributes<HTMLSelectElement> {
  options: SelectOption[];
  ariaLabel: string;
}

export default function Select({ options, ariaLabel, className = "", ...props }: SelectProps) {
  return (
    <select
      aria-label={ariaLabel}
      className={`rounded-xl border border-mist-200 bg-white px-3 py-2.5 text-sm text-ink outline-none transition focus:border-lavender-500 focus:ring-2 focus:ring-lavender-300/50 ${className}`}
      {...props}
    >
      {options.map((o) => (
        <option key={o.value} value={o.value}>{o.label}</option>
      ))}
    </select>
  );
}
