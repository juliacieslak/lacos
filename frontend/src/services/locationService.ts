// busca de serviços próximos. A chamada é feita para o backend, nunca diretamente para as APIs externas
// (ViaCEP, Nominatim e Overpass), o que permite cache, limite de uso e troca de provedor.
import { apiRequest } from "./api";
import type { Address } from "../types/address";
import type { PharmacySearchResult } from "../types/location";

export const locationService = {
  searchPharmacies: (cep: string, radius: number, signal?: AbortSignal) =>
    apiRequest<PharmacySearchResult>("/api/locations/pharmacies", {
      query: { cep, radius },
      signal,
      // geocodificação + consulta espacial podem levar alguns segundos.
      timeoutMs: 60_000,
    }),
  lookupCep: (cep: string, signal?: AbortSignal) => apiRequest<Address>(`/api/lookups/cep/${cep}`, { signal }),
};
