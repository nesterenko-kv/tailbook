import Link from "next/link";
import { Button, Card, Avatar, AvatarFallback } from "@/components/ui";
import {
  ArrowRightIcon, ClockIcon, HeartIcon, MapPinIcon, PhoneIcon,
  ShieldIcon, StarIcon, InstagramIcon, PawIcon,
} from "@/components/icons";
import { faqItems, groomerProfiles, reviews, serviceTemplates } from "@/lib/display-data";
import { landingConfig } from "@/lib/landing-config";
import type { FeatureCard } from "@/lib/landing-config";
import { ClientHeader } from "@/components/client-header";
import { ServiceCard } from "@/components/service-card";
import { GroomerCard } from "@/components/groomer-card";
import { SimpleAccordion } from "@/components/simple-accordion";
import type { ReactNode } from "react";

const featureIcons: Record<string, ReactNode> = {
  ShieldIcon: <ShieldIcon className="mx-auto mb-3 h-8 w-8 text-primary" />,
  ClockIcon: <ClockIcon className="mx-auto mb-3 h-8 w-8 text-primary" />,
  HeartIcon: <HeartIcon className="mx-auto mb-3 h-8 w-8 text-primary fill-primary" />,
  StarIcon: <StarIcon className="mx-auto mb-3 h-8 w-8 text-primary" />,
};

const cardHover = "transition-shadow duration-300 hover:shadow-lg";

export default function LandingPage() {
  const { salon, hero, features, sections, nav, bookButton } = landingConfig;
  const popularServices = serviceTemplates.filter((s) => s.popular);

  return (
    <div className="min-h-screen bg-background">
      <ClientHeader
        showNav
        navItems={nav}
        bookButtonLabel={bookButton}
        salonName={salon.name}
      />

      <section className="relative overflow-hidden py-16 lg:py-24">
        <div className="absolute inset-0 -z-10 bg-[radial-gradient(ellipse_at_top_right,_var(--tw-gradient-stops))] from-accent via-background to-background" />
        <div className="container grid items-center gap-12 lg:grid-cols-2">
          <div>
            <h1 className="mb-6 text-4xl font-bold leading-tight lg:text-5xl">
              {hero.titleLines.map((line, i) => (
                <span key={i}>{line}{i < hero.titleLines.length - 1 && <br />}</span>
              ))}
            </h1>
            <p className="mb-8 max-w-xl text-lg text-muted-foreground">{hero.subtitle}</p>
            <div className="flex flex-col gap-4 sm:flex-row">
              <Link href="/booking/services">
                <Button size="lg" className="w-full sm:w-auto">
                  {hero.cta} <ArrowRightIcon className="h-4 w-4" />
                </Button>
              </Link>
              <a href={salon.phoneHref}>
                <Button size="lg" variant="outline" className="w-full sm:w-auto">
                  <PhoneIcon className="h-4 w-4" /> {hero.ctaAlt}
                </Button>
              </a>
            </div>
            <div className="mt-8 flex items-center gap-8">
              <div>
                <div className="mb-1 flex items-center gap-1">
                  {Array.from({ length: 5 }).map((_, i) => (
                    <StarIcon key={i} className="h-4 w-4 fill-amber-400 text-amber-400" />
                  ))}
                </div>
                <p className="text-sm text-muted-foreground">{hero.reviewsLabel}</p>
              </div>
              <div>
                <p className="text-2xl font-bold text-primary">{hero.experienceYears}</p>
                <p className="text-sm text-muted-foreground">{hero.experienceLabel}</p>
              </div>
            </div>
          </div>
          <div className="relative">
            <div className="aspect-square overflow-hidden rounded-[28px] shadow-soft">
              <img
                src={hero.image}
                alt={hero.imageAlt}
                className="h-full w-full object-cover"
              />
            </div>
          </div>
        </div>
      </section>

      <section className="bg-accent/30 py-12">
        <div className="container grid gap-6 sm:grid-cols-2 lg:grid-cols-4">
          {features.map((f: FeatureCard) => (
            <Card key={f.title} className={`p-6 text-center ${cardHover}`}>
              {featureIcons[f.icon] ?? featureIcons.ShieldIcon}
              <h3 className="mb-2 font-medium">{f.title}</h3>
              <p className="text-sm text-muted-foreground">{f.description}</p>
            </Card>
          ))}
        </div>
      </section>

      <section id={sections.services.id} className="py-16 lg:py-24">
        <div className="container">
          <div className="mb-12 text-center">
            <h2 className="mb-4 text-3xl font-bold lg:text-4xl">{sections.services.title}</h2>
            <p className="mx-auto max-w-2xl text-lg text-muted-foreground">{sections.services.subtitle}</p>
          </div>
          <div className="mb-8 grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
            {popularServices.map((service) => (
              <ServiceCard key={service.id} service={service} />
            ))}
          </div>
          <div className="text-center">
            <Link href="/booking/services">
              <Button size="lg">{sections.services.cta} <ArrowRightIcon className="h-4 w-4" /></Button>
            </Link>
          </div>
        </div>
      </section>

      <section id={sections.team.id} className="bg-accent/30 py-16 lg:py-24">
        <div className="container">
          <div className="mb-12 text-center">
            <h2 className="mb-4 text-3xl font-bold lg:text-4xl">{sections.team.title}</h2>
            <p className="mx-auto max-w-2xl text-lg text-muted-foreground">{sections.team.subtitle}</p>
          </div>
          <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
            {groomerProfiles.map((groomer) => (
              <GroomerCard key={groomer.id} groomer={groomer} />
            ))}
          </div>
        </div>
      </section>

      <section id={sections.reviews.id} className="py-16 lg:py-24">
        <div className="container">
          <div className="mb-12 text-center">
            <h2 className="mb-4 text-3xl font-bold lg:text-4xl">{sections.reviews.title}</h2>
            <p className="mx-auto max-w-2xl text-lg text-muted-foreground">{sections.reviews.subtitle}</p>
          </div>
          <div className="grid gap-6 sm:grid-cols-2 lg:grid-cols-3">
            {reviews.map((review) => (
              <Card key={review.id} className={`p-6 ${cardHover}`}>
                <div className="mb-4 flex items-center gap-3">
                  <Avatar className="h-11 w-11">
                    <AvatarFallback>{review.author.slice(0, 1)}</AvatarFallback>
                  </Avatar>
                  <div className="flex-1">
                    <h4 className="font-medium">{review.author}</h4>
                    <p className="text-sm text-muted-foreground">{review.pet}</p>
                  </div>
                  <div className="flex items-center gap-1">
                    {Array.from({ length: review.rating }).map((_, i) => (
                      <StarIcon key={i} className="h-4 w-4 fill-amber-400 text-amber-400" />
                    ))}
                  </div>
                </div>
                <p className="text-sm text-muted-foreground">{review.text}</p>
              </Card>
            ))}
          </div>
        </div>
      </section>

      <section className="bg-accent/30 py-16 lg:py-24">
        <div className="container max-w-3xl">
          <h2 className="mb-8 text-center text-3xl font-bold lg:text-4xl">{sections.faq.title}</h2>
          <SimpleAccordion items={faqItems} />
        </div>
      </section>

      <section id={sections.contact.id} className="py-16 lg:py-24">
        <div className="container grid gap-8 lg:grid-cols-2">
          <div>
            <h2 className="mb-4 text-3xl font-bold lg:text-4xl">{sections.contact.title}</h2>
            <p className="mb-8 text-lg text-muted-foreground">{sections.contact.subtitle}</p>
            <div className="space-y-4 text-sm">
              <div className="flex items-start gap-3">
                <MapPinIcon className="mt-0.5 h-5 w-5 text-primary" />
                <div>
                  <p className="font-medium">{sections.contact.addressLabel}</p>
                  <p className="text-muted-foreground">{salon.address}</p>
                </div>
              </div>
              <div className="flex items-start gap-3">
                <PhoneIcon className="mt-0.5 h-5 w-5 text-primary" />
                <div>
                  <p className="font-medium">{sections.contact.phoneLabel}</p>
                  <a href={salon.phoneHref} className="text-muted-foreground hover:text-primary">{salon.phone}</a>
                </div>
              </div>
              <div className="flex items-start gap-3">
                <InstagramIcon className="mt-0.5 h-5 w-5 text-primary" />
                <div>
                  <p className="font-medium">{sections.contact.socialLabel}</p>
                  <a href={salon.instagramHref} className="text-muted-foreground hover:text-primary">{salon.instagram}</a>
                </div>
              </div>
            </div>
          </div>
          <Card className={`p-8 ${cardHover}`}>
            <h3 className="mb-3 text-xl font-medium">{sections.contact.ctaTitle}</h3>
            <p className="mb-6 text-muted-foreground">{sections.contact.ctaDescription}</p>
            <Link href="/booking/services">
              <Button size="lg" className="w-full">
                {sections.contact.ctaButton} <ArrowRightIcon className="h-4 w-4" />
              </Button>
            </Link>
          </Card>
        </div>
      </section>

      <footer className="border-t border-border bg-accent/20 py-10">
        <div className="container">
          <div className="flex flex-col items-center gap-4 text-center sm:flex-row sm:justify-between sm:text-left">
            <div className="flex items-center gap-2">
              <PawIcon className="h-5 w-5 text-primary" />
              <span className="text-sm text-muted-foreground">{sections.footer.tagline}</span>
            </div>
            <p className="text-xs text-muted-foreground">{sections.footer.copyright}</p>
          </div>
        </div>
      </footer>
    </div>
  );
}
