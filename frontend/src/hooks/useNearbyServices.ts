// controla a busca de farmácias por CEP.
// - valida o CEP antes de chamar a API;
// - cancela a busca anterior quando uma nova é iniciada (evita resultados fora de ordem);
// - ignora a repetição da mesma busca enquanto ela ainda está em andamento (evita requisições duplicadas).
import { useCallback, useEffect, useRef, useState } from "react";
import { locationService } from "../services/locationService";
import { ApiError, isAbortError } from "../services/api";
import { toApiError } from "./useAsync";
import { isValidCep, onlyDigits } from "../utils/formatCep";
import type { PharmacySearchResult } from "../types/location";

export function useNearbyServices() {
  const [result, setResult] = useState<PharmacySearchResult | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<ApiError | null>(null);
  const controllerRef = useRef<AbortController | null>(null);
  const currentKeyRef = useRef<string | null>(null);

  useEffect(() => () => controllerRef.current?.abort(), []);

  const search = useCallback(async (cep: string, radius: number) => {
    const digits = onlyDigits(cep);
    if (!isValidCep(digits)) {
      setError(new ApiError(400, "INVALID_CEP", "CEP inválido. Informe os 8 dígitos (ex.: 80010-000)."));
      return;
    }
    const key = `${digits}:${radius}`;
    if (loading && currentKeyRef.current === key) return;

    controllerRef.current?.abort();
    const controller = new AbortController();
    controllerRef.current = controller;
    currentKeyRef.current = key;
    setLoading(true);
    setError(null);
    try {
      const data = await locationService.searchPharmacies(digits, radius, controller.signal);
      if (!controller.signal.aborted) setResult(data);
    } catch (err) {
      if (!controller.signal.aborted && !isAbortError(err)) {
        setError(toApiError(err));
        setResult(null);
      }
    } finally {
      if (controllerRef.current === controller) {
        setLoading(false);
        currentKeyRef.current = null;
      }
    }
  }, [loading]);

  const reset = useCallback(() => {
    controllerRef.current?.abort();
    setResult(null);
    setError(null);
    setLoading(false);
  }, []);

  return { result, loading, error, search, reset };
}
