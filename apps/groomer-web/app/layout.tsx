import "./globals.css";
import type { Metadata } from "next";
import { Providers } from "./providers";

export const metadata: Metadata = {
    title: "Tailbook Groomer",
    description: "Tailbook groomer application shell"
};

export default function RootLayout({ children }: Readonly<{ children: React.ReactNode }>) {
    return (
        <html lang="en">
            <body className="min-h-screen bg-slate-950 text-slate-50 antialiased">
                <Providers>{children}</Providers>
            </body>
        </html>
    );
}
