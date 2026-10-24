// exibe nome, endereço, distância, telefone e link do Google Maps de uma farmácia.
// campos não informados pelo provedor simplesmente não são exibidos (nada é inventado).
import { ExternalLink, MapPin, Phone, Pill } from "lucide-react";
import Badge from "../ui/badge";
import type { Pharmacy } from "../../types/location";
import { formatDistance } from "../../utils/formatMeasurement";

export default function HealthServiceCard({ pharmacy: p }: { pharmacy: Pharmacy }) {
  const street = [p.street, p.houseNumber].filter(Boolean).join(", ");
  const cityState = [p.city, p.state].filter(Boolean).join("/");
  const phoneHref = p.phone ? `tel:${p.phone.replace(/[^\d+]/g, "")}` : undefined;
  return (
    <li className="flex flex-col gap-3 rounded-2xl border border-mist-200 bg-white p-4 sm:flex-row sm:items-center">
      <span className="grid h-10 w-10 shrink-0 place-items-center rounded-xl bg-sage-100 text-sage-700"><Pill className="h-5 w-5" /></span>
      <div className="min-w-0 flex-1">
        <div className="flex flex-wrap items-center gap-2">
          <p className={`font-semibold ${p.hasRegisteredName ? "text-ink" : "italic text-mist-500"}`}>{p.name}</p>
          <Badge tone="lavender">{formatDistance(p.distanceMeters)}</Badge>
        </div>
        <p className="mt-0.5 flex items-start gap-1 text-sm text-mist-500">
          <MapPin className="mt-0.5 h-3.5 w-3.5 shrink-0" />
          <span>
            {street || "Endereço não cadastrado no mapa"}
            {p.neighborhood && ` · ${p.neighborhood}`}
            {cityState && ` · ${cityState}`}
          </span>
        </p>
        {p.phone && (
          <a href={phoneHref} className="mt-0.5 inline-flex items-center gap-1 text-sm text-serene-700 hover:underline"><Phone className="h-3.5 w-3.5" /> {p.phone}</a>
        )}
      </div>
      <a href={p.googleMapsUrl} target="_blank" rel="noopener noreferrer"
        className="inline-flex items-center justify-center gap-1.5 rounded-xl border border-mist-200 px-3 py-2 text-sm font-semibold text-lavender-700 hover:border-lavender-300 hover:bg-lavender-50">
        <ExternalLink className="h-4 w-4" /> Abrir no Maps
      </a>
    </li>
  );
}
