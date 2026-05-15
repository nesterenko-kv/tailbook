import Link from "next/link";
import { HeartIcon, UserIcon } from "@/components/icons";
import { Button } from "@/components/ui";

export function ClientHeader({ showNav = false, showProfile = false }: Readonly<{ showNav?: boolean; showProfile?: boolean }>) {
  return (
    <header className="sticky top-0 z-40 border-b border-border bg-background/95 backdrop-blur">
      <div className="container flex h-16 items-center justify-between">
        <Link href="/" className="flex items-center gap-2">
          <HeartIcon className="h-6 w-6 fill-primary text-primary" />
          <span className="text-xl font-semibold">Tailbook</span>
        </Link>
        {showNav ? (
          <nav className="hidden items-center gap-6 md:flex">
            <a href="#services" className="text-sm hover:text-primary">Послуги</a>
            <a href="#team" className="text-sm hover:text-primary">Команда</a>
            <a href="#reviews" className="text-sm hover:text-primary">Відгуки</a>
            <a href="#contact" className="text-sm hover:text-primary">Контакти</a>
          </nav>
        ) : null}
        <div className="flex items-center gap-2">
          {showProfile ? (
            <Link href="/dashboard/profile"><Button variant="ghost" size="icon"><UserIcon className="h-5 w-5" /></Button></Link>
          ) : null}
          <Link href="/booking/services"><Button size="sm">Записатися</Button></Link>
        </div>
      </div>
    </header>
  );
}
