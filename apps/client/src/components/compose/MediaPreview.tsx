import { useEffect, useState } from "react";
import { X } from "lucide-react";

export default function MediaPreview({ files, onRemove }: { files: File[], onRemove: (index: number) => void }) {
  const [previews, setPreviews] = useState<string[]>([]);

  useEffect(() => {
    const objectUrls = files.map(file => URL.createObjectURL(file));
    setPreviews(objectUrls);

    // Cleanup object URLs afterwards
    return () => {
      objectUrls.forEach(url => URL.revokeObjectURL(url));
    };
  }, [files]);

  if (files.length === 0) return null;

  return (
    <div className={`mt-4.5 grid gap-2 ${files.length > 1 ? 'grid-cols-2' : 'grid-cols-1'}`}>
      {files.map((file, index) => {
        const isVideo = file.type.startsWith("video/");
        return (
          <div key={file.name + index} className="relative h-36 rounded-2xl bg-paper-deep border border-line overflow-hidden">
            {isVideo ? (
              <video src={previews[index]} className="w-full h-full object-cover" muted loop playsInline autoPlay />
            ) : (
              <img src={previews[index]} alt="Preview" className="w-full h-full object-cover" />
            )}
            <button
              type="button"
              aria-label="Remove attachment"
              onClick={() => onRemove(index)}
              className="absolute top-2 right-2 w-6 h-6 rounded-full bg-black/50 text-white flex items-center justify-center hover:bg-black/70 transition-colors backdrop-blur-sm"
            >
              <X size={14} />
            </button>
          </div>
        );
      })}
    </div>
  );
}