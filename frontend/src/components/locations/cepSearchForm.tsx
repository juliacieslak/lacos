// formulário que recebe o CEP (com máscara e validação) e o raio da busca de farmácias próximas.
import { Search } from "lucide-react";
import { useState, type FormEvent } from "react";
import Button from "../ui/button";
import { formatCep, isValidCep } from "../../utils/formatCep";

const RADIUS_OPTIONS = [1000, 3000, 5000, 10000];

interface Props {
  loading: boolean;
  initialCep?: string;
  onSearch: (cep: string, radius: number) => void;
}

export default function CepSearchForm({ loading, initialCep = "", onSearch }: Props) {
  const [cep, setCep] = useState(formatCep(initialCep));
  const [radius, setRadius] = useState(3000);
  const [touched, setTouched] = useState(false);
  const invalid = touched && !isValidCep(cep);

  const submit = (e: FormEvent) => {
    e.preventDefault();
    setTouched(true);
    if (isValidCep(cep)) onSearch(cep, radius);
  };

  return (
    <form onSubmit={submit} className="flex flex-col gap-3 sm:flex-row sm:items-start" noValidate>
      <div className="flex-1">
        <label htmlFor="cep" className="sr-only">CEP</label>
        <input id="cep" value={cep} inputMode="numeric" autoComplete="postal-code" placeholder="Digite o CEP (ex.: 80010-000)" maxLength={9}
          onChange={(e) => setCep(formatCep(e.target.value))} onBlur={() => setTouched(true)} aria-invalid={invalid} aria-describedby={invalid ? "cep-error" : undefined}
          className={`w-full rounded-xl border bg-white px-4 py-3 text-base outline-none transition focus:border-lavender-500 focus:ring-2 focus:ring-lavender-300/50 ${invalid ? "border-danger-600" : "border-mist-200"}`} />
        {invalid && <p id="cep-error" className="mt-1.5 text-xs text-danger-600">CEP inválido. Informe os 8 dígitos.</p>}
      </div>
      <select aria-label="Raio da busca" value={radius} onChange={(e) => setRadius(Number(e.target.value))}
        className="rounded-xl border border-mist-200 bg-white px-3 py-3 text-sm outline-none focus:border-lavender-500">
        {RADIUS_OPTIONS.map((r) => <option key={r} value={r}>Até {r / 1000} km</option>)}
      </select>
      <Button type="submit" size="lg" loading={loading} icon={<Search className="h-4 w-4" />}>Buscar farmácias</Button>
    </form>
  );
}
