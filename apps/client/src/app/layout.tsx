import { Fraunces, Work_Sans } from "next/font/google";
import "./globals.css";

const fraunces = Fraunces({
  subsets: ["latin"],
  variable: "--font-fraunces",
  style: ["normal", "italic"],
  weight: ["400", "500", "600"],
});

const workSans = Work_Sans({
  subsets: ["latin"],
  variable: "--font-work-sans",
  weight: ["400", "500", "600"],
});

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    // add/remove "dark" here (or via next-themes) to switch palettes
    <html lang="en" className={`${fraunces.variable} ${workSans.variable}`}>
      <body className="font-sans bg-paper text-ink min-h-screen">
        {children}
      </body>
    </html>
  );
}
