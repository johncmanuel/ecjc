"use client";

import { useState, useMemo } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import Button from "@/components/ui/Button";
import ComposeToolbar from "@/components/compose/ComposeToolbar";
import MediaPreview from "@/components/compose/MediaPreview";
import { useApi } from "@/hooks/useApi";
import { useGroups } from "@/components/GroupProvider";

export default function ComposePage() {
  const router = useRouter();
  const api = useApi();
  const { activeGroup } = useGroups();
  
  const [text, setText] = useState("");
  const [attachments, setAttachments] = useState<File[]>([]);
  const [isSubmitting, setIsSubmitting] = useState(false);

  // TODO: create shared constants on both client and server for min/max word count and max attachments 
  // so we have a single source of truth
  const minWordCount = 10;
  const maxWordCount = 10000;
  const maxAttachments = 4;

  const wordCount = useMemo(() => {
    return text.split(/[\s\r\n]+/).filter(w => w.length > 0).length;
  }, [text]);

  const isValid = wordCount >= minWordCount && wordCount <= maxWordCount && attachments.length <= maxAttachments && activeGroup;

  const handleFilesSelected = (files: File[]) => {
    setAttachments(prev => {
      const newFiles = [...prev, ...files];
      if (newFiles.length > maxAttachments) {
        alert(`You can only attach up to ${maxAttachments} media files.`);
        return newFiles.slice(0, maxAttachments);
      }
      return newFiles;
    });
  };

  const handleRemoveFile = (index: number) => {
    setAttachments(prev => prev.filter((_, i) => i !== index));
  };

  const handlePost = async () => {
    if (!isValid || !activeGroup || isSubmitting) return;

    setIsSubmitting(true);
    let entryId = null;

    try {
      const entry = await api.createEntry(activeGroup.id!, { textContent: text });
      entryId = entry.id;

      if (attachments.length > 0) {
        for (const file of attachments) {
          await api.uploadMedia(entryId!, { data: file, fileName: file.name });
        }
      }

      router.push("/");
    } catch (err: any) {
      console.error("Failed to post entry", err);
      alert(err.message || "An error occurred while posting your entry.");
      
      // Clean up the entry if media failed to upload
      if (entryId) {
        try {
          await api.deleteEntry(entryId);
        } catch (cleanupErr) {
          console.error("Failed to clean up entry after media upload failure", cleanupErr);
        }
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="flex flex-col min-h-[calc(100vh-73px)]">
      <div className="px-4.5 py-3.5 flex items-center justify-between border-b border-line">
        <Link href="/" className="text-[13px] text-ink-soft hover:text-ink transition-colors">Cancel</Link>
        <span className="font-serif text-[15px] font-medium">New entry</span>
        <Button 
          variant="ink" 
          onClick={handlePost} 
          disabled={!isValid || isSubmitting}
        >
          {isSubmitting ? "Posting..." : "Post"}
        </Button>
      </div>

      <div className="flex-1 p-5 flex flex-col">
        <textarea
          value={text}
          onChange={(e) => setText(e.target.value)}
          placeholder="What's today like?"
          disabled={isSubmitting}
          className="font-serif text-[15px] leading-relaxed text-ink placeholder:italic placeholder:text-ink-faint resize-none flex-1 bg-transparent outline-none"
        />

        <MediaPreview files={attachments} onRemove={handleRemoveFile} />
        
        <div className="flex items-center justify-between mt-auto">
          <ComposeToolbar onFilesSelected={handleFilesSelected} />
          
          <div className="pt-4.5 flex items-center">
            <span className={`text-xs ${wordCount < 10 ? 'text-red-500' : 'text-ink-soft'}`}>
              {wordCount} / 10 words
            </span>
          </div>
        </div>
      </div>
    </div>
  );
}