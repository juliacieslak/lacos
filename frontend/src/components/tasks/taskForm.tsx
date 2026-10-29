// formulário para criar ou editar uma tarefa da rotina de cuidado: título, categoria, horário e recorrência.
import { useState, type FormEvent } from "react";
import DaysOfWeekPicker from "../ui/daysOfWeekPicker";
import ErrorMessage from "../ui/errorMessage";
import FormActions from "../ui/formActions";
import Input, { SelectField, Textarea } from "../ui/input";
import { useAction } from "../../hooks/useAsync";
import type { CareCategory, CareTask, CareTaskRequest } from "../../types/careTask";
import { careCategoryLabels, toOptions } from "../../utils/labels";

export default function TaskForm({ task, onSubmit, onCancel }: { task?: CareTask; onSubmit: (d: CareTaskRequest) => Promise<unknown>; onCancel: () => void }) {
  const [form, setForm] = useState({
    title: task?.title ?? "", description: task?.description ?? "", category: task?.category ?? ("CARE_PROVIDED" as CareCategory),
    scheduledTime: task?.scheduledTime.slice(0, 5) ?? "08:00", daysOfWeek: task?.daysOfWeek ?? [], startDate: task?.startDate ?? "", endDate: task?.endDate ?? "",
  });
  const save = useAction(onSubmit);
  const err = (k: string) => save.error?.fieldErrors[k]?.[0];

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    void save.run({
      title: form.title, description: form.description || null, category: form.category, scheduledTime: `${form.scheduledTime}:00`,
      daysOfWeek: form.daysOfWeek, startDate: form.startDate || null, endDate: form.endDate || null,
    });
  };

  return (
    <form onSubmit={handleSubmit} noValidate>
      <div className="grid gap-4 sm:grid-cols-2">
        <Input label="Tarefa" required value={form.title} maxLength={150} placeholder="Ex.: Almoço e hidratação" onChange={(e) => setForm({ ...form, title: e.target.value })}
          error={err("title")} containerClassName="sm:col-span-2" />
        <SelectField label="Categoria" options={toOptions(careCategoryLabels)} value={form.category} onChange={(e) => setForm({ ...form, category: e.target.value as CareCategory })} />
        <Input label="Horário" type="time" required value={form.scheduledTime} onChange={(e) => setForm({ ...form, scheduledTime: e.target.value })} error={err("scheduledTime")} />
        <Input label="Início" type="date" value={form.startDate} hint="Padrão: hoje" onChange={(e) => setForm({ ...form, startDate: e.target.value })} />
        <Input label="Fim" type="date" value={form.endDate} hint="Opcional" onChange={(e) => setForm({ ...form, endDate: e.target.value })} error={err("endDate")} />
        <div className="sm:col-span-2"><DaysOfWeekPicker value={form.daysOfWeek} onChange={(d) => setForm({ ...form, daysOfWeek: d })} /></div>
        <Textarea label="Instruções" value={form.description} maxLength={1000} onChange={(e) => setForm({ ...form, description: e.target.value })} containerClassName="sm:col-span-2" />
      </div>
      <ErrorMessage error={save.error && !Object.keys(save.error.fieldErrors).length ? save.error : null} className="mt-4" />
      <FormActions onCancel={onCancel} loading={save.pending} />
    </form>
  );
}
