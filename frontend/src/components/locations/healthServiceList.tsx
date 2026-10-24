// lista as farmácias encontradas, já ordenadas por distância pelo backend.
import HealthServiceCard from "./healthServiceCard";
import EmptyState from "../ui/emptyState";
import type { Pharmacy } from "../../types/location";

export default function HealthServiceList({ pharmacies, radius }: { pharmacies: Pharmacy[]; radius: number }) {
  if (pharmacies.length === 0)
    return <EmptyState title="Nenhuma farmácia encontrada" description={`Não há farmácias cadastradas no OpenStreetMap em até ${radius / 1000} km deste CEP. Tente aumentar o raio.`} />;
  return <ul className="space-y-3">{pharmacies.map((p) => <HealthServiceCard key={p.id} pharmacy={p} />)}</ul>;
}
