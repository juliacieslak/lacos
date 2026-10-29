// formulário declarativo simples para os registros clínicos (campos texto, data, data/hora e seleção).
import { useState, type FormEvent } from "react";
import ErrorMessage from "../ui/errorMessage";
import FormActions from "../ui/formActions";
import Input, { SelectField, Textarea, type SelectOption } from "../ui/input";
import { useAction } from "../../hooks/useAsync";

export interface FieldDef {
  name: string;
  label: string;
  type: "text" | "textarea" | "date" | "datetime" | "select";
  required?: boolean;
  maxLength?: number;
  options?: SelectOption[];
  placeholder?: string;
  hint?: string;
  wide?: boolean;
}

interface Props {
  fields: FieldDef[];
  initial: Record<string, string>;
  // converte os valores do formulário no corpo esperado pela API.
  toRequest: (values: Record<string, string>) => unknown;
  onSubmit: (data: never) => Promise<void>;
  onCancel: () => void;
}

export default function SimpleForm({ fields, initial, toRequest, onSubmit, onCancel }: Props) {
  const [values, setValues] = useState(initial);
  const save = useAction(onSubmit as (data: unknown) => Promise<void>);
  const set = (name: string, v: string) => setValues((s) => ({ ...s, [name]: v }));
  const err = (name: string) => save.error?.fieldErrors[name]?.[0];

  const handleSubmit = (e: FormEvent) => {
    e.preventDefault();
    void save.run(toRequest(values));
  };

  return (
    <form onSubmit={handleSubmit} noValidate>
      <div className="grid gap-4 sm:grid-cols-2">
        {fields.map((f) => {
          const common = { label: f.label, required: f.required, error: err(f.name), hint: f.hint, containerClassName: f.wide ? "sm:col-span-2" : undefined };
          if (f.type === "textarea")
            return <Textarea key={f.name} {...common} maxLength={f.maxLength} value={values[f.name] ?? ""} placeholder={f.placeholder} onChange={(e) => set(f.name, e.target.value)} />;
          if (f.type === "select")
            return <SelectField key={f.name} {...common} options={f.options ?? []} value={values[f.name] ?? ""} onChange={(e) => set(f.name, e.target.value)} />;
          return (
            <Input key={f.name} {...common} type={f.type === "datetime" ? "datetime-local" : f.type} maxLength={f.maxLength}
              placeholder={f.placeholder} value={values[f.name] ?? ""} onChange={(e) => set(f.name, e.target.value)} />
          );
        })}
      </div>
      <ErrorMessage error={save.error && !Object.keys(save.error.fieldErrors).length ? save.error : null} className="mt-4" />
      <FormActions onCancel={onCancel} loading={save.pending} />
    </form>
  );
}
