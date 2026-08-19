import { useRef } from "react";
import { Image as ImageIcon } from "lucide-react";

export default function ComposeToolbar({ onFilesSelected }: { onFilesSelected: (files: File[]) => void }) {
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files) {
      onFilesSelected(Array.from(e.target.files));
    }

    // Clear the input so the same files can be selected again if needed
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  return (
    <div className="mt-auto pt-4.5 border-t border-line flex gap-5">
      <input
        type="file"
        ref={fileInputRef}
        onChange={handleFileChange}
        className="hidden"
        multiple
        accept="image/*,video/*"
      />
      <button
        type="button"
        aria-label="Attach media"
        onClick={() => fileInputRef.current?.click()}
        className="h-9 px-3 rounded-[10px] bg-card border border-line flex items-center gap-2 text-ink-soft text-sm hover:bg-paper transition-colors"
      >
        <ImageIcon size={16} />
        Attach Media
      </button>
    </div>
  );
}