// formulário para criar ou editar um paciente (RF03).
import { useState, type FormEvent } from "react";
import { IdCard, Mail, MapPin, Phone, Ruler, UserRound } from "lucide-react";
import ErrorMessage from "../ui/errorMessage";
import FormActions from "../ui/formActions";
import Input, { SelectField } from "../ui/input";
import { useAction } from "../../hooks/useAsync";
import type { CreatePatientRequest, Patient, UpdatePatientRequest } from "../../types/patient";
import { formatCep, isValidCep } from "../../utils/formatCep";
import { formatCpf, formatPhone, isValidCpf } from "../../utils/validation";
import { ethnicityLabels, genderLabels, toOptions } from "../../utils/labels";

interface Props {
  patient?: Patient;
  onSubmit: (data: CreatePatientRequest | UpdatePatientRequest) => Promise<unknown>;
  onCancel: () => void;
}

export default function PatientForm({ patient, onSubmit, onCancel }: Props) {
  const isNew = !patient;
  const [form, setForm] = useState({
    fullName: patient?.fullName ?? "", birthDate: patient?.birthDate ?? "", birthPlace: patient?.birthPlace ?? "",
    cep: formatCep(patient?.cep ?? ""), ethnicity: patient?.ethnicity ?? "NAO_DECLARADA", gender: patient?.gender ?? "FEMALE",
    email: patient?.email ?? "", phone: formatPhone(patient?.phone ?? ""), heightCm: patient?.heightCm ? String(patient.heightCm) : "",
    cpf: "", relationshipType: "",
  });
  const [localErrors, setLocalErrors] = useState<Record<string, string[]>>({});
  const save = useAction(onSubmit);
  const set = (key: keyof typeof form, value: string) => setForm((f) => ({ ...f, [key]: value }));

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    const errors: Record<string, string[]> = {};
    if (isNew && !isValidCpf(form.cpf)) errors.cpf = ["CPF inválido."];
    if (!isValidCep(form.cep)) errors.cep = ["CEP inválido."];
    const height = Number(form.heightCm);
    if (!height || height < 30 || height > 250) errors.heightCm = ["Informe a altura entre 30 e 250 cm."];
    setLocalErrors(errors);
    if (Object.keys(errors).length) return;

    const base: UpdatePatientRequest = {
      fullName: form.fullName, birthDate: form.birthDate, birthPlace: form.birthPlace, cep: form.cep,
      ethnicity: form.ethnicity as UpdatePatientRequest["ethnicity"], gender: form.gender as UpdatePatientRequest["gender"],
      email: form.email || null, phone: form.phone || null, heightCm: height,
    };
    await save.run(isNew ? { ...base, cpf: form.cpf, relationshipType: form.relationshipType } : base);
  };

  const errors = { ...(save.error?.fieldErrors ?? {}), ...localErrors };
  const err = (k: string) => errors[k]?.[0];

  return (
    <form onSubmit={handleSubmit} noValidate>
      <div className="grid gap-4 sm:grid-cols-2">
        <Input label="Nome completo" required icon={<UserRound className="h-4 w-4" />} value={form.fullName} maxLength={150}
          onChange={(e) => set("fullName", e.target.value)} error={err("fullName")} containerClassName="sm:col-span-2" />
        <Input label="Data de nascimento" type="date" required value={form.birthDate} onChange={(e) => set("birthDate", e.target.value)} error={err("birthDate")} />
        <Input label="Local de nascimento" required value={form.birthPlace} placeholder="Cidade/UF" maxLength={150}
          onChange={(e) => set("birthPlace", e.target.value)} error={err("birthPlace")} />
        {isNew && (
          <Input label="CPF" required icon={<IdCard className="h-4 w-4" />} value={form.cpf} inputMode="numeric" placeholder="000.000.000-00"
            onChange={(e) => set("cpf", formatCpf(e.target.value))} error={err("cpf")} />
        )}
        <Input label="CEP onde mora" required icon={<MapPin className="h-4 w-4" />} value={form.cep} inputMode="numeric" placeholder="00000-000"
          onChange={(e) => set("cep", formatCep(e.target.value))} error={err("cep")} />
        <SelectField label="Gênero" required options={toOptions(genderLabels)} value={form.gender} onChange={(e) => set("gender", e.target.value)} error={err("gender")} />
        <SelectField label="Etnia" required options={toOptions(ethnicityLabels)} value={form.ethnicity} onChange={(e) => set("ethnicity", e.target.value)} error={err("ethnicity")} />
        <Input label="Altura (cm)" required type="number" min={30} max={250} icon={<Ruler className="h-4 w-4" />} value={form.heightCm}
          onChange={(e) => set("heightCm", e.target.value)} error={err("heightCm")} />
        <Input label="Telefone" icon={<Phone className="h-4 w-4" />} value={form.phone} inputMode="tel" hint="Opcional"
          onChange={(e) => set("phone", formatPhone(e.target.value))} error={err("phone")} />
        <Input label="E-mail" type="email" icon={<Mail className="h-4 w-4" />} value={form.email} hint="Opcional" maxLength={255}
          onChange={(e) => set("email", e.target.value)} error={err("email")} />
        {isNew && (
          <Input label="Sua relação com o paciente" required value={form.relationshipType} placeholder="Ex.: filha, neto, cuidador" maxLength={50}
            onChange={(e) => set("relationshipType", e.target.value)} error={err("relationshipType")} containerClassName="sm:col-span-2" />
        )}
      </div>
      {isNew && <p className="mt-4 text-xs text-mist-500">Ao cadastrar, você se torna o administrador do perfil e poderá convidar a rede de cuidado.</p>}
      <ErrorMessage error={save.error && !Object.keys(save.error.fieldErrors).length ? save.error : null} className="mt-4" />
      <FormActions onCancel={onCancel} loading={save.pending} submitLabel={isNew ? "Cadastrar paciente" : "Salvar alterações"} />
    </form>
  );
}
