import { useState, useEffect } from "react";

type ScrollDirection = "up" | "down" | null;

export function useScrollDirection(): ScrollDirection {
  const [scrollDirection, setScrollDirection] = useState<ScrollDirection>(null);
  const minThreshold = -5;
  const maxThreshold = 5;

  useEffect(() => {
    let lastScrollY = window.scrollY;

    const updateScrollDirection = () => {
      const scrollY = window.scrollY;
      const direction = scrollY > lastScrollY ? "down" : "up";

      // update the state if the direction has changed and the scroll distance is significant
      if (direction !== scrollDirection && (scrollY - lastScrollY > maxThreshold || scrollY - lastScrollY < minThreshold)) {
        setScrollDirection(direction);
      }

      lastScrollY = scrollY > 0 ? scrollY : 0;
    };

    window.addEventListener("scroll", updateScrollDirection);
    return () => {
      window.removeEventListener("scroll", updateScrollDirection);
    };
  }, [scrollDirection]);

  return scrollDirection;
}
