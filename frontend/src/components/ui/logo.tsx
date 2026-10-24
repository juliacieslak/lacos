import logoImage from "../../assets/images/lacos.jpeg";

// define as propriedades que o componente Logo pode receber.
interface LogoProps {
  width?: number;
  alt?: string;

  // classes adicionais do Tailwind CSS.
  className?: string;
}

// componente reutilizável da logo do Laços.
export default function Logo({
  width = 150,
  alt = "Laços — cuidado conectado",
  className = "",
}: LogoProps) {
  return (
    <img
      src={logoImage}
      alt={alt}
      width={width}
      height="auto"
      // impede deformação da imagem.
      className={`h-auto object-contain ${className}`}
    />
  );
}

