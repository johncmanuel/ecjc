export default function MediaPreview({ onRemove }: { onRemove?: () => void }) {
  return (
    <div className="mt-4.5 h-36 rounded-2xl bg-gradient-to-br from-thread-a-soft to-paper-deep relative">
      <button
        type="button"
        aria-label="Remove attachment"
        onClick={onRemove}
        className="absolute top-2 right-2 w-5.5 h-5.5 rounded-full bg-ink/55 text-white text-xs flex items-center justify-center"
      >
        ✕
      </button>
    </div>
  );
}