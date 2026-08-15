"use client";

import { useTheme } from "./ThemeProvider";
import { Sun, Moon, Monitor } from "lucide-react";

export default function ThemeToggle() {
  const { theme, setTheme } = useTheme();

  const cycleTheme = () => {
    if (theme === "system") setTheme("light");
    else if (theme === "light") setTheme("dark");
    else setTheme("system");
  };

  return (
    <button
      onClick={cycleTheme}
      aria-label={`Current theme is ${theme}. Click to change.`}
      title={`Theme: ${theme}`}
      className="p-2 text-ink-soft hover:text-ink transition-colors rounded-full hover:bg-black/5 dark:hover:bg-white/5"
    >
      {theme === "light" && <Sun size={20} />}
      {theme === "dark" && <Moon size={20} />}
      {theme === "system" && <Monitor size={20} />}
    </button>
  );
}
