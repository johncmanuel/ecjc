export default function AppLayout({
  children,
  modal,
}: {
  children: React.ReactNode;
  modal: React.ReactNode;
}) {
  return (
    <div className="max-w-md mx-auto min-h-screen bg-paper relative">
      <header className="px-6 py-4 border-b border-line">
        <h1 className="font-serif text-xl font-medium text-ink dark:text-ink-faint">
          ecjc 
        </h1>
        {/* TODO: Replace with actual user names from database */}
        <p className="text-[11px] text-ink-faint mt-0.5">You &amp; Emmanuel</p> 
      </header>
      {children}
      {modal}
    </div>
  );
}
