// campos de cadastro do usuário (RF01) compartilhados entre o onboarding e a edição de perfil.
import { Briefcase, MapPin, Phone, UserRound } from "lucide-react";
import Input, { SelectField } from "../ui/input";
import type { ProfileFields } from "../../types/user";
import type { FieldErrors } from "../../services/api";
import { formatCep } from "../../utils/formatCep";
import { formatPhone } from "../../utils/validation";
import { ethnicityLabels, genderLabels, profileTypeLabels, toOptions } from "../../utils/labels";

interface Props {
  value: ProfileFields;
  onChange: (value: ProfileFields) => void;
  errors: FieldErrors;
}

export default function ProfileFieldsForm({ value, onChange, errors }: Props) {
  const set = <K extends keyof ProfileFields>(key: K, v: ProfileFields[K]) => onChange({ ...value, [key]: v });
  const err = (key: string) => errors[key]?.[0];
  return (
    <div className="grid gap-4 sm:grid-cols-2">
      <SelectField label="Como você participa do cuidado?" required options={toOptions(profileTypeLabels)} value={value.profileType}
        onChange={(e) => set("profileType", e.target.value as ProfileFields["profileType"])} error={err("profileType")} containerClassName="sm:col-span-2" />
      <Input label="Nome completo" required icon={<UserRound className="h-4 w-4" />} value={value.name} maxLength={150}
        onChange={(e) => set("name", e.target.value)} error={err("name")} containerClassName="sm:col-span-2" autoComplete="name" />
      <Input label="Data de nascimento" type="date" required value={value.birthDate} onChange={(e) => set("birthDate", e.target.value)} error={err("birthDate")} />
      <Input label="Telefone" required icon={<Phone className="h-4 w-4" />} value={value.phone} inputMode="tel" placeholder="(41) 99999-0000"
        onChange={(e) => set("phone", formatPhone(e.target.value))} error={err("phone")} autoComplete="tel" />
      <Input label="Profissão" required icon={<Briefcase className="h-4 w-4" />} value={value.profession} maxLength={100}
        onChange={(e) => set("profession", e.target.value)} error={err("profession")} />
      <Input label="CEP" required icon={<MapPin className="h-4 w-4" />} value={value.cep} inputMode="numeric" placeholder="00000-000"
        onChange={(e) => set("cep", formatCep(e.target.value))} error={err("cep")} autoComplete="postal-code" />
      <SelectField label="Etnia" required options={toOptions(ethnicityLabels)} value={value.ethnicity}
        onChange={(e) => set("ethnicity", e.target.value as ProfileFields["ethnicity"])} error={err("ethnicity")} />
      <SelectField label="Gênero" required options={toOptions(genderLabels)} value={value.gender}
        onChange={(e) => set("gender", e.target.value as ProfileFields["gender"])} error={err("gender")} />
    </div>
  );
}
