"use client";

import Link from "next/link";
import { Settings } from "lucide-react";
import ThemeToggle from "@/components/ThemeToggle";
import { GroupHeader } from "@/components/layout/GroupHeader";
import { useScrollDirection } from "@/hooks/useScrollDirection";

export function TopHeader() {
  const scrollDirection = useScrollDirection();

  return (
    <header 
      className={`sticky top-0 z-50 bg-paper/80 backdrop-blur-md px-6 py-4 border-b border-line flex items-center justify-between transition-transform duration-300 ${
        scrollDirection === "down" ? "-translate-y-full" : "translate-y-0"
      }`}
    >
      <GroupHeader />
      <div className="flex items-center gap-3">
        <ThemeToggle />
        <Link 
          href="/settings" 
          aria-label="Settings"
          title="Settings"
          className="p-2 text-ink-soft hover:text-ink transition-colors rounded-full hover:bg-black/5 dark:hover:bg-white/5"
        >
          <Settings size={20} />
        </Link>
      </div>
    </header>
  );
}
