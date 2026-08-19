import { redirect } from "next/navigation";
import { headers } from "next/headers";
import { auth } from "@/lib/auth";
import { GroupProvider } from "@/components/GroupProvider";
import { TopHeader } from "@/components/layout/TopHeader";

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
      <GroupProvider>
        <TopHeader />
        <div className="flex-1">
          {children}
        </div>
        {modal}
      </GroupProvider>
    </div>
  );
}
