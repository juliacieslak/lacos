// endereço retornado pela consulta de CEP do backend (GET /api/lookups/cep/{cep}).
export interface Address {
  cep: string;
  street: string | null;
  neighborhood: string | null;
  city: string;
  state: string;
}
