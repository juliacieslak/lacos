// formulário para cadastrar ou editar medicamento (RF08): nome, dosagem, marca, motivo, via, prescritor,
// início/fim, frequência e horários de administração.
import { Plus, X } from "lucide-react";
import { useState, type FormEvent } from "react";
import DaysOfWeekPicker from "../ui/daysOfWeekPicker";
import ErrorMessage from "../ui/errorMessage";
import FormActions from "../ui/formActions";
import Input, { SelectField } from "../ui/input";
import { useAction } from "../../hooks/useAsync";
import type { AdministrationRoute, Medication, MedicationRequest } from "../../types/medication";
import { todayIso } from "../../utils/formatDate";
import { routeLabels, toOptions } from "../../utils/labels";

interface Props {
  medication?: Medication;
  onSubmit: (data: MedicationRequest) => Promise<unknown>;
  onCancel: () => void;
}

export default function MedicationForm({ medication, onSubmit, onCancel }: Props) {
  const [form, setForm] = useState<MedicationRequest>({
    name: medication?.name ?? "", dosage: medication?.dosage ?? "", brand: medication?.brand ?? "", usageReason: medication?.usageReason ?? "",
    administrationRoute: medication?.administrationRoute ?? "ORAL", prescribedBy: medication?.prescribedBy ?? "",
    startDate: medication?.startDate ?? todayIso(), endDate: medication?.endDate ?? null, frequency: medication?.frequency ?? "",
    administrationTimes: medication?.administrationTimes.map((t) => t.slice(0, 5)) ?? ["08:00"], daysOfWeek: medication?.daysOfWeek ?? [],
  });
  const [newTime, setNewTime] = useState("");
  const save = useAction(onSubmit);
  const set = <K extends keyof MedicationRequest>(k: K, v: MedicationRequest[K]) => setForm((f) => ({ ...f, [k]: v }));
  const err = (k: string) => save.error?.fieldErrors[k]?.[0];

  const addTime = () => {
    if (newTime && !form.administrationTimes.includes(newTime)) set("administrationTimes", [...form.administrationTimes, newTime].sort());
    setNewTime("");
  };

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    void save.run({
      ...form,
      brand: form.brand || null, usageReason: form.usageReason || null, prescribedBy: form.prescribedBy || null, endDate: form.endDate || null,
      // o backend espera TimeOnly "HH:mm:ss".
      administrationTimes: form.administrationTimes.map((t) => (t.length === 5 ? `${t}:00` : t)),
    });
  };

  return (
    <form onSubmit={handleSubmit} noValidate>
      <div className="grid gap-4 sm:grid-cols-2">
        <Input label="Nome" required value={form.name} maxLength={150} onChange={(e) => set("name", e.target.value)} error={err("name")} />
        <Input label="Dosagem" required value={form.dosage} maxLength={100} placeholder="Ex.: 500 mg, 1 comprimido" onChange={(e) => set("dosage", e.target.value)} error={err("dosage")} />
        <Input label="Marca" value={form.brand ?? ""} maxLength={100} onChange={(e) => set("brand", e.target.value)} />
        <SelectField label="Via de administração" required options={toOptions(routeLabels)} value={form.administrationRoute}
          onChange={(e) => set("administrationRoute", e.target.value as AdministrationRoute)} />
        <Input label="Motivo do uso" value={form.usageReason ?? ""} maxLength={500} onChange={(e) => set("usageReason", e.target.value)} />
        <Input label="Profissional que prescreveu" value={form.prescribedBy ?? ""} maxLength={150} onChange={(e) => set("prescribedBy", e.target.value)} />
        <Input label="Início" type="date" required value={form.startDate} onChange={(e) => set("startDate", e.target.value)} error={err("startDate")} />
        <Input label="Fim (se houver)" type="date" value={form.endDate ?? ""} onChange={(e) => set("endDate", e.target.value || null)} error={err("endDate")} />
        <Input label="Frequência" required value={form.frequency} maxLength={100} placeholder="Ex.: 8 em 8 horas" onChange={(e) => set("frequency", e.target.value)}
          error={err("frequency")} containerClassName="sm:col-span-2" />
        <div className="sm:col-span-2">
          <p className="mb-2 text-xs font-semibold uppercase tracking-[0.06em] text-mist-500">Horários de administração</p>
          <div className="flex flex-wrap items-center gap-2">
            {form.administrationTimes.map((t) => (
              <span key={t} className="inline-flex items-center gap-1 rounded-lg bg-lavender-100 px-2.5 py-1 text-sm font-semibold text-lavender-700">
                {t.slice(0, 5)}
                <button type="button" aria-label={`Remover ${t.slice(0, 5)}`} onClick={() => set("administrationTimes", form.administrationTimes.filter((x) => x !== t))}>
                  <X className="h-3.5 w-3.5" />
                </button>
              </span>
            ))}
            <input type="time" aria-label="Novo horário" value={newTime} onChange={(e) => setNewTime(e.target.value)}
              className="rounded-lg border border-mist-200 bg-mist-100 px-2 py-1 text-sm outline-none focus:border-lavender-500" />
            <button type="button" onClick={addTime} className="inline-flex items-center gap-1 rounded-lg px-2 py-1 text-sm font-semibold text-lavender-600 hover:bg-lavender-50">
              <Plus className="h-3.5 w-3.5" /> Adicionar
            </button>
          </div>
          <p className="mt-1.5 text-xs text-mist-500">Sem horários = uso "se necessário" (sem lembretes automáticos).</p>
        </div>
        <div className="sm:col-span-2"><DaysOfWeekPicker value={form.daysOfWeek} onChange={(d) => set("daysOfWeek", d)} /></div>
      </div>
      <ErrorMessage error={save.error && !Object.keys(save.error.fieldErrors).length ? save.error : null} className="mt-4" />
      <FormActions onCancel={onCancel} loading={save.pending} />
    </form>
  );
}
