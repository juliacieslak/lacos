// máscara e validação de CEP. exemplo: 01001000 > 01001-000.

export function onlyDigits(value: string) {
  return value.replace(/\D/g, "");
}

export function formatCep(value: string) {
  const digits = onlyDigits(value).slice(0, 8);
  return digits.length > 5 ? `${digits.slice(0, 5)}-${digits.slice(5)}` : digits;
}

// mesmas regras do backend: 8 dígitos e diferente de 00000000.
export function isValidCep(value: string) {
  const digits = onlyDigits(value);
  return digits.length === 8 && digits !== "00000000";
}
