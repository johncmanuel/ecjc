import Link from "next/link";

export function FabButton({ href, label }: { href: string; label: string }) {
  return (
    <Link
      href={href}
      aria-label={label}
      // "right" is computed so the button sits 1.5rem inside the right edge
      // of the centered max-w-md (28rem) column, not the raw viewport edge.
      // On screens <= 448px wide this collapses to the plain 1.5rem case.
      className="fixed bottom-6 right-[max(1.5rem,calc(50vw_-_224px_+_1.5rem))] w-13 h-13 rounded-full bg-ink text-paper text-2xl flex items-center justify-center shadow-[0_10px_24px_-8px_rgba(30,32,26,0.5)] z-40"
    >
      +
    </Link>
  );
}