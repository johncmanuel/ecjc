"use client";

import Link from "next/link";
import { Settings } from "lucide-react";
import ThemeToggle from "@/components/ThemeToggle";
import { GroupHeader } from "@/components/layout/GroupHeader";
import { useScrollDirection } from "@/hooks/useScrollDirection";
import { usePathname } from "next/navigation";

// the overall header that contains the group header and the settings button, and hides on scroll down
export function TopHeader() {
  const scrollDirection = useScrollDirection();
  const pathname = usePathname();

  // bring user to top of page when clicking on the title if already on the home page
  const handleHeaderClick = (e: React.MouseEvent) => {
    if (pathname !== "/") return;

    const target = e.target as HTMLElement;
    if (target.closest('button') || target.closest('a')) {
      return;
    }

    window.scrollTo({ top: 0, behavior: "smooth" });
  };

  return (
    <header 
      onClick={handleHeaderClick}
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
