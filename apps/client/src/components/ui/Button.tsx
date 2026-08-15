import { ButtonHTMLAttributes } from "react";

type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: "ink" | "soft" | "thread-a";
};

// base button
export default function Button({ variant = "ink", className = "", children, ...props }: ButtonProps) {
  const base = "rounded-full text-sm font-semibold flex items-center justify-center gap-2 transition-opacity active:opacity-80";
  const variants = {
    ink: "bg-ink text-btn-text px-4 py-1.5",
    soft: "bg-card border border-line text-ink px-4 py-3",
    "thread-a": "bg-thread-a text-btn-text px-4 py-3.5",
  };

  return (
    <button className={`${base} ${variants[variant]} ${className}`} {...props}>
      {children}
    </button>
  );
}