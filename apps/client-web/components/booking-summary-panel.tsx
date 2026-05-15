import { Card, Separator } from "@/components/ui";
import { CalendarIcon, ClockIcon, PawIcon, UserIcon } from "@/components/icons";
import { formatCurrency, formatDateLong } from "@/lib/booking-helpers";
import type { BookingState } from "@/lib/booking-helpers";
import { serviceTemplates } from "@/lib/display-data";

export function BookingSummaryPanel({ booking }: Readonly<{ booking: BookingState }>) {
  const selectedServices = serviceTemplates.filter((item) => booking.selectedTemplateIds.includes(item.id));
  const totalPrice = booking.quote?.totalAmount ?? selectedServices.reduce((sum, service) => sum + service.priceFrom, 0);
  const totalDuration = booking.quote?.reservedMinutes ?? selectedServices.reduce((sum, service) => sum + service.duration, 0);

  return (
    <Card className="sticky top-24 p-6">
      <h3 className="mb-4 text-lg font-medium">Ваш запис</h3>
      <div className="space-y-4 text-sm">
        <div>
          <div className="mb-2 flex items-center gap-2 font-medium"><PawIcon className="h-4 w-4 text-primary" /> Послуги</div>
          {selectedServices.length > 0 ? (
            <div className="space-y-2 text-muted-foreground">
              {selectedServices.map((service) => <div key={service.id} className="flex justify-between gap-4"><span>{service.name}</span><span>від {formatCurrency(service.priceFrom)}</span></div>)}
            </div>
          ) : <p className="text-muted-foreground">Ще не обрано</p>}
        </div>
        <Separator />
        <div>
          <div className="mb-2 flex items-center gap-2 font-medium"><UserIcon className="h-4 w-4 text-primary" /> Вихованець</div>
          {booking.pet ? <div className="text-muted-foreground"><p className="font-medium text-foreground">{booking.pet.name || "Без імені"}</p><p>{booking.pet.breedName ?? booking.pet.animalTypeCode ?? "Новий профіль"}</p></div> : <p className="text-muted-foreground">Ще не додано</p>}
        </div>
        <Separator />
        <div>
          <div className="mb-2 flex items-center gap-2 font-medium"><CalendarIcon className="h-4 w-4 text-primary" /> Дата та час</div>
          {booking.preferredDate && booking.preferredSlot ? <div className="text-muted-foreground"><p>{formatDateLong(booking.preferredDate)}</p><p>{booking.preferredSlot}</p></div> : <p className="text-muted-foreground">Оберіть на наступному кроці</p>}
        </div>
        <Separator />
        <div className="flex justify-between gap-4 text-base"><span className="font-medium">Орієнтовна вартість</span><span className="font-bold text-primary">від {formatCurrency(totalPrice)}</span></div>
        <div className="flex items-center gap-2 text-xs text-muted-foreground"><ClockIcon className="h-4 w-4" /> Орієнтовна тривалість: {totalDuration} хв</div>
      </div>
    </Card>
  );
}
