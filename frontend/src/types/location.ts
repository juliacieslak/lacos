// busca de farmácias por CEP (GET /api/locations/pharmacies).
// campos nulos = informação não disponibilizada pelo provedor (nada é inventado).

export type GeoPrecision = "STREET" | "POSTAL_CODE" | "CITY";

export interface Pharmacy {
  id: string;
  name: string;
  hasRegisteredName: boolean;
  street: string | null;
  houseNumber: string | null;
  neighborhood: string | null;
  city: string | null;
  state: string | null;
  postcode: string | null;
  phone: string | null;
  latitude: number;
  longitude: number;
  distanceMeters: number;
  googleMapsUrl: string;
}

export interface PharmacySearchResult {
  cep: string;
  street: string | null;
  neighborhood: string | null;
  city: string;
  state: string;
  origin: { latitude: number; longitude: number; precision: GeoPrecision };
  radiusMeters: number;
  source: string;
  pharmacies: Pharmacy[];
}
