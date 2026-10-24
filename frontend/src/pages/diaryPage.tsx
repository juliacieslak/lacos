// aba "Diário": diário de acompanhamento (RF04) e sintomas/queixas/alterações (RF09).
import { BookHeart, Thermometer } from "lucide-react";
import RecordSection from "../components/records/recordSection";
import SimpleForm from "../components/records/simpleForm";
import Badge from "../components/ui/badge";
import { usePatientContext } from "../contexts/patientContext";
import { diaryService, symptomService } from "../services/clinicalService";
import type { CareCategory } from "../types/careTask";
import type { DiaryEntry, DiaryRequest, Symptom, SymptomRequest, SymptomType } from "../types/clinical";
import { formatDateTime, fromDateTimeLocal, toDateTimeLocal } from "../utils/formatDate";
import { careCategoryLabels, symptomTypeLabels, toOptions } from "../utils/labels";

export default function DiaryPage() {
  const { patient } = usePatientContext();
  const common = { patientId: patient.id, permissions: patient.myPermissions };

  return (
    <div className="space-y-6">
      <RecordSection<DiaryEntry, DiaryRequest> {...common} title="Diário de acompanhamento"
        subtitle="Atividades, alimentação, higiene, sono, cuidados, queixas e dores" icon={<BookHeart className="h-4 w-4" />}
        emptyText="Nenhuma anotação no diário" service={diaryService}
        renderItem={(d) => (
          <>
            <div className="flex flex-wrap items-center gap-2">
              <Badge tone="sage">{careCategoryLabels[d.category]}</Badge>
              <span className="text-xs text-mist-500">{formatDateTime(d.occurredAt)}</span>
            </div>
            <p className="mt-1.5 whitespace-pre-line text-sm text-ink">{d.description}</p>
          </>
        )}
        renderForm={({ item, ...rest }) => (
          <SimpleForm {...rest}
            fields={[
              { name: "category", label: "Categoria", type: "select", options: toOptions(careCategoryLabels) },
              { name: "occurredAt", label: "Quando", type: "datetime" },
              { name: "description", label: "Descrição", type: "textarea", required: true, maxLength: 4000, wide: true },
            ]}
            initial={{ category: item?.category ?? "CARE_PROVIDED", occurredAt: toDateTimeLocal(item?.occurredAt), description: item?.description ?? "" }}
            toRequest={(v): DiaryRequest => ({ category: v.category as CareCategory, description: v.description, occurredAt: fromDateTimeLocal(v.occurredAt) })} />
        )} />

      <RecordSection<Symptom, SymptomRequest> {...common} title="Sintomas e queixas" icon={<Thermometer className="h-4 w-4" />}
        emptyText="Nenhum sintoma registrado" service={symptomService}
        renderItem={(s) => (
          <>
            <div className="flex flex-wrap items-center gap-2">
              <Badge tone="warning">{symptomTypeLabels[s.type]}</Badge>
              <span className="text-xs text-mist-500">{formatDateTime(s.occurredAt)}</span>
            </div>
            <p className="mt-1.5 text-sm text-ink">{s.description}</p>
          </>
        )}
        renderForm={({ item, ...rest }) => (
          <SimpleForm {...rest}
            fields={[
              { name: "type", label: "Tipo", type: "select", options: toOptions(symptomTypeLabels) },
              { name: "occurredAt", label: "Data e horário", type: "datetime" },
              { name: "description", label: "Descrição", type: "textarea", required: true, maxLength: 2000, wide: true },
            ]}
            initial={{ type: item?.type ?? "SYMPTOM", occurredAt: toDateTimeLocal(item?.occurredAt), description: item?.description ?? "" }}
            toRequest={(v): SymptomRequest => ({ type: v.type as SymptomType, description: v.description, occurredAt: fromDateTimeLocal(v.occurredAt) })} />
        )} />
    </div>
  );
}
