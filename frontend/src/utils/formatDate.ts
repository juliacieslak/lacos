// formatação de datas no padrão brasileiro e conversões entre o formato da API e dos campos de formulário.

const dateFmt = new Intl.DateTimeFormat("pt-BR", { day: "2-digit", month: "2-digit", year: "numeric" });
const dateTimeFmt = new Intl.DateTimeFormat("pt-BR", { day: "2-digit", month: "2-digit", year: "numeric", hour: "2-digit", minute: "2-digit" });
const timeFmt = new Intl.DateTimeFormat("pt-BR", { hour: "2-digit", minute: "2-digit" });
const longFmt = new Intl.DateTimeFormat("pt-BR", { weekday: "long", day: "numeric", month: "long", year: "numeric" });

// "yyyy-MM-dd" (DateOnly) é interpretado como data local, sem deslocamento de fuso.
function parseDateOnly(value: string) {
  const [y, m, d] = value.slice(0, 10).split("-").map(Number);
  return new Date(y, m - 1, d);
}

export const formatDate = (value?: string | null) => (value ? dateFmt.format(value.length === 10 ? parseDateOnly(value) : new Date(value)) : "—");
export const formatDateTime = (value?: string | null) => (value ? dateTimeFmt.format(new Date(value)) : "—");
export const formatTime = (value?: string | null) => (value ? timeFmt.format(new Date(value)) : "—");
export const formatLongDate = (value: Date | string) =>
  longFmt.format(typeof value === "string" ? parseDateOnly(value) : value).replace(/^\w/, (c) => c.toUpperCase());

// "08:00:00" → "08:00"
export const formatTimeOnly = (value: string) => value.slice(0, 5);

// data local de hoje em "yyyy-MM-dd".
export function todayIso(date = new Date()) {
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}

export function addDays(iso: string, days: number) {
  const d = parseDateOnly(iso);
  d.setDate(d.getDate() + days);
  return todayIso(d);
}

// valor para <input type="datetime-local"> a partir de um instante ISO, e o inverso.
export function toDateTimeLocal(value?: string | null) {
  const d = value ? new Date(value) : new Date();
  const pad = (n: number) => String(n).padStart(2, "0");
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

export const fromDateTimeLocal = (value: string) => (value ? new Date(value).toISOString() : null);

export function greeting(date = new Date()) {
  const h = date.getHours();
  return h < 12 ? "Bom dia" : h < 18 ? "Boa tarde" : "Boa noite";
}
