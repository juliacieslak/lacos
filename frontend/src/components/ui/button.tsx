// botão padrão da aplicação (variações de cor, tamanho e estado de carregamento).
import { Loader2 } from "lucide-react";
import type { ButtonHTMLAttributes, ReactNode } from "react";

type Variant = "primary" | "secondary" | "ghost" | "danger" | "success";
type Size = "sm" | "md" | "lg";

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: Variant;
  size?: Size;
  loading?: boolean;
  icon?: ReactNode;
  fullWidth?: boolean;
}

const variants: Record<Variant, string> = {
  // lavanda 600 garante contraste AA com texto branco.
  primary: "bg-lavender-600 text-white shadow-[0_10px_24px_rgba(123,110,199,0.24)] hover:bg-lavender-700",
  secondary: "border border-mist-200 bg-white text-lavender-700 hover:border-lavender-300 hover:bg-lavender-50",
  ghost: "text-mist-500 hover:bg-lavender-50 hover:text-lavender-700",
  danger: "bg-danger-600 text-white hover:bg-[#9a1d3f]",
  success: "bg-sage-700 text-white hover:bg-[#3e5d3f]",
};

const sizes: Record<Size, string> = {
  sm: "px-3 py-2 text-xs",
  md: "px-4 py-2.5 text-sm",
  lg: "px-5 py-3.5 text-sm",
};

export default function Button({
  variant = "primary", size = "md", loading = false, icon, fullWidth, className = "", children, disabled, type = "button", ...props
}: ButtonProps) {
  return (
    <button
      type={type}
      disabled={disabled || loading}
      className={`inline-flex items-center justify-center gap-2 rounded-xl font-semibold transition active:scale-[0.98] disabled:cursor-not-allowed disabled:opacity-60 ${variants[variant]} ${sizes[size]} ${fullWidth ? "w-full" : ""} ${className}`}
      {...props}
    >
      {loading ? <Loader2 className="h-4 w-4 animate-spin" aria-hidden /> : icon}
      {children}
    </button>
  );
}
