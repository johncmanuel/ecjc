import  Toggle  from "@/components/ui/Toggle";

type SettingsRowProps = {
  title: string;
  description: string;
  on?: boolean;
  onToggle?: () => void;
};

export default function SettingsRow({ title, description, on = false, onToggle }: SettingsRowProps) {
  return (
    <button
      type="button"
      onClick={onToggle}
      className="w-full bg-card border border-line rounded-2xl px-4 py-3.5 flex items-center justify-between mb-2.5 text-left"
    >
      <div>
        <div className="text-[13px] font-medium">{title}</div>
        <div className="text-[11px] text-ink-faint mt-0.5">{description}</div>
      </div>
      <Toggle on={on} />
    </button>
  );
}