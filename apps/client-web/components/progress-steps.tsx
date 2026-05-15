import { CheckIcon } from "@/components/icons";
import { cn } from "@/lib/cn";

export function ProgressSteps({ steps }: Readonly<{ steps: { label: string; completed?: boolean; active?: boolean }[] }>) {
  return (
    <div className="mx-auto mb-8 flex max-w-3xl items-center justify-between gap-2 overflow-x-auto pb-2">
      {steps.map((step, index) => (
        <div key={step.label} className="flex min-w-0 flex-1 items-center gap-2">
          <div className={cn("flex h-8 w-8 shrink-0 items-center justify-center rounded-full border text-sm font-medium", step.completed ? "border-primary bg-primary text-primary-foreground" : step.active ? "border-primary bg-accent text-foreground" : "border-border bg-card text-muted-foreground")}>
            {step.completed ? <CheckIcon className="h-4 w-4" /> : index + 1}
          </div>
          <span className={cn("truncate text-sm", step.active ? "text-foreground" : "text-muted-foreground")}>{step.label}</span>
          {index < steps.length - 1 ? <div className="hidden h-px flex-1 bg-border md:block" /> : null}
        </div>
      ))}
    </div>
  );
}
