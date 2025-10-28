import type { ButtonHTMLAttributes, ReactNode } from "react";

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  children: ReactNode;
  className?: string;
  isPrimary: boolean;
}

export default function WlButton({
  disabled = false,
  isPrimary = true,
  onClick,
  className = "",
  children,
  ...rest
}: ButtonProps) {
  let base = "text-white p-2 rounded disabled:opacity-50 disabled:cursor-not-allowed";
  if (isPrimary) {
    base += " bg-primary hover:bg-primary/80";
  }
  else {
    base += " bg-secondary hover:bg-secondary/80";
  }
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