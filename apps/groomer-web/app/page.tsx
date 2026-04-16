const cards = [
    "Separate web shell ready",
    "Tailwind v4 wired",
    "TanStack Query provider wired",
    "API base URL env hook ready for later stages"
];

export default function HomePage() {
    return (
        <main className="mx-auto flex min-h-screen max-w-5xl flex-col gap-8 px-6 py-16">
            <section className="space-y-4">
                <span className="inline-flex rounded-full border border-emerald-500/30 bg-emerald-500/10 px-3 py-1 text-sm text-emerald-300">
                    Stage 0 shell
                </span>
                <h1 className="text-4xl font-semibold tracking-tight">Tailbook Groomer</h1>
                <p className="max-w-2xl text-slate-300">
                    This is the isolated groomer application shell for Tailbook. Real routes, auth-aware layout, and domain screens land in later stages.
                </p>
            </section>

            <section className="grid gap-4 md:grid-cols-2">
                {cards.map((card) => (
                    <article key={card} className="rounded-2xl border border-slate-800 bg-slate-900/60 p-5 shadow-sm">
                        <p className="text-sm text-slate-200">{card}</p>
                    </article>
                ))}
            </section>
        </main>
    );
}
