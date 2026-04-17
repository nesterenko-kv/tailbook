import { cn } from "@/lib/cn";

export function SlotButton({ time, available, selected, suggested, onSelect }: Readonly<{ time: string; available: boolean; selected?: boolean; suggested?: boolean; onSelect: () => void }>) {
  return (
    <button type="button" disabled={!available} onClick={onSelect} className={cn("relative rounded-lg border px-3 py-3 text-sm font-medium transition", !available && "cursor-not-allowed border-border bg-secondary text-muted-foreground opacity-60", available && !selected && "border-border bg-card hover:border-primary hover:bg-accent", selected && "border-primary bg-primary text-primary-foreground")}> 
      {time}
      {suggested && available ? <span className="absolute -top-2 right-2 rounded-full bg-accent px-2 py-0.5 text-[10px] text-foreground">Рекомендовано</span> : null}
    </button>
  );
}
