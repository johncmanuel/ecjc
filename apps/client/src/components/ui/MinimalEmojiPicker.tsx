// storing a finite set of emojis here for now, might make this configurable
// in the future so this isn't hardcoded
const EMOJIS = [
  "👍", "❤️", "😂", "🔥", "✨", "🎉",
  "👀", "🚀", "🙌", "💯", "👏", "😊",
  "🤔", "😢", "😲", "💀", "💩", "🙏",
  "😎", "😍", "🥳", "🤯", "🥰", "😭"
];

type MinimalEmojiPickerProps = {
  onSelect: (emoji: string) => void;
};

export default function MinimalEmojiPicker({ onSelect }: MinimalEmojiPickerProps) {
  return (
    <div className="bg-paper shadow-xl border border-line rounded-xl p-2 w-64 max-w-[90vw] grid grid-cols-6 gap-1">
      {EMOJIS.map(emoji => (
        <button
          key={emoji}
          onClick={(e) => {
            e.stopPropagation();
            onSelect(emoji);
          }}
          className="flex items-center justify-center text-xl p-2 rounded-lg hover:bg-paper-deep transition-transform hover:scale-110 active:scale-95 cursor-pointer"
        >
          {emoji}
        </button>
      ))}
    </div>
  );
}
