"use client";

import { useRouter } from "next/navigation";
import { signOut } from "@/lib/auth-client";
import { LogOut } from "lucide-react";

interface SignOutButtonProps {
  className?: string;
}

export default function SignOutButton({ className = "" }: SignOutButtonProps) {
  const router = useRouter();

  return (
    <button 
      onClick={async () => {
        await signOut({
          fetchOptions: {
            onSuccess: () => {
              router.push("/login");
            },
          },
        });
      }}
      className={`w-full flex items-center justify-center gap-2.5 rounded-2xl border border-red-500/20 bg-red-500/5 px-5 py-3.5 text-sm font-medium text-red-600 dark:text-red-400 transition-colors hover:bg-red-500/10 ${className}`}
    >
      <LogOut size={16} />
      Sign Out
    </button>
  );
}
