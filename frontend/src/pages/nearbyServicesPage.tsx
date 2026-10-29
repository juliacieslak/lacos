// "Serviços próximos": farmácias perto de um CEP (ViaCEP → geocodificação → OpenStreetMap, via backend).
import { Info, MapPin } from "lucide-react";
import CepSearchForm from "../components/locations/cepSearchForm";
import HealthServiceList from "../components/locations/healthServiceList";
import { Card, PageHeader } from "../components/ui/card";
import ErrorMessage from "../components/ui/errorMessage";
import Loading from "../components/ui/loading";
import { useSession } from "../contexts/sessionContext";
import { useNearbyServices } from "../hooks/useNearbyServices";
import type { GeoPrecision } from "../types/location";

const precisionText: Record<GeoPrecision, string> = {
  STREET: "a partir do logradouro do CEP",
  POSTAL_CODE: "a partir do CEP",
  CITY: "a partir do centro da cidade (o CEP não tinha localização mais precisa)",
};

// mensagens específicas para cada erro devolvido pela API.
const errorText: Record<string, string> = {
  NOT_FOUND: "CEP não encontrado. Confira os números digitados.",
  VALIDATION_FAILED: "CEP inválido. Informe os 8 dígitos (ex.: 80010-000).",
  GEOCODING_FAILED: "Não conseguimos localizar este CEP no mapa. Tente um CEP próximo.",
  EXTERNAL_SERVICE_UNAVAILABLE: "O serviço de mapas está indisponível no momento. Tente novamente em instantes.",
};

export default function NearbyServicesPage() {
  const { session } = useSession();
  const { result, loading, error, search } = useNearbyServices();

  return (
    <>
      <PageHeader eyebrow="Serviços próximos" title="Farmácias perto de você" description="Informe um CEP para encontrar as farmácias mais próximas da região." />
      <Card className="mb-6">
        <CepSearchForm loading={loading} initialCep={session?.user.cep ?? ""} onSearch={(cep, radius) => void search(cep, radius)} />
      </Card>

      {error && <ErrorMessage error={errorText[error.code] ?? error.message} className="mb-6" />}
      {loading && <Loading label="Buscando farmácias próximas..." />}

      {result && !loading && (
        <Card>
          <div className="mb-4 flex flex-wrap items-start justify-between gap-3">
            <div>
              <p className="flex items-center gap-1.5 text-sm font-semibold text-ink"><MapPin className="h-4 w-4 text-lavender-500" />
                {[result.street, result.neighborhood].filter(Boolean).join(" · ") || "CEP geral da cidade"} — {result.city}/{result.state} ({result.cep})
              </p>
              <p className="mt-1 text-xs text-mist-500">{result.pharmacies.length} farmácia(s) em até {result.radiusMeters / 1000} km · distância calculada {precisionText[result.origin.precision]}.</p>
            </div>
          </div>
          <HealthServiceList pharmacies={result.pharmacies} radius={result.radiusMeters} />
          <p className="mt-4 flex items-start gap-1.5 text-[11px] text-mist-400">
            <Info className="mt-0.5 h-3 w-3 shrink-0" /> Fonte: {result.source}. Distâncias aproximadas em linha reta; confirme horário e endereço com o estabelecimento.
          </p>
        </Card>
      )}
    </>
  );
}
