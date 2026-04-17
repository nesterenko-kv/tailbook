import type { ReactNode } from "react";
import { Button } from "@/components/ui";

export function BottomMobileCTA({ children, disabled, onClick }: Readonly<{ children: ReactNode; disabled?: boolean; onClick?: () => void }>) {
  return (
    <div className="fixed inset-x-0 bottom-0 z-40 border-t border-border bg-background/95 p-4 backdrop-blur lg:hidden">
      <Button onClick={onClick} disabled={disabled} size="lg" className="w-full">{children}</Button>
    </div>
  );
}
