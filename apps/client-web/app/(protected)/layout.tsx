import { AuthGuard } from "@/components/auth-guard";
import { ClientShell } from "@/components/client-shell";

export default function ProtectedLayout({ children }: Readonly<{ children: React.ReactNode }>) {
    return (
        <AuthGuard>
            <ClientShell>{children}</ClientShell>
        </AuthGuard>
    );
}
