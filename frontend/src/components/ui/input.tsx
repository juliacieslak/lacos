// campo de entrada padrão com rótulo, ícone, dica e mensagem de erro acessíveis.
import { useId, type InputHTMLAttributes, type ReactNode, type SelectHTMLAttributes, type TextareaHTMLAttributes } from "react";

interface FieldShellProps {
  label: string;
  error?: string;
  hint?: string;
  required?: boolean;
  id: string;
  children: ReactNode;
  className?: string;
}

const fieldBox =
  "flex items-center gap-3 rounded-xl border bg-mist-100 px-4 py-3 transition focus-within:border-lavender-500 focus-within:ring-2 focus-within:ring-lavender-300/50";

export function FieldShell({ label, error, hint, required, id, children, className = "" }: FieldShellProps) {
  return (
    <div className={`block ${className}`}>
      <label htmlFor={id} className="mb-2 block text-xs font-semibold uppercase tracking-[0.06em] text-mist-500">
        {label} {required && <span className="text-danger-600" aria-hidden>*</span>}
      </label>
      {children}
      {error ? (
        <p id={`${id}-error`} className="mt-1.5 text-xs text-danger-600">{error}</p>
      ) : hint ? (
        <p id={`${id}-hint`} className="mt-1.5 text-xs text-mist-500">{hint}</p>
      ) : null}
    </div>
  );
}

interface InputProps extends InputHTMLAttributes<HTMLInputElement> {
  label: string;
  error?: string;
  hint?: string;
  icon?: ReactNode;
  containerClassName?: string;
}

export default function Input({ label, error, hint, icon, containerClassName, id, required, className = "", ...props }: InputProps) {
  const autoId = useId();
  const inputId = id ?? autoId;
  return (
    <FieldShell label={label} error={error} hint={hint} required={required} id={inputId} className={containerClassName}>
      <span className={`${fieldBox} ${error ? "border-danger-600" : "border-mist-200"}`}>
        {icon && <span className="shrink-0 text-mist-400">{icon}</span>}
        <input
          id={inputId}
          required={required}
          aria-invalid={Boolean(error)}
          aria-describedby={error ? `${inputId}-error` : hint ? `${inputId}-hint` : undefined}
          className={`w-full min-w-0 bg-transparent text-sm text-ink outline-none placeholder:text-mist-400 ${className}`}
          {...props}
        />
      </span>
    </FieldShell>
  );
}

interface TextareaProps extends TextareaHTMLAttributes<HTMLTextAreaElement> {
  label: string;
  error?: string;
  hint?: string;
  containerClassName?: string;
}

export function Textarea({ label, error, hint, id, required, containerClassName, ...props }: TextareaProps) {
  const autoId = useId();
  const inputId = id ?? autoId;
  return (
    <FieldShell label={label} error={error} hint={hint} required={required} id={inputId} className={containerClassName}>
      <textarea
        id={inputId}
        required={required}
        rows={3}
        aria-invalid={Boolean(error)}
        className={`w-full rounded-xl border bg-mist-100 px-4 py-3 text-sm text-ink outline-none transition placeholder:text-mist-400 focus:border-lavender-500 focus:ring-2 focus:ring-lavender-300/50 ${error ? "border-danger-600" : "border-mist-200"}`}
        {...props}
      />
    </FieldShell>
  );
}

export interface SelectOption { value: string; label: string }

interface SelectFieldProps extends SelectHTMLAttributes<HTMLSelectElement> {
  label: string;
  options: SelectOption[];
  error?: string;
  hint?: string;
  placeholder?: string;
  containerClassName?: string;
}

export function SelectField({ label, options, error, hint, placeholder, id, required, containerClassName, ...props }: SelectFieldProps) {
  const autoId = useId();
  const inputId = id ?? autoId;
  return (
    <FieldShell label={label} error={error} hint={hint} required={required} id={inputId} className={containerClassName}>
      <select
        id={inputId}
        required={required}
        aria-invalid={Boolean(error)}
        className={`w-full rounded-xl border bg-mist-100 px-4 py-3 text-sm text-ink outline-none transition focus:border-lavender-500 focus:ring-2 focus:ring-lavender-300/50 ${error ? "border-danger-600" : "border-mist-200"}`}
        {...props}
      >
        {placeholder && <option value="">{placeholder}</option>}
        {options.map((o) => (
          <option key={o.value} value={o.value}>{o.label}</option>
        ))}
      </select>
    </FieldShell>
  );
}
