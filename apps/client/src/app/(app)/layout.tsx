import { redirect } from "next/navigation";
import { headers } from "next/headers";
import Link from "next/link";
import { Settings } from "lucide-react";
import ThemeToggle from "@/components/ThemeToggle";
import { auth } from "@/lib/auth";

export default async function AppLayout({
  children,
  modal,
}: {
  children: React.ReactNode;
  modal: React.ReactNode;
}) {
  const session = await auth.api.getSession({
    headers: await headers(),
  });

  if (!session) {
    redirect("/login");
  }

  return (
    <div className="max-w-md mx-auto min-h-screen bg-paper relative flex flex-col">
      <header className="px-6 py-4 border-b border-line flex items-center justify-between">
        <div>
          <h1 className="font-serif text-xl font-medium text-ink dark:text-ink-faint">
            ecjc 
          </h1>
          {/* TODO: Replace with actual user names from database */}
          <p className="text-[11px] text-ink-faint mt-0.5">You &amp; Emmanuel</p> 
        </div>
        <div className="flex items-center gap-3">
          <ThemeToggle />
          <Link 
            href="/settings" 
            aria-label="Settings"
            className="p-2 text-ink-soft hover:text-ink transition-colors rounded-full hover:bg-black/5 dark:hover:bg-white/5"
          >
            <Settings size={20} />
          </Link>
        </div>
      </header>
      <div className="flex-1">
        {children}
      </div>
      {modal}
    </div>
  );
}
