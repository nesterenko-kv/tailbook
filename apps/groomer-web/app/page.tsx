import Link from "next/link";

export default function HomePage() {
    return (
        <main className="mx-auto flex min-h-screen max-w-5xl flex-col gap-10 px-6 py-16">
            <section className="space-y-4">
                <span className="inline-flex rounded-full border border-emerald-500/30 bg-emerald-500/10 px-3 py-1 text-sm text-emerald-300">
                    Stage 8 groomer-safe web
                </span>
                <h1 className="text-4xl font-semibold tracking-tight">Tailbook Groomer</h1>
                <p className="max-w-3xl text-slate-300">
                    Groomer-safe appointment and visit execution screens. This shell intentionally reads only the dedicated
                    groomer API surface and never consumes CRM contact data.
                </p>
            </section>

            <section className="grid gap-4 md:grid-cols-2">
                <Link href="/login" className="rounded-2xl border border-slate-800 bg-slate-900/60 p-6 transition hover:border-emerald-500/40 hover:bg-slate-900">
                    <h2 className="text-lg font-medium">Login</h2>
                    <p className="mt-2 text-sm text-slate-300">Authenticate with a groomer account and store the JWT locally in the browser.</p>
                </Link>
                <Link href="/appointments" className="rounded-2xl border border-slate-800 bg-slate-900/60 p-6 transition hover:border-emerald-500/40 hover:bg-slate-900">
                    <h2 className="text-lg font-medium">My appointments</h2>
                    <p className="mt-2 text-sm text-slate-300">Review assigned appointments, open the groomer-safe detail view, start visits, and execute procedures.</p>
                </Link>
            </section>
        </main>
    );
}
