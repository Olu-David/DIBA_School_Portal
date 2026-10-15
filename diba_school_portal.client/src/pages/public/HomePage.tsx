import { Link } from "react-router-dom";
import {
  ArrowRight, ArrowUpRight, BookOpen, Baby, Sparkles, GraduationCap,
  Trophy, Users, Building2, FlaskConical, Globe2,
  Calendar, Award, Quote, HeartHandshake, Target, ShieldCheck,
  ScrollText, Beaker, BookMarked, type LucideIcon,
} from "lucide-react";
import HeroCarousel from "../../components/public/HeroCarousel";

interface SectionCard {
  title: string;
  ages: string;
  blurb: string;
  icon: LucideIcon;
  bar: string;
  bg: string;
  ring: string;
  badge: "rose" | "sky" | "violet" | "emerald";
}

const sections: SectionCard[] = [
  {
    title: "Early Years",
    ages: "18 months - 5",
    blurb:
      "Pre-Nursery and Nursery under one roof. Small groups, patient carers, songs in Hausa and English, and a nap room that actually gets used.",
    icon: Baby,
    bar: "from-rose-300 to-rose-500",
    bg: "bg-rose-50/60",
    ring: "hover:border-rose-200",
    badge: "rose",
  },
  {
    title: "Primary",
    ages: "Primary 1 - 6",
    blurb:
      "Six years to build fluent readers and confident mathematicians. We mark every exercise. Twice a week, every week.",
    icon: Sparkles,
    bar: "from-sky-400 to-sky-600",
    bg: "bg-sky-50/60",
    ring: "hover:border-sky-200",
    badge: "sky",
  },
  {
    title: "Junior Secondary",
    ages: "JSS 1 - 3",
    blurb:
      "The BECE years. Broad curriculum, guided choices, and a proper introduction to labs — not just a diagram of one.",
    icon: BookOpen,
    bar: "from-violet-400 to-violet-600",
    bg: "bg-violet-50/60",
    ring: "hover:border-violet-200",
    badge: "violet",
  },
  {
    title: "Senior Secondary",
    ages: "SS 1 - 3",
    blurb:
      "Science, Commercial, or Arts. Three streams, one standard. WAEC and NECO — and after that, university or trade or wherever they're headed.",
    icon: GraduationCap,
    bar: "from-brand-400 to-brand-600",
    bg: "bg-brand-50/60",
    ring: "hover:border-brand-200",
    badge: "emerald",
  },
];

const distinctions: { icon: LucideIcon; title: string; body: string }[] = [
  {
    icon: Target,
    title: "Classes of twenty. Not fifty.",
    body: "We cap every class at twenty pupils. Not because it's trendy — because it's the only way a teacher can mark every exercise twice a week and still know who struggled with question seven.",
  },
  {
    icon: HeartHandshake,
    title: "A church school, and proud of it.",
    body: "We were founded by the Evangelical Church International in 2015. Morning devotion, weekly chapel, and Christian Religious Knowledge are part of every pupil's week — from Pre-Nursery to SS3.",
  },
  {
    icon: FlaskConical,
    title: "The Bunsen burners light.",
    body: "Physics, Chemistry and Biology labs with working apparatus — including a fume hood that was actually installed by a contractor, not just pictured in a brochure.",
  },
  {
    icon: Globe2,
    title: "English taught. Hausa spoken.",
    body: "Instruction is in English. Break-time is not. Our pupils leave fluent in the language of the classroom and comfortable in the language of the market.",
  },
  {
    icon: BookMarked,
    title: "CRK every year. Chapel every week.",
    body: "Not an optional extra — a core subject. Pupils sit CRK through primary and secondary, and every section gathers for chapel once a week. Parents know what they're signing up for.",
  },
  {
    icon: ShieldCheck,
    title: "The gateman knows every name.",
    body: "So does the bursar, the matron, and the Head. No child slips through. That's not a slogan — it's what happens when you stay this size on purpose.",
  },
];

const stats = [
  { value: "487", label: "Pupils enrolled" },
  { value: "37", label: "Teaching & support staff" },
  { value: "10", label: "Years in Sokoto" },
  { value: "21 of 24", label: "WAEC candidates with 5+ credits (2024)" },
];

const achievements = [
  {
    year: "2024",
    title: "First place, Sokoto State Science Fair (Senior)",
    body: "Three SS2 pupils built a solar-powered irrigation pump out of a car battery and a second-hand panel. It works. It's still in the Biology lab.",
    icon: Beaker,
  },
  {
    year: "2024",
    title: "Sokoto Metro Debate Cup — winners",
    body: "Four pupils from SS2 and SS3 went eleven rounds without dropping a single debate. They beat eleven schools across Sokoto and Kebbi to take it.",
    icon: Quote,
  },
  {
    year: "2023",
    title: "Twelve graduates into federal universities",
    body: "UDUS, ABU Zaria, and BUK between them. Three went for Medicine, two for Engineering, the rest for Accounting and Education.",
    icon: Trophy,
  },
  {
    year: "2023",
    title: "Best Private Nursery & Primary School, Sokoto Metro",
    body: "Awarded by the ECI regional synod at their annual education review. We've framed it. It's in the reception.",
    icon: Award,
  },
];

const events = [
  { day: "15", month: "Nov", title: "10th Anniversary & Open Day", time: "10:00 - 15:00" },
  { day: "06", month: "Dec", title: "Inter-House Sports Festival", time: "08:00 - 16:00" },
  { day: "19", month: "Dec", title: "Carol Service & End-of-Term Concert", time: "17:00 - 19:30" },
  { day: "23", month: "Dec", title: "Christmas Break Begins", time: "All day" },
];

const news = [
  {
    tag: "Facilities",
    title: "New science labs open this term — with a working fume hood",
    excerpt:
      "Three rooms refitted for Physics, Chemistry and Biology. Microscopes that focus, burners that light, and a fume hood installed by a contractor who has done this before.",
    image:
      "https://images.unsplash.com/photo-1532094349884-543bc11b234d?auto=format&fit=crop&w=800&q=80",
  },
  {
    tag: "Debate",
    title: "Senior team takes the Sokoto Metro Debate Cup",
    excerpt:
      "Eleven rounds. Eleven wins. Three pupils from SS2, one from SS3. They'll be defending the cup in December.",
    image:
      "https://images.unsplash.com/photo-1544531585-9847b68c8c86?auto=format&fit=crop&w=800&q=80",
  },
  {
    tag: "Admissions",
    title: "2025/2026 admissions open — JSS1 and SS1 nearly full",
    excerpt:
      "Entrance assessments every Saturday at 9am. JSS1 has nine places left. SS1 has six. Early applicants get first pick of streams.",
    image:
      "https://images.unsplash.com/photo-1509062522246-3755977927d7?auto=format&fit=crop&w=800&q=80",
  },
];

const portals = [
  {
    to: "/auth/student",
    label: "Student Portal",
    body: "Check results, view your bill, and pay fees online.",
    icon: GraduationCap,
    ring: "hover:border-brand-300",
    bg: "from-brand-50 to-brand-100/60",
    iconBg: "bg-brand-100 text-brand-700",
  },
  {
    to: "/auth/parent",
    label: "Parent Portal",
    body: "Track your children's fees, results, and school activity.",
    icon: Users,
    ring: "hover:border-sky-300",
    bg: "from-sky-50 to-sky-100/60",
    iconBg: "bg-sky-100 text-sky-700",
  },
  {
    to: "/auth/staff",
    label: "Staff Portal",
    body: "Manage classes, record scores, and access the bursary.",
    icon: Building2,
    ring: "hover:border-violet-300",
    bg: "from-violet-50 to-violet-100/60",
    iconBg: "bg-violet-100 text-violet-700",
  },
];

export default function HomePage() {
  return (
    <>
      <HeroCarousel />

      <section className="bg-flame-400">
        <div className="max-w-7xl mx-auto px-6 lg:px-12 py-4 flex items-center justify-center">
          <p className="text-surface-950 font-serif text-[15px] sm:text-base tracking-wide text-center">
            Knowledge is Wealth
          </p>
        </div>
      </section>

      <section className="max-w-7xl mx-auto px-6 lg:px-12 py-20 lg:py-28">
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-12 items-start">
          <div className="lg:col-span-5">
            <p className="text-[12px] tracking-[0.25em] uppercase text-brand-600 font-semibold mb-4">
              Welcome to DIBA
            </p>
            <h2 className="font-serif text-3xl sm:text-4xl lg:text-[40px] leading-tight text-surface-950 tracking-tight">
              Twenty-four pupils in 2015.{" "}
              <span className="italic text-flame-600">Four hundred and eighty-seven</span>{" "}
              today. Same bungalow-standard of care.
            </h2>
          </div>
          <div className="lg:col-span-7 lg:pt-4">
            <p className="text-[16px] leading-relaxed text-surface-700">
              DIBA opened on 6 November 2015 with twenty-four pupils in a rented
              bungalow off Trade Fair Road. Three of those twenty-four graduated
              in our first WAEC cohort in 2023.
            </p>
            <p className="mt-5 text-[16px] leading-relaxed text-surface-700">
              We are still small enough to know every child's name. Still large
              enough to run three senior streams and a proper science lab. The
              bungalow is gone. The way we teach has not changed.
            </p>
            <div className="mt-8 flex items-center gap-4">
              <Link
                to="/about"
                className="inline-flex items-center gap-2 text-brand-600 font-semibold text-[14px] border-b-2 border-flame-400 pb-1 hover:border-brand-600 transition-colors"
              >
                Read our story
                <ArrowRight className="w-4 h-4" />
              </Link>
            </div>
          </div>
        </div>
      </section>

      <section className="bg-surface-100/70 border-y border-surface-200">
        <div className="max-w-7xl mx-auto px-6 lg:px-12 py-20 lg:py-24">
          <div className="mb-12 max-w-2xl">
            <p className="text-[12px] tracking-[0.25em] uppercase text-brand-600 font-semibold mb-3">
              Our sections
            </p>
            <h2 className="font-serif text-3xl sm:text-4xl text-surface-950 tracking-tight">
              Four sections. One continuous journey.
            </h2>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5">
            {sections.map((s) => {
              const Icon = s.icon;
              return (
                <div
                  key={s.title}
                  className={`group relative bg-white rounded-3xl border border-surface-200 p-7 transition-all duration-300 ${s.ring} hover:shadow-lg hover:-translate-y-1`}
                >
                  <div className={`h-1 w-12 rounded-full bg-gradient-to-r ${s.bar} mb-6`} />
                  <div className={`w-12 h-12 rounded-2xl ${s.bg} grid place-items-center mb-5`}>
                    <Icon className="w-5 h-5 text-surface-700" strokeWidth={1.8} />
                  </div>
                  <p className="text-[11px] uppercase tracking-[0.2em] text-surface-500 font-medium">
                    {s.ages}
                  </p>
                  <h3 className="mt-1 font-serif text-[22px] text-surface-950 leading-tight">
                    {s.title}
                  </h3>
                  <p className="mt-3 text-[13px] leading-relaxed text-surface-600">
                    {s.blurb}
                  </p>
                  <div className="mt-6 flex items-center gap-2 text-brand-600 text-[12px] font-semibold">
                    Learn more
                    <ArrowUpRight className="w-3.5 h-3.5 group-hover:translate-x-0.5 group-hover:-translate-y-0.5 transition-transform" />
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      </section>

      <section className="bg-surface-950 text-white">
        <div className="max-w-7xl mx-auto px-6 lg:px-12 py-16 lg:py-20">
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-8">
            {stats.map((s) => (
              <div key={s.label}>
                <p className="font-serif text-4xl sm:text-5xl text-flame-300 leading-none">
                  {s.value}
                </p>
                <p className="mt-3 text-[12px] uppercase tracking-[0.2em] text-white/70 font-medium">
                  {s.label}
                </p>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="max-w-7xl mx-auto px-6 lg:px-12 py-20 lg:py-28">
        <div className="mb-12 max-w-2xl">
          <p className="text-[12px] tracking-[0.25em] uppercase text-brand-600 font-semibold mb-3">
            Why parents choose DIBA
          </p>
          <h2 className="font-serif text-3xl sm:text-4xl text-surface-950 tracking-tight">
            Six things that set us apart.
          </h2>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-x-10 gap-y-12">
          {distinctions.map((d, i) => {
            const Icon = d.icon;
            return (
              <div key={d.title}>
                <div className="flex items-start gap-4">
                  <div className="shrink-0">
                    <div className="w-11 h-11 rounded-2xl bg-flame-100 grid place-items-center">
                      <Icon className="w-5 h-5 text-flame-700" strokeWidth={1.8} />
                    </div>
                  </div>
                  <div>
                    <p className="text-[11px] tracking-[0.2em] uppercase text-surface-400 font-medium">
                      {String(i + 1).padStart(2, "0")}
                    </p>
                    <h3 className="mt-1 font-serif text-[19px] text-surface-950 leading-snug">
                      {d.title}
                    </h3>
                    <p className="mt-2 text-[13px] leading-relaxed text-surface-600">
                      {d.body}
                    </p>
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      </section>

      <section className="bg-surface-100/70 border-y border-surface-200">
        <div className="max-w-7xl mx-auto px-6 lg:px-12 py-20 lg:py-24">
          <div className="grid grid-cols-1 lg:grid-cols-12 gap-12">
            <div className="lg:col-span-4">
              <p className="text-[12px] tracking-[0.25em] uppercase text-brand-600 font-semibold mb-3">
                Honour roll
              </p>
              <h2 className="font-serif text-3xl sm:text-4xl text-surface-950 tracking-tight leading-tight">
                Recent achievements
              </h2>
              <p className="mt-5 text-[14px] leading-relaxed text-surface-600">
                Beyond the classroom, our pupils compete, perform, and lead. A
                selection of what they've done lately.
              </p>
              <Link
                to="/about"
                className="mt-8 inline-flex items-center gap-2 text-brand-600 font-semibold text-[14px] border-b-2 border-flame-400 pb-1 hover:border-brand-600 transition-colors"
              >
                Full record
                <ArrowRight className="w-4 h-4" />
              </Link>
            </div>

            <div className="lg:col-span-8">
              <div className="space-y-3">
                {achievements.map((a) => {
                  const Icon = a.icon;
                  return (
                    <div
                      key={a.title}
                      className="flex items-start gap-5 p-5 bg-white rounded-2xl border border-surface-200 hover:border-flame-200 hover:shadow-sm transition-all"
                    >
                      <div className="w-11 h-11 rounded-xl bg-surface-50 grid place-items-center shrink-0">
                        <Icon className="w-5 h-5 text-brand-700" strokeWidth={1.8} />
                      </div>
                      <div className="flex-1 min-w-0">
                        <div className="flex items-start gap-3 flex-wrap">
                          <span className="text-[11px] font-semibold tracking-wider text-flame-600">
                            {a.year}
                          </span>
                          <h3 className="font-serif text-[17px] text-surface-950 leading-snug">
                            {a.title}
                          </h3>
                        </div>
                        <p className="mt-2 text-[13px] text-surface-600 leading-relaxed">
                          {a.body}
                        </p>
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>
          </div>
        </div>
      </section>

      <section className="max-w-7xl mx-auto px-6 lg:px-12 py-20 lg:py-24">
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-10">
          <div className="lg:col-span-4">
            <div className="flex items-center gap-3 mb-6">
              <Calendar className="w-5 h-5 text-brand-700" />
              <h2 className="font-serif text-2xl text-surface-950">Upcoming</h2>
            </div>
            <div className="space-y-3">
              {events.map((e) => (
                <div
                  key={e.title}
                  className="flex items-center gap-4 p-4 bg-white rounded-2xl border border-surface-200"
                >
                  <div className="w-14 h-14 rounded-2xl bg-surface-950 text-white grid place-items-center shrink-0">
                    <div className="text-center leading-none">
                      <p className="text-[18px] font-semibold">{e.day}</p>
                      <p className="text-[10px] uppercase tracking-wider text-flame-300 mt-0.5">
                        {e.month}
                      </p>
                    </div>
                  </div>
                  <div className="min-w-0">
                    <p className="text-[13px] font-semibold text-surface-950 leading-tight">
                      {e.title}
                    </p>
                    <p className="text-[11px] text-surface-500 mt-1">{e.time}</p>
                  </div>
                </div>
              ))}
            </div>
          </div>

          <div className="lg:col-span-8">
            <div className="flex items-center justify-between mb-6">
              <div className="flex items-center gap-3">
                <ScrollText className="w-5 h-5 text-brand-700" />
                <h2 className="font-serif text-2xl text-surface-950">Latest from campus</h2>
              </div>
              <Link
                to="/about"
                className="hidden sm:inline-flex items-center gap-1 text-brand-600 text-[13px] font-semibold hover:underline"
              >
                All news <ArrowUpRight className="w-3.5 h-3.5" />
              </Link>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-5">
              {news.map((n) => (
                <article
                  key={n.title}
                  className="group bg-white rounded-2xl border border-surface-200 overflow-hidden hover:shadow-md transition-all"
                >
                  <div className="aspect-[4/3] overflow-hidden bg-surface-200">
                    <img
                      src={n.image}
                      alt=""
                      className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-700"
                      loading="lazy"
                    />
                  </div>
                  <div className="p-5">
                    <span className="inline-block text-[10px] tracking-wider uppercase text-flame-700 bg-flame-50 px-2 py-1 rounded font-semibold">
                      {n.tag}
                    </span>
                    <h3 className="mt-3 font-serif text-[16px] text-surface-950 leading-snug">
                      {n.title}
                    </h3>
                    <p className="mt-2 text-[12px] text-surface-600 leading-relaxed line-clamp-3">
                      {n.excerpt}
                    </p>
                  </div>
                </article>
              ))}
            </div>
          </div>
        </div>
      </section>

      <section className="bg-surface-100/70 border-y border-surface-200">
        <div className="max-w-7xl mx-auto px-6 lg:px-12 py-20 lg:py-24">
          <div className="text-center max-w-2xl mx-auto mb-12">
            <p className="text-[12px] tracking-[0.25em] uppercase text-brand-600 font-semibold mb-3">
              Already part of DIBA?
            </p>
            <h2 className="font-serif text-3xl sm:text-4xl text-surface-950 tracking-tight">
              Sign in to your portal
            </h2>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-5">
            {portals.map((p) => {
              const Icon = p.icon;
              return (
                <Link
                  key={p.to}
                  to={p.to}
                  className={`group relative bg-gradient-to-br ${p.bg} rounded-3xl border border-surface-200 p-7 transition-all ${p.ring} hover:shadow-lg hover:-translate-y-1`}
                >
                  <div className={`w-12 h-12 rounded-2xl ${p.iconBg} grid place-items-center mb-6`}>
                    <Icon className="w-5 h-5" strokeWidth={1.8} />
                  </div>
                  <h3 className="font-serif text-[20px] text-surface-950">{p.label}</h3>
                  <p className="mt-2 text-[13px] text-surface-600 leading-relaxed">{p.body}</p>
                  <div className="mt-6 inline-flex items-center gap-2 text-brand-600 text-[13px] font-semibold">
                    Continue
                    <ArrowRight className="w-3.5 h-3.5 group-hover:translate-x-0.5 transition-transform" />
                  </div>
                </Link>
              );
            })}
          </div>
        </div>
      </section>

      <section className="max-w-7xl mx-auto px-6 lg:px-12 py-20 lg:py-24">
        <div className="relative overflow-hidden rounded-[32px] bg-surface-950 text-white">
          <div className="absolute inset-0 opacity-20">
            <div className="absolute -top-24 -right-24 w-96 h-96 rounded-full bg-brand-500 blur-3xl" />
            <div className="absolute -bottom-24 -left-24 w-80 h-80 rounded-full bg-flame-500 blur-3xl" />
          </div>

          <div className="relative z-10 px-8 sm:px-14 lg:px-20 py-16 lg:py-20 grid grid-cols-1 lg:grid-cols-12 gap-10 items-center">
            <div className="lg:col-span-8">
              <div className="flex items-center gap-4 mb-6">
                <img
                  src="/images/logo.png"
                  alt="Dominion Intelligent Builders' Academy"
                  className="w-14 h-14 object-contain"
                />
                <div>
                  <p className="text-[11px] tracking-[0.3em] uppercase text-flame-300 font-semibold">
                    2025 / 2026
                  </p>
                  <p className="text-[12px] text-white/70 mt-0.5">Admissions now open</p>
                </div>
              </div>
              <h2 className="font-serif text-3xl sm:text-4xl lg:text-[44px] leading-tight tracking-tight">
                Come see a class in session.
              </h2>
              <p className="mt-5 text-white/75 text-[15px] leading-relaxed max-w-2xl">
                Saturdays at 9am, or any weekday between 10 and 2. No appointment
                needed — walk in, ask for the Head, and we'll take you round
                ourselves.
              </p>
            </div>

            <div className="lg:col-span-4 flex flex-col gap-3">
              <Link
                to="/admissions"
                className="inline-flex items-center justify-center gap-2 px-6 py-4 rounded-full bg-flame-400 text-surface-950 font-semibold text-[14px] hover:bg-flame-300 transition-colors"
              >
                Begin application
                <ArrowRight className="w-4 h-4" />
              </Link>
              <Link
                to="/contact"
                className="inline-flex items-center justify-center gap-2 px-6 py-4 rounded-full border border-white/25 font-medium text-[14px] hover:bg-white/10 transition-colors"
              >
                Book a campus tour
              </Link>
            </div>
          </div>
        </div>
      </section>
    </>
  );
}