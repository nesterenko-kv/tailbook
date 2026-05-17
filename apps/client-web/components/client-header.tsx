import Link from "next/link";
import { HeartIcon, UserIcon } from "@/components/icons";
import { Button } from "@/components/ui";
import type { NavItem } from "@/lib/landing-config";

export function ClientHeader({
  showNav = false,
  showProfile = false,
  navItems,
  bookButtonLabel = "Записатися",
  salonName = "Tailbook",
}: Readonly<{
  showNav?: boolean;
  showProfile?: boolean;
  navItems?: NavItem[];
  bookButtonLabel?: string;
  salonName?: string;
}>) {
  return (
    <header className="sticky top-0 z-40 border-b border-border bg-background/95 backdrop-blur">
      <div className="container flex h-16 items-center justify-between">
        <Link href="/" className="flex items-center gap-2">
          <HeartIcon className="h-6 w-6 fill-primary text-primary" />
          <span className="text-xl font-semibold">{salonName}</span>
        </Link>
        {showNav && navItems ? (
          <nav className="hidden items-center gap-6 md:flex">
            {navItems.map((item) => (
              <a key={item.href} href={item.href} className="text-sm hover:text-primary">{item.label}</a>
            ))}
          </nav>
        ) : null}
        <div className="flex items-center gap-2">
          {showProfile ? (
            <Link href="/dashboard/profile"><Button variant="ghost" size="icon"><UserIcon className="h-5 w-5" /></Button></Link>
          ) : null}
          <Link href="/booking/services"><Button size="sm">{bookButtonLabel}</Button></Link>
        </div>
      </div>
    </header>
  );
}
