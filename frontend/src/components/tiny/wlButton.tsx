import type { ButtonHTMLAttributes , ReactNode} from "react";

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  children: ReactNode;
  className?: string;
}

export default function WlButton({
  disabled = false,
  onClick,
  className = "",
  children,
  ...rest
}: ButtonProps) {
  const base =
    "bg-blue-600 text-white p-2 rounded disabled:opacity-50 disabled:cursor-not-allowed";
  return (
    <button
      {...rest}
      disabled={disabled}
      onClick={onClick}
      className={`${base} ${className}`}
    >
      {children}
    </button>
  );
}