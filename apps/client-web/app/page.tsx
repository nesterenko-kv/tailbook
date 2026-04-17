import Link from "next/link";
import { Button, Card, Avatar, AvatarFallback } from "@/components/ui";
import { ArrowRightIcon, ClockIcon, HeartIcon, MapPinIcon, PhoneIcon, ShieldIcon, StarIcon } from "@/components/icons";
import { faqItems, groomerProfiles, reviews, salonInfo, serviceTemplates } from "@/lib/display-data";
import { ClientHeader } from "@/components/client-header";
import { ServiceCard } from "@/components/service-card";
import { GroomerCard } from "@/components/groomer-card";
import { SimpleAccordion } from "@/components/simple-accordion";

export default function LandingPage() {
  const popularServices = serviceTemplates.filter((service) => service.popular);

  return (
    <div className="min-h-screen bg-background">
      <ClientHeader showNav />

      <section className="relative overflow-hidden py-16 lg:py-24">
        <div className="container grid items-center gap-12 lg:grid-cols-2">
          <div>
            <h1 className="mb-6 text-4xl font-bold leading-tight lg:text-5xl">Професійний грумінг<br />для ваших улюбленців</h1>
            <p className="mb-8 max-w-xl text-lg text-muted-foreground">Досвідчені майстри, сучасне обладнання та індивідуальний підхід до кожного вихованця. Запишіться онлайн за 1 хвилину.</p>
            <div className="flex flex-col gap-4 sm:flex-row">
              <Link href="/booking/services"><Button size="lg" className="w-full sm:w-auto">Записатися онлайн <ArrowRightIcon className="h-4 w-4" /></Button></Link>
              <a href={salonInfo.phoneHref}><Button size="lg" variant="outline" className="w-full sm:w-auto"><PhoneIcon className="h-4 w-4" /> Подзвонити</Button></a>
            </div>
            <div className="mt-8 flex items-center gap-8">
              <div>
                <div className="mb-1 flex items-center gap-1">{Array.from({ length: 5 }).map((_, i) => <StarIcon key={i} className="h-4 w-4 fill-amber-400 text-amber-400" />)}</div>
                <p className="text-sm text-muted-foreground">500+ відгуків</p>
              </div>
              <div>
                <p className="text-2xl font-bold text-primary">8+</p>
                <p className="text-sm text-muted-foreground">років досвіду</p>
              </div>
            </div>
          </div>
          <div className="relative">
            <div className="aspect-square overflow-hidden rounded-[28px] shadow-soft">
              <img src="https://images.unsplash.com/photo-1713996240147-7a2f77d2871b?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=1080" alt="Професійний грумінг" className="h-full w-full object-cover" />
            </div>
          </div>
        </div>
      </section>

      <section className="bg-accent/30 py-12">
        <div className="container grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
          <Card className="p-6 text-center"><ShieldIcon className="mx-auto mb-3 h-8 w-8 text-primary" /><h3 className="mb-2 font-medium">Безпечно</h3><p className="text-sm text-muted-foreground">Сертифіковані майстри та якісна косметика</p></Card>
          <Card className="p-6 text-center"><ClockIcon className="mx-auto mb-3 h-8 w-8 text-primary" /><h3 className="mb-2 font-medium">Швидко</h3><p className="text-sm text-muted-foreground">Зручний онлайн-запис без дзвінків</p></Card>
          <Card className="p-6 text-center"><HeartIcon className="mx-auto mb-3 h-8 w-8 text-primary fill-primary" /><h3 className="mb-2 font-medium">З любов'ю</h3><p className="text-sm text-muted-foreground">Індивідуальний підхід до кожної тваринки</p></Card>
          <Card className="p-6 text-center"><StarIcon className="mx-auto mb-3 h-8 w-8 text-primary" /><h3 className="mb-2 font-medium">Якісно</h3><p className="text-sm text-muted-foreground">Професійне обладнання та досвід</p></Card>
        </div>
      </section>

      <section id="services" className="py-16 lg:py-24">
        <div className="container">
          <div className="mb-12 text-center"><h2 className="mb-4 text-3xl font-bold lg:text-4xl">Популярні послуги</h2><p className="mx-auto max-w-2xl text-lg text-muted-foreground">Оберіть послугу та запишіться онлайн за кілька хвилин</p></div>
          <div className="mb-8 grid gap-6 sm:grid-cols-2 lg:grid-cols-3">{popularServices.map((service) => <ServiceCard key={service.id} service={service} />)}</div>
          <div className="text-center"><Link href="/booking/services"><Button size="lg">Переглянути всі послуги <ArrowRightIcon className="h-4 w-4" /></Button></Link></div>
        </div>
      </section>

      <section id="team" className="bg-accent/30 py-16 lg:py-24">
        <div className="container">
          <div className="mb-12 text-center"><h2 className="mb-4 text-3xl font-bold lg:text-4xl">Наша команда</h2><p className="mx-auto max-w-2xl text-lg text-muted-foreground">Досвідчені грумери з міжнародними сертифікатами</p></div>
          <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">{groomerProfiles.map((groomer) => <GroomerCard key={groomer.id} groomer={groomer} />)}</div>
        </div>
      </section>

      <section id="reviews" className="py-16 lg:py-24">
        <div className="container">
          <div className="mb-12 text-center"><h2 className="mb-4 text-3xl font-bold lg:text-4xl">Що кажуть наші клієнти</h2><p className="mx-auto max-w-2xl text-lg text-muted-foreground">Щасливі вихованці та задоволені власники</p></div>
          <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
            {reviews.map((review) => (
              <Card key={review.id} className="p-6">
                <div className="mb-4 flex items-center gap-3">
                  <Avatar className="h-11 w-11"><AvatarFallback>{review.author.slice(0, 1)}</AvatarFallback></Avatar>
                  <div className="flex-1"><h4 className="font-medium">{review.author}</h4><p className="text-sm text-muted-foreground">{review.pet}</p></div>
                  <div className="flex items-center gap-1">{Array.from({ length: review.rating }).map((_, i) => <StarIcon key={i} className="h-4 w-4 fill-amber-400 text-amber-400" />)}</div>
                </div>
                <p className="text-sm text-muted-foreground">{review.text}</p>
              </Card>
            ))}
          </div>
        </div>
      </section>

      <section className="bg-accent/30 py-16 lg:py-24">
        <div className="container max-w-3xl">
          <h2 className="mb-8 text-center text-3xl font-bold lg:text-4xl">Часті питання</h2>
          <SimpleAccordion items={faqItems} />
        </div>
      </section>

      <section id="contact" className="py-16 lg:py-24">
        <div className="container grid gap-8 lg:grid-cols-2">
          <div>
            <h2 className="mb-4 text-3xl font-bold lg:text-4xl">Контакти</h2>
            <p className="mb-8 text-lg text-muted-foreground">Ми працюємо щодня та швидко підтверджуємо нові booking requests.</p>
            <div className="space-y-4 text-sm">
              <div className="flex items-start gap-3"><MapPinIcon className="mt-0.5 h-5 w-5 text-primary" /><div><p className="font-medium">Адреса</p><p className="text-muted-foreground">{salonInfo.address}</p></div></div>
              <div className="flex items-start gap-3"><PhoneIcon className="mt-0.5 h-5 w-5 text-primary" /><div><p className="font-medium">Телефон</p><a href={salonInfo.phoneHref} className="text-muted-foreground hover:text-primary">{salonInfo.phone}</a></div></div>
              <div className="flex items-start gap-3"><MapPinIcon className="mt-0.5 h-5 w-5 text-primary" /><div><p className="font-medium">Instagram</p><a href={salonInfo.instagramHref} className="text-muted-foreground hover:text-primary">{salonInfo.instagram}</a></div></div>
            </div>
          </div>
          <Card className="p-8"><h3 className="mb-3 text-xl font-medium">Готові записатися?</h3><p className="mb-6 text-muted-foreground">Виберіть послугу, розкажіть про вихованця та залиште контакти. Ми підтвердимо запис після перевірки слоту.</p><Link href="/booking/services"><Button size="lg" className="w-full">Почати запис <ArrowRightIcon className="h-4 w-4" /></Button></Link></Card>
        </div>
      </section>
    </div>
  );
}
