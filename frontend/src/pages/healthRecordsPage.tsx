// aba "Saúde": condições com CID (RF14), alergias (RF16) e cirurgias (RF15).
import { AlertTriangle, Scissors, Stethoscope } from "lucide-react";
import RecordSection from "../components/records/recordSection";
import SimpleForm from "../components/records/simpleForm";
import Badge from "../components/ui/badge";
import { usePatientContext } from "../contexts/patientContext";
import { allergyService, conditionService, surgeryService } from "../services/clinicalService";
import type { Allergy, AllergyRequest, AllergySeverity, HealthCondition, HealthConditionRequest, HealthConditionStatus, Surgery, SurgeryRequest } from "../types/clinical";
import { formatDate } from "../utils/formatDate";
import { conditionStatusLabels, severityLabels, toOptions } from "../utils/labels";

export default function HealthRecordsPage() {
  const { patient } = usePatientContext();
  const common = { patientId: patient.id, permissions: patient.myPermissions };

  return (
    <div className="space-y-6">
      <RecordSection<HealthCondition, HealthConditionRequest> {...common} title="Condições de saúde" subtitle="Diagnósticos com código CID"
        icon={<Stethoscope className="h-4 w-4" />} emptyText="Nenhuma condição registrada" service={conditionService}
        renderItem={(c) => (
          <>
            <p className="font-semibold text-ink">{c.name} <span className="font-normal text-mist-500">· CID {c.cidCode}</span></p>
            <div className="mt-1.5 flex flex-wrap gap-1.5">
              <Badge tone={c.status === "ACTIVE" ? "warning" : c.status === "IN_REMISSION" ? "serene" : "neutral"}>{conditionStatusLabels[c.status]}</Badge>
              {c.diagnosisDate && <Badge tone="neutral">Diagnóstico: {formatDate(c.diagnosisDate)}</Badge>}
            </div>
            {c.notes && <p className="mt-2 text-sm text-mist-700">{c.notes}</p>}
          </>
        )}
        renderForm={({ item, ...rest }) => (
          <SimpleForm {...rest}
            fields={[
              { name: "name", label: "Condição / transtorno", type: "text", required: true, maxLength: 200, wide: true },
              { name: "cidCode", label: "Código CID", type: "text", required: true, placeholder: "Ex.: I10, E11.9", hint: "CID-10 ou CID-11" },
              { name: "diagnosisDate", label: "Data do diagnóstico", type: "date" },
              { name: "status", label: "Status", type: "select", options: toOptions(conditionStatusLabels) },
              { name: "notes", label: "Observações", type: "textarea", maxLength: 2000, wide: true },
            ]}
            initial={{ name: item?.name ?? "", cidCode: item?.cidCode ?? "", diagnosisDate: item?.diagnosisDate ?? "", status: item?.status ?? "ACTIVE", notes: item?.notes ?? "" }}
            toRequest={(v): HealthConditionRequest => ({ name: v.name, cidCode: v.cidCode.toUpperCase(), diagnosisDate: v.diagnosisDate || null, status: v.status as HealthConditionStatus, notes: v.notes || null })} />
        )} />

      <RecordSection<Allergy, AllergyRequest> {...common} title="Alergias" icon={<AlertTriangle className="h-4 w-4" />}
        emptyText="Nenhuma alergia registrada" service={allergyService}
        renderItem={(a) => (
          <>
            <p className="font-semibold text-ink">{a.causativeAgent}</p>
            <p className="text-sm text-mist-700">{a.manifestation}</p>
            <div className="mt-1.5 flex flex-wrap gap-1.5">
              <Badge tone={a.severity === "SEVERE" || a.severity === "LIFE_THREATENING" ? "danger" : "warning"}>{severityLabels[a.severity]}</Badge>
              {a.identifiedOn && <Badge tone="neutral">Identificada em {formatDate(a.identifiedOn)}</Badge>}
            </div>
          </>
        )}
        renderForm={({ item, ...rest }) => (
          <SimpleForm {...rest}
            fields={[
              { name: "causativeAgent", label: "Agente causador", type: "text", required: true, maxLength: 200 },
              { name: "severity", label: "Gravidade", type: "select", options: toOptions(severityLabels) },
              { name: "manifestation", label: "Forma de manifestação", type: "textarea", required: true, maxLength: 1000, wide: true },
              { name: "identifiedOn", label: "Data de identificação", type: "date" },
            ]}
            initial={{ causativeAgent: item?.causativeAgent ?? "", severity: item?.severity ?? "MODERATE", manifestation: item?.manifestation ?? "", identifiedOn: item?.identifiedOn ?? "" }}
            toRequest={(v): AllergyRequest => ({ causativeAgent: v.causativeAgent, manifestation: v.manifestation, identifiedOn: v.identifiedOn || null, severity: v.severity as AllergySeverity })} />
        )} />

      <RecordSection<Surgery, SurgeryRequest> {...common} title="Cirurgias" icon={<Scissors className="h-4 w-4" />}
        emptyText="Nenhuma cirurgia registrada" service={surgeryService}
        renderItem={(s) => (
          <>
            <p className="font-semibold text-ink">{s.procedureName} <span className="font-normal text-mist-500">· {formatDate(s.performedOn)}</span></p>
            <p className="text-sm text-mist-700">{s.reason}</p>
          </>
        )}
        renderForm={({ item, ...rest }) => (
          <SimpleForm {...rest}
            fields={[
              { name: "procedureName", label: "Procedimento", type: "text", required: true, maxLength: 200 },
              { name: "performedOn", label: "Data de realização", type: "date", required: true },
              { name: "reason", label: "Motivo", type: "textarea", required: true, maxLength: 1000, wide: true },
            ]}
            initial={{ procedureName: item?.procedureName ?? "", performedOn: item?.performedOn ?? "", reason: item?.reason ?? "" }}
            toRequest={(v): SurgeryRequest => ({ procedureName: v.procedureName, performedOn: v.performedOn, reason: v.reason })} />
        )} />
    </div>
  );
}
