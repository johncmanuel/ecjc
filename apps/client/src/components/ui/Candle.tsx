export default function Candle({ lit = true }: { lit?: boolean }) {
  return (
    <div
      className={`w-14 h-14 rounded-full flex items-center justify-center ${
        lit ? "bg-[radial-gradient(circle,var(--glow-soft)_0%,transparent_70%)]" : "bg-card border border-line"
      }`}
    >
      <div
        className={
          lit
            ? "w-6 h-6 rounded-full bg-glow shadow-[0_0_20px_5px_var(--glow-soft)] animate-flicker"
            : "w-5 h-5 rounded-full bg-ink-faint opacity-50"
        }
      />
    </div>
  );
}