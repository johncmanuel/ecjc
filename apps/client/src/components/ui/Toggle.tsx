export default function Toggle({ on = false }: { on?: boolean }) {
  return (
    <div
      className={`w-9 h-5 rounded-full relative flex-shrink-0 transition-colors ${
        on ? "bg-thread-a" : "bg-line"
      }`}
    >
      <div
        className={`w-4 h-4 rounded-full bg-white shadow absolute top-0.5 transition-all ${
          on ? "left-[18px]" : "left-[3px]"
        }`}
      />
    </div>
  );
}