export type FeatureCard = {
  icon: string;
  title: string;
  description: string;
};

export type NavItem = {
  href: string;
  label: string;
};

export type LandingConfig = {
  salon: {
    name: string;
    phone: string;
    phoneHref: string;
    instagram: string;
    instagramHref: string;
    address: string;
  };
  hero: {
    titleLines: [string, string];
    subtitle: string;
    cta: string;
    ctaAlt: string;
    reviewsLabel: string;
    experienceYears: string;
    experienceLabel: string;
    image: string;
    imageAlt: string;
  };
  features: FeatureCard[];
  sections: {
    services: { id: string; title: string; subtitle: string; cta: string };
    team: { id: string; title: string; subtitle: string };
    reviews: { id: string; title: string; subtitle: string };
    faq: { id: string; title: string };
    contact: {
      id: string;
      title: string;
      subtitle: string;
      addressLabel: string;
      phoneLabel: string;
      socialLabel: string;
      ctaTitle: string;
      ctaDescription: string;
      ctaButton: string;
    };
    footer: {
      tagline: string;
      copyright: string;
    };
  };
  nav: NavItem[];
  bookButton: string;
};

export const landingConfig: LandingConfig = {
  salon: {
    name: process.env.NEXT_PUBLIC_SALON_NAME ?? "Doggy Groom",
    phone: process.env.NEXT_PUBLIC_SALON_PHONE ?? "+380 67 614 16 70",
    phoneHref: process.env.NEXT_PUBLIC_SALON_PHONE_HREF ?? "tel:+380676141670",
    instagram: process.env.NEXT_PUBLIC_SALON_INSTAGRAM ?? "@doggy_groom",
    instagramHref:
      process.env.NEXT_PUBLIC_SALON_INSTAGRAM_HREF ??
      "https://instagram.com/doggy_groom",
    address: process.env.NEXT_PUBLIC_SALON_ADDRESS ?? "бульвар Шевченка 10а, Запоріжжя",
  },
  hero: {
    titleLines: ["Грумінг салон", "Doggy Groom"],
    subtitle:
      "Досвідчені майстри, сучасне обладнання та індивідуальний підхід до кожного вихованця. Запоріжжя, бульвар Шевченка 10а.",
    cta: "Записатися онлайн",
    ctaAlt: "Подзвонити",
    reviewsLabel: "500+ відгуків",
    experienceYears: "8+",
    experienceLabel: "років досвіду",
    image:
      "https://images.unsplash.com/photo-1713996240147-7a2f77d2871b?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=1080",
    imageAlt: "Грумінг салон Doggy Groom",
  },
  features: [
    {
      icon: "ShieldIcon",
      title: "Безпечно",
      description: "Сертифіковані майстри та якісна косметика",
    },
    {
      icon: "ClockIcon",
      title: "Швидко",
      description: "Зручний онлайн-запис без дзвінків",
    },
    {
      icon: "HeartIcon",
      title: "З любов'ю",
      description: "Індивідуальний підхід до кожної тваринки",
    },
    {
      icon: "StarIcon",
      title: "Якісно",
      description: "Професійне обладнання та досвід",
    },
  ],
  sections: {
    services: {
      id: "services",
      title: "Популярні послуги",
      subtitle: "Оберіть послугу та запишіться онлайн за кілька хвилин",
      cta: "Переглянути всі послуги",
    },
    team: {
      id: "team",
      title: "Наша команда",
      subtitle: "Досвідчені грумери з міжнародними сертифікатами",
    },
    reviews: {
      id: "reviews",
      title: "Що кажуть наші клієнти",
      subtitle: "Щасливі вихованці та задоволені власники",
    },
    faq: {
      id: "faq",
      title: "Часті питання",
    },
    contact: {
      id: "contact",
      title: "Контакти",
      subtitle:
        "Ми працюємо щодня та швидко підтверджуємо нові booking requests.",
      addressLabel: "Адреса",
      phoneLabel: "Телефон",
      socialLabel: "Instagram",
      ctaTitle: "Готові записатися?",
      ctaDescription:
        "Виберіть послугу, розкажіть про вихованця та залиште контакти. Ми підтвердимо запис після перевірки слоту.",
      ctaButton: "Почати запис",
    },
    footer: {
      tagline: "Грумінг салон Doggy Groom — професійний догляд для ваших улюбленців",
      copyright: "Doggy Groom. Усі права захищені.",
    },
  },
  nav: [
    { href: "#services", label: "Послуги" },
    { href: "#team", label: "Команда" },
    { href: "#reviews", label: "Відгуки" },
    { href: "#contact", label: "Контакти" },
  ],
  bookButton: "Записатися",
};
