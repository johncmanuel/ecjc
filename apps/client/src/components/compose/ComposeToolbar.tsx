const TOOLS = [
  { label: "📷", name: "Photo" },
  { label: "🎞", name: "Video" },
  { label: "GIF", name: "Gif" },
];

export default function ComposeToolbar({ onAttach }: { onAttach?: (kind: string) => void }) {
  return (
    <div className="mt-auto pt-4.5 border-t border-line flex gap-5">
      {TOOLS.map((tool) => (
        <button
          key={tool.name}
          type="button"
          aria-label={`Attach ${tool.name.toLowerCase()}`}
          onClick={() => onAttach?.(tool.name)}
          className="w-9 h-9 rounded-[10px] bg-card border border-line flex items-center justify-center text-ink-soft text-sm"
        >
          {tool.label}
        </button>
      ))}
    </div>
  );
}