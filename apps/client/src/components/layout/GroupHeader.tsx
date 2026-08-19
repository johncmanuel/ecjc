"use client";

import { useState, useRef, useEffect } from "react";
import { useGroups } from "@/components/GroupProvider";
import { ChevronDown, Check } from "lucide-react";
import Link from "next/link";
import { usePathname } from "next/navigation";

// represents the left side of the header, which includes the title and the active group name
export function GroupHeader() {
  const { groups, activeGroup, setActiveGroupId, isLoading } = useGroups();
  const [isOpen, setIsOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const pathname = usePathname();

  // bring user to top of page when clicking on the title if already on the home page
  const handleTitleClick = (e: React.MouseEvent) => {
    if (pathname === "/") {
      e.preventDefault();
      window.scrollTo({ top: 0, behavior: "smooth" });
    }
  };

  // close dropdown when clicking/pressing outside of the header
  useEffect(() => {
    function handleClickOutside(event: MouseEvent) {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    }
    document.addEventListener("mousedown", handleClickOutside);
    return () => document.removeEventListener("mousedown", handleClickOutside);
  }, []);

  if (isLoading) {
    return (
      <div>
        <h1 className="font-serif text-xl font-medium text-ink dark:text-ink-faint">
          <Link href="/" onClick={handleTitleClick} className="hover:text-ink transition-colors">
            ecjc
          </Link>
        </h1>
        <div className="h-4 w-24 bg-line animate-pulse mt-0.5 rounded"></div>
      </div>
    );
  }

  if (!activeGroup || groups.length === 0) {
    return (
      <div>
        <h1 className="font-serif text-xl font-medium text-ink dark:text-ink-faint">
          <Link href="/" onClick={handleTitleClick} className="hover:text-ink transition-colors">
          ecjc
          </Link>
        </h1>
        <p className="text-[11px] text-ink-faint mt-0.5">Welcome</p>
      </div>
    );
  }

  const getPartnerName = (group: typeof activeGroup) => {
     if (group.members!.length === 1) return "Just You";
     return group.members!.map(m => m.firstName || "Someone").join(" & ");
  }

  const activeTitle = getPartnerName(activeGroup);

  return (
    <div className="relative" ref={dropdownRef}>
      <h1 className="font-serif text-xl font-medium text-ink dark:text-ink-faint">
        <Link href="/" onClick={handleTitleClick} className="hover:text-ink transition-colors">
          ecjc
        </Link>
      </h1>
      
      {groups.length > 1 ? (
        <button 
          onClick={() => setIsOpen(!isOpen)}
          className="flex items-center gap-1 mt-0.5 text-[11px] text-ink-faint hover:text-ink transition-colors"
        >
          {activeTitle}
          <ChevronDown size={12} className={`transition-transform ${isOpen ? "rotate-180" : ""}`} />
        </button>
      ) : (
        <p className="text-[11px] text-ink-faint mt-0.5">{activeTitle}</p>
      )}

      {isOpen && groups.length > 1 && (
        <div className="absolute top-full left-0 mt-2 w-48 bg-paper border border-line rounded-lg shadow-lg overflow-hidden z-50">
          <div className="py-1">
            {groups.map(group => (
              <button
                key={group.id}
                onClick={() => {
                  setActiveGroupId(group.id!);
                  setIsOpen(false);
                }}
                className={`w-full text-left px-4 py-2 text-sm flex items-center justify-between hover:bg-black/5 dark:hover:bg-white/5 transition-colors ${
                  group.id === activeGroup.id ? "text-ink font-medium" : "text-ink-soft"
                }`}
              >
                <span className="truncate pr-4">{getPartnerName(group)}</span>
                {group.id === activeGroup.id && <Check size={14} className="text-ink" />}
              </button>
            ))}
          </div>
        </div>
      )}
    </div>
  );
}
