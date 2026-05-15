export type DisplayServiceTemplate = {
  id: string;
  name: string;
  description: string;
  category: "dog" | "cat" | "extra";
  priceFrom: number;
  duration: number;
  popular?: boolean;
  image?: string;
  keywords: string[];
};

export type DisplayGroomer = {
  id: string;
  name: string;
  avatar?: string;
  specialties: string[];
  experience: string;
  bio: string;
};

export const serviceTemplates: DisplayServiceTemplate[] = [
  {
    id: "service-dog-complex",
    name: "Комплекс для собак",
    description: "Повний догляд: миття, стрижка, обробка кігтів, чистка вух",
    category: "dog",
    priceFrom: 800,
    duration: 120,
    popular: true,
    image: "https://images.unsplash.com/photo-1767381392938-c95d24cd5873?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=1080",
    keywords: ["комплекс", "повний", "full", "groom", "dog", "собак"]
  },
  {
    id: "service-model-cut",
    name: "Модельна стрижка",
    description: "Ексклюзивна стрижка за породою або на вибір",
    category: "dog",
    priceFrom: 1200,
    duration: 180,
    popular: true,
    image: "https://images.unsplash.com/photo-1651273427958-bf78352e39be?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=1080",
    keywords: ["стриж", "haircut", "модель", "model"]
  },
  {
    id: "service-express-wash",
    name: "Експрес-миття",
    description: "Швидке миття професійними засобами та сушка",
    category: "dog",
    priceFrom: 400,
    duration: 60,
    image: "https://images.unsplash.com/photo-1641290378771-563a05ed1e66?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=1080",
    keywords: ["мит", "wash", "bath", "експрес"]
  },
  {
    id: "service-cat-complex",
    name: "Комплекс для котів",
    description: "Повний догляд для котів: миття, стрижка, кігті",
    category: "cat",
    priceFrom: 700,
    duration: 90,
    popular: true,
    image: "https://images.unsplash.com/photo-1772643337885-e1cfbaec11d2?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=1080",
    keywords: ["cat", "кот", "комплекс", "повний"]
  },
  {
    id: "service-cat-hygiene",
    name: "Гігієнічна стрижка кота",
    description: "Стрижка без наркозу в комфортних умовах",
    category: "cat",
    priceFrom: 600,
    duration: 75,
    keywords: ["cat", "кот", "стриж", "гігієн"]
  },
  {
    id: "service-paws-spa",
    name: "SPA для лап",
    description: "Зволоження подушечок лап і догляд за кігтями",
    category: "extra",
    priceFrom: 200,
    duration: 30,
    keywords: ["spa", "лап", "paw"]
  },
  {
    id: "service-teeth",
    name: "Чистка зубів",
    description: "Професійний догляд за ротовою порожниною",
    category: "extra",
    priceFrom: 350,
    duration: 45,
    keywords: ["зуб", "teeth", "tooth"]
  },
  {
    id: "service-trimming",
    name: "Тримінг",
    description: "Видалення відмерлої шерсті для жорсткошерстих порід",
    category: "dog",
    priceFrom: 900,
    duration: 120,
    keywords: ["trim", "трим", "шерст"]
  }
];

export const groomerProfiles: DisplayGroomer[] = [
  {
    id: "groomer-1",
    name: "Олена Коваленко",
    avatar: "https://images.unsplash.com/photo-1739551757773-bd3ea56e9330?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=1080",
    specialties: ["Модельні стрижки", "Пудель", "Йоркширський тер’єр"],
    experience: "8 років досвіду",
    bio: "Сертифікований грумер з міжнародними дипломами. Спеціалізується на складних породних стрижках."
  },
  {
    id: "groomer-2",
    name: "Марина Петренко",
    avatar: "https://images.unsplash.com/photo-1707720531504-ce087725861a?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=1080",
    specialties: ["Коти", "Бішон фрізе", "SPA процедури"],
    experience: "5 років досвіду",
    bio: "Працює з котами будь-якого темпераменту. Ніжний підхід до кожного вихованця."
  },
  {
    id: "groomer-3",
    name: "Ірина Сидоренко",
    avatar: "https://images.unsplash.com/photo-1632144130358-6cfeed023e27?crop=entropy&cs=tinysrgb&fit=max&fm=jpg&q=80&w=1080",
    specialties: ["Великі породи", "Тримінг", "Експрес-обслуговування"],
    experience: "6 років досвіду",
    bio: "Експерт з роботи з великими та активними собаками. Швидко та якісно."
  }
];

export const reviews = [
  {
    id: "review-1",
    author: "Анна Л.",
    rating: 5,
    text: "Олена - справжній професіонал! Мій пудель виглядає як зірка після кожного відвідування. Дуже рекомендую!",
    pet: "Макс, пудель"
  },
  {
    id: "review-2",
    author: "Дмитро К.",
    rating: 5,
    text: "Найкращий грумінг салон у місті. Зручний онлайн-запис, привітний персонал, відмінний результат.",
    pet: "Белла, йорк"
  },
  {
    id: "review-3",
    author: "Оксана В.",
    rating: 5,
    text: "Марина знайшла підхід до мого кота, який боїться всіх процедур. Тепер ходимо тільки сюди!",
    pet: "Мурчик, британець"
  }
];

export const salonInfo = {
  phone: "+380 12 345 67 89",
  phoneHref: "tel:+380123456789",
  instagram: "@tailbook.salon",
  instagramHref: "https://instagram.com/tailbook.salon",
  address: "вул. Героїв, 25, Київ"
};

export const faqItems = [
  {
    question: "Чи потрібна реєстрація для запису?",
    answer: "Ні, реєстрація не обов'язкова. Ви можете записатися як гість, залишивши тільки контактні дані. Якщо створите акаунт, зможете швидше записуватися наступного разу та переглядати історію відвідувань."
  },
  {
    question: "Як підтверджується запис?",
    answer: "Після відправки заявки ми зв’яжемося з вами для підтвердження часу, майстра та деталей. Це зберігає точність і не порушує реальну модель booking request → appointment."
  },
  {
    question: "Чи можна вказати бажаного грумера?",
    answer: "Так. На кроці вибору часу можна вказати конкретного майстра або залишити варіант ‘без переваги’, щоб ми запропонували перший доступний слот."
  }
];
