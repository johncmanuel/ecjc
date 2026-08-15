"use client";

import { useState } from "react";
import Button from "@/components/ui/Button";
import ComposeToolbar from "@/components/compose/ComposeToolbar";
import MediaPreview from "@/components/compose/MediaPreview";

export default function ComposePage() {
  const [text, setText] = useState("");
  const [hasAttachment, setHasAttachment] = useState(false);

  return (
    <div className="flex flex-col min-h-[calc(100vh-73px)]">
      <div className="px-4.5 py-3.5 flex items-center justify-between border-b border-line">
        <span className="text-[13px] text-ink-soft">Cancel</span>
        <span className="font-serif text-[15px] font-medium">New entry</span>
        <Button variant="ink">Post</Button>
      </div>

      <div className="flex-1 p-5 flex flex-col">
        <textarea
          value={text}
          onChange={(e) => setText(e.target.value)}
          placeholder="What's today like?"
          className="font-serif text-[15px] leading-relaxed text-ink placeholder:italic placeholder:text-ink-faint resize-none flex-1 bg-transparent outline-none"
        />

        {hasAttachment && <MediaPreview onRemove={() => setHasAttachment(false)} />}

        <ComposeToolbar onAttach={() => setHasAttachment(true)} />
      </div>
    </div>
  );
}