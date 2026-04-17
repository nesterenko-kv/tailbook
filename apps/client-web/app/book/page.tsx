import { BookingRequestFlow } from "@/components/booking-request-flow";

export default function PublicBookingPage() {
    return (
        <main className="mx-auto max-w-6xl px-6 py-10">
            <BookingRequestFlow variant="public" />
        </main>
    );
}
