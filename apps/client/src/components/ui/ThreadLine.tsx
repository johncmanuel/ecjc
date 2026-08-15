export default function ThreadLine({ author }: { author: "a" | "b" }) {
  return (
    <div
      className={`w-[3px] rounded-full flex-shrink-0 ${
        author === "a" ? "bg-thread-a" : "bg-thread-b"
      }`}
    />
  );
}