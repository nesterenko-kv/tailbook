"use client";

import { useState } from "react";
import { Card } from "@/components/ui";
import { ChevronDownIcon } from "@/components/icons";
import { cn } from "@/lib/cn";

export function SimpleAccordion({ items }: Readonly<{ items: { question: string; answer: string }[] }>) {
  const [openIndex, setOpenIndex] = useState<number | null>(0);

  return (
    <div className="space-y-4">
      {items.map((item, index) => {
        const open = openIndex == index;
        return (
          <Card key={item.question} className="overflow-hidden px-6 py-2">
            <button type="button" className="flex w-full items-center justify-between gap-4 py-4 text-left" onClick={() => setOpenIndex(open ? null : index)}>
              <span className="font-medium">{item.question}</span>
              <ChevronDownIcon className={cn("h-4 w-4 text-muted-foreground transition-transform", open && "rotate-180")} />
            </button>
            {open ? <div className="pb-4 text-sm leading-6 text-muted-foreground">{item.answer}</div> : null}
          </Card>
        );
      })}
    </div>
  );
}
