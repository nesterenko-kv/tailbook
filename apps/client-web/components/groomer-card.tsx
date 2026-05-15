import { Avatar, AvatarFallback, AvatarImage, Badge, Card } from "@/components/ui";
import type { DisplayGroomer } from "@/lib/display-data";
import { cn } from "@/lib/cn";

export function GroomerCard({ groomer, compact = false, selected = false, onSelect }: Readonly<{ groomer: DisplayGroomer; compact?: boolean; selected?: boolean; onSelect?: () => void }>) {
  return (
    <Card className={cn("p-4 transition hover:shadow-md", onSelect && "cursor-pointer", selected && "ring-2 ring-primary shadow-md")} onClick={onSelect}>
      <div className={cn("flex gap-4", compact ? "items-center" : "items-start")}>
        <Avatar className={compact ? "h-14 w-14" : "h-16 w-16"}>
          {groomer.avatar ? <AvatarImage src={groomer.avatar} alt={groomer.name} /> : null}
          <AvatarFallback>{groomer.name.slice(0, 1)}</AvatarFallback>
        </Avatar>
        <div className="min-w-0 flex-1">
          <div className="mb-1 flex items-start justify-between gap-2">
            <h3 className="font-medium">{groomer.name}</h3>
            {selected ? <Badge>Обрано</Badge> : null}
          </div>
          <p className="mb-2 text-sm text-muted-foreground">{groomer.experience}</p>
          <div className="mb-2 flex flex-wrap gap-2">
            {groomer.specialties.slice(0, compact ? 2 : 3).map((item) => <Badge key={item} variant="secondary">{item}</Badge>)}
          </div>
          {!compact ? <p className="text-sm leading-6 text-muted-foreground">{groomer.bio}</p> : null}
        </div>
      </div>
    </Card>
  );
}
