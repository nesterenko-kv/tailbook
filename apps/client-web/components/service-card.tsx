import { Card, Badge, Button } from "@/components/ui";
import { ClockIcon } from "@/components/icons";
import { formatCurrency } from "@/lib/booking-helpers";
import type { DisplayServiceTemplate } from "@/lib/display-data";
import { cn } from "@/lib/cn";

export function ServiceCard({ service, selected = false, onToggle, interactive = false }: Readonly<{ service: DisplayServiceTemplate; selected?: boolean; onToggle?: () => void; interactive?: boolean }>) {
  const body = (
    <>
      {service.image ? <div className="aspect-[4/3] overflow-hidden rounded-t-[inherit]"><img src={service.image} alt={service.name} className="h-full w-full object-cover" /></div> : null}
      <div className="p-5">
        <div className="mb-3 flex items-start justify-between gap-3">
          <div>
            <h3 className="mb-1 font-medium">{service.name}</h3>
            <p className="text-sm text-muted-foreground">{service.description}</p>
          </div>
          {service.popular ? <Badge>Популярно</Badge> : null}
        </div>
        <div className="mb-4 flex items-center justify-between text-sm">
          <span className="font-semibold text-primary">від {formatCurrency(service.priceFrom)}</span>
          <span className="inline-flex items-center gap-1 text-muted-foreground"><ClockIcon className="h-4 w-4" /> {service.duration} хв</span>
        </div>
        {interactive ? <Button variant={selected ? "default" : "outline"} className="w-full" onClick={onToggle}>{selected ? "Обрано" : "Обрати"}</Button> : null}
      </div>
    </>
  );

  return <Card className={cn("overflow-hidden transition hover:-translate-y-0.5 hover:shadow-md", selected && "ring-2 ring-primary shadow-md")}>{body}</Card>;
}
