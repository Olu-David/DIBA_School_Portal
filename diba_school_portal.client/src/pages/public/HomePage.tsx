import { Link } from "react-router-dom";
import {
  ArrowRight, ArrowUpRight, BookOpen, Baby, Sparkles, GraduationCap,
  Trophy, Users, Building2, FlaskConical, Palette, Music, Globe2,
  Calendar, Award, Quote, HeartHandshake, Target, ShieldCheck,
  ScrollText, Beaker, Microscope, LucideIcon,
} from "lucide-react";
import HeroCarousel from "../../components/public/HeroCarousel";
import SchoolCrest from "../../components/public/SchoolCrest";

// ---------- Section data ----------
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
    title: "Nursery",
    ages: "Ages 2 – 5",
    blurb: "Play-based learning in warm, safe surroundings. Early literacy, numeracy, and wonder.",
    icon: Baby,
    bar: "from-pink-400 to-pink-600",
    bg: "bg-pink-50/60",
    ring: "hover:border-pink-200",
    badge: "rose",
  },
  {
    title: "Primary",
    ages: "Ages 6 – 11",
    blurb: "Strong foundations in English, Mathematics, Science, and Character. Small classes.",
    icon: Sparkles,
    bar: "from-sky-400 to-sky-600",
    bg: "bg-sky-50/60",
    ring: "hover:border-sky-200",
    badge: "sky",
  },
  {
    title: "Junior Secondary",
    ages: "Ages 12 – 14",
    blurb: "The BECE years. Broad curriculum, guided discovery, and leadership training.",
    icon: BookOpen,
    bar: "from-violet-400 to-violet-600",
    bg: "bg-violet-50/60",
    ring: "hover:border-violet-200",
    badge: "violet",
  },
  {
    title: "Senior Secondary",
    ages: "Ages 15 – 17",
    blurb: "Science, Arts, and Commercial streams. WAEC, NECO, and Cambridge examination pathways.",
    icon: GraduationCap,
    bar: "from-emerald-400 to-emerald-600",
    bg: "bg-emerald-50/60",
    ring: "hover:border-emerald-200",
    badge: "emerald",
  },
];

// ---------- Distinctions ----------
const distinctions: { icon: LucideIcon; title: string; body: string }[] = [
  {
    icon: Target,
    title: "Small classes. Big attention.",
    body: "Average class size of 18 students. Every child is known by name and by need.",
  },
  {
    icon: FlaskConical,
    title: "STEM-first curriculum",
    body: "Two science labs, an ICT suite, and a robotics club. Science is a subject and a habit.",
  },
  {
    icon: HeartHandshake,
    title: "Character before credentials",
    body: "Weekly moral instruction, house system, service projects. We build people, not just grades.",
  },
  {
    icon: Globe2,
    title: "Global outlook, local roots",
    body: "Cambridge & WAEC pathways. Graduates in Lagos, London, Toronto, and Boston.",
  },
  {
    icon: Palette,
    title: "Arts and creativity",
    body: "Music, visual arts, drama, and African culture. Every student plays, paints, or performs.",
  },
  {
    icon: ShieldCheck,
    title: "Safe, supervised campus",
    body: "24/7 security, a resident nurse, CCTV coverage, and disciplined boarding options.",
  },
];

// ---------- Stats ----------
const stats = [
  { value: "850+", label: "Students enrolled" },
  { value: "62", label: "Qualified teachers" },
  { value: "15", label: "Years of excellence" },
  { value: "98%", label: "WAEC credit rate" },
];

// ---------- Achievements ----------
const achievements = [
  {
    year: "2024",
    title: "National Science Olympiad — 1st Place (Senior)",
    body: "Chidera Okonkwo (SS3 Science) beat 320 contenders from 48 schools.",
    icon: Beaker,
  },
  {
    year: "2024",
    title: "Lagos Inter-School Debate Champions",
    body: "DIBA's team took the trophy for the third consecutive year.",
    icon: Quote,
  },
  {
    year: "2023",
    title: "Best Private Secondary School — Lekki Zone",
    body: "Awarded by the Lagos State Ministry of Education.",
    icon: Award,
  },
  {
    year: "2023",
    title: "6 graduates admitted to Ivy League & Russell Group",
    body: "Harvard, Yale, MIT, Imperial, UCL, and Bristol.",
    icon: Trophy,
  },
];

// ---------- Events ----------
const events = [
  { day: "15", month: "Nov", title: "Open Day & Campus Tour", time: "10:00 — 14:00" },
  { day: "06", month: "Dec", title: "Inter-House Sports Festival", time: "08:00 — 16:00" },
  { day: "20", month: "Dec", title: "Carol Service & End-of-Term Concert", time: "17:00 — 19:30" },
  { day: "23", month: "Dec", title: "Christmas Break Begins", time: "All day" },
];

// ---------- News ----------
const news = [
  {
    tag: "Facilities",
    title: "Newly commissioned secondary science lab opens this term",
    excerpt:
      "Fully equipped with fume hoods, microscopes, and digital sensors — three times the size of our old lab.",
    image:
      "https://images.unsplash.com/photo-1532094349884-543bc11b234d?auto=format&fit=crop&w=800&q=80",
  },
  {
    tag: "Debate",
    title: "DIBA senior team wins the Lagos Inter-School Debate Cup",
    excerpt:
      "Our four-person team won every round, defeating schools from Ikoyi, Victoria Island, and Lekki.",
    image:
      "https://images.unsplash.com/photo-1544531585-9847b68c8c86?auto=format&fit=crop&w=800&q=80",
  },
  {
    tag: "Admissions",
    title: "2025/2026 admissions now open across all sections",
    excerpt:
      "Entrance assessments hold every Saturday from 9am. Limited places in JSS1 and SS1.",
    image:
      "https://images.unsplash.com/photo-1509062522246-3755977927d7?auto=format&fit=crop&w=800&q=80",
  },
];

// ---------- Portals ----------
const portals = [
  {
    to: "/auth/student",
    label: "Student Portal",
    body: "Check results, view your bill, and pay fees online.",
    icon: GraduationCap,
    ring: "hover:border-emerald-300",
    bg: "from-emerald-50 to-emerald-100/60",
    iconBg: "bg-emerald-100 text-emerald-700",
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

      {/* -------------------- MOTTO RIBBON -------------------- */}
      <section className="bg-amber-400">
        <div className="max-w-7xl mx-auto px-6 lg:px-12 py-4 flex items-center justify-center">
          <p className="text-emerald-950 font-serif text-[15px] sm:text-base tracking-wide text-center">
            Knowledge<span className="mx-4 opacity-40">·</span>
            Character<span className="mx-4 opacity-40">·</span>
            Service
          </p>
        </div>
      </section>

      {/* -------------------- WELCOME / MOTTO -------------------- */}
      <section className="max-w-7xl mx-auto px-6 lg:px-12 py-20 lg:py-28">
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-12 items-start">
          <div className="lg:col-span-5">
            <p className="text-[12px] tracking-[0.25em] uppercase text-emerald-700 font-semibold mb-4">
              Welcome to DIBA
            </p>
            <h2 className="font-serif text-3xl sm:text-4xl lg:text-[40px] leading-tight text-emerald-950 tracking-tight">
              A school that takes{" "}
              <span className="italic text-amber-600">intelligence</span> and{" "}
              <span className="italic text-amber-600">character</span> seriously.
            </h2>
          </div>
          <div className="lg:col-span-7 lg:pt-4">
            <p className="text-[16px] leading-relaxed text-slate-700">
              Dominion Intelligent Builders Academy was founded in 2010 on a simple conviction: that every child carries
              within them the capacity for excellence — academic, moral, and personal. We exist to draw it out.
            </p>
            <p className="mt-5 text-[16px] leading-relaxed text-slate-700">
              From our Nursery in Lekki Phase 1 through Senior Secondary, our students learn in small classes, are led by
              teachers who know them personally, and are prepared not just for WAEC and Cambridge but for a life of purpose.
            </p>
            <div className="mt-8 flex items-center gap-4">
              <Link
                to="/about"
                className="inline-flex items-center gap-2 text-emerald-800 font-semibold text-[14px] border-b-2 border-amber-400 pb-1 hover:border-emerald-800 transition-colors"
              >
                Read our story
                <ArrowRight className="w-4 h-4" />
              </Link>
            </div>
          </div>
        </div>
      </section>

      {/* -------------------- FOUR SECTIONS -------------------- */}
      <section className="bg-stone-100/70 border-y border-stone-200">
        <div className="max-w-7xl mx-auto px-6 lg:px-12 py-20 lg:py-24">
          <div className="mb-12 max-w-2xl">
            <p className="text-[12px] tracking-[0.25em] uppercase text-emerald-700 font-semibold mb-3">
              Our sections
            </p>
            <h2 className="font-serif text-3xl sm:text-4xl text-emerald-950 tracking-tight">
              Four sections. One continuous journey.
            </h2>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5">
            {sections.map((s) => {
              const Icon = s.icon;
              return (
                <div
                  key={s.title}
                  className={`group relative bg-white rounded-3xl border border-stone-200 p-7 transition-all duration-300 ${s.ring} hover:shadow-lg hover:-translate-y-1`}
                >
                  <div className={`h-1 w-12 rounded-full bg-gradient-to-r ${s.bar} mb-6`} />
                  <div className={`w-12 h-12 rounded-2xl ${s.bg} grid place-items-center mb-5`}>
                    <Icon className="w-5 h-5 text-slate-700" strokeWidth={1.8} />
                  </div>
                  <p className="text-[11px] uppercase tracking-[0.2em] text-slate-500 font-medium">
                    {s.ages}
                  </p>
                  <h3 className="mt-1 font-serif text-[22px] text-emerald-950 leading-tight">
                    {s.title}
                  </h3>
                  <p className="mt-3 text-[13px] leading-relaxed text-slate-600">
                    {s.blurb}
                  </p>
                  <div className="mt-6 flex items-center gap-2 text-emerald-800 text-[12px] font-semibold">
                    Learn more
                    <ArrowUpRight className="w-3.5 h-3.5 group-hover:translate-x-0.5 group-hover:-translate-y-0.5 transition-transform" />
                  </div>
                </div>
              );
            })}
          </div>
        </div>
      </section>

      {/* -------------------- STATS -------------------- */}
      <section className="bg-emerald-950 text-white">
        <div className="max-w-7xl mx-auto px-6 lg:px-12 py-16 lg:py-20">
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-8">
            {stats.map((s) => (
              <div key={s.label}>
                <p className="font-serif text-4xl sm:text-5xl text-amber-300 leading-none">
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

      {/* -------------------- DISTINCTIONS -------------------- */}
      <section className="max-w-7xl mx-auto px-6 lg:px-12 py-20 lg:py-28">
        <div className="mb-12 max-w-2xl">
          <p className="text-[12px] tracking-[0.25em] uppercase text-emerald-700 font-semibold mb-3">
            Why parents choose DIBA
          </p>
          <h2 className="font-serif text-3xl sm:text-4xl text-emerald-950 tracking-tight">
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
                    <div className="w-11 h-11 rounded-2xl bg-amber-100 grid place-items-center">
                      <Icon className="w-5 h-5 text-amber-700" strokeWidth={1.8} />
                    </div>
                  </div>
                  <div>
                    <p className="text-[11px] tracking-[0.2em] uppercase text-slate-400 font-medium">
                      {String(i + 1).padStart(2, "0")}
                    </p>
                    <h3 className="mt-1 font-serif text-[19px] text-emerald-950 leading-snug">
                      {d.title}
                    </h3>
                    <p className="mt-2 text-[13px] leading-relaxed text-slate-600">
                      {d.body}
                    </p>
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      </section>

      {/* -------------------- HONOUR ROLL -------------------- */}
      <section className="bg-stone-100/70 border-y border-stone-200">
        <div className="max-w-7xl mx-auto px-6 lg:px-12 py-20 lg:py-24">
          <div className="grid grid-cols-1 lg:grid-cols-12 gap-12">
            <div className="lg:col-span-4">
              <p className="text-[12px] tracking-[0.25em] uppercase text-emerald-700 font-semibold mb-3">
                Honour roll
              </p>
              <h2 className="font-serif text-3xl sm:text-4xl text-emerald-950 tracking-tight leading-tight">
                Recent achievements
              </h2>
              <p className="mt-5 text-[14px] leading-relaxed text-slate-600">
                Beyond the classroom, our students compete, perform, and lead. A selection of what they've done lately.
              </p>
              <Link
                to="/about"
                className="mt-8 inline-flex items-center gap-2 text-emerald-800 font-semibold text-[14px] border-b-2 border-amber-400 pb-1 hover:border-emerald-800 transition-colors"
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
                      className="flex items-start gap-5 p-5 bg-white rounded-2xl border border-stone-200 hover:border-amber-200 hover:shadow-sm transition-all"
                    >
                      <div className="w-11 h-11 rounded-xl bg-emerald-50 grid place-items-center shrink-0">
                        <Icon className="w-5 h-5 text-emerald-700" strokeWidth={1.8} />
                      </div>
                      <div className="flex-1 min-w-0">
                        <div className="flex items-start gap-3 flex-wrap">
                          <span className="text-[11px] font-semibold tracking-wider text-amber-600">
                            {a.year}
                          </span>
                          <h3 className="font-serif text-[17px] text-emerald-950 leading-snug">
                            {a.title}
                          </h3>
                        </div>
                        <p className="mt-2 text-[13px] text-slate-600 leading-relaxed">
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

      {/* -------------------- EVENTS + NEWS -------------------- */}
      <section className="max-w-7xl mx-auto px-6 lg:px-12 py-20 lg:py-24">
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-10">
          {/* Events */}
          <div className="lg:col-span-4">
            <div className="flex items-center gap-3 mb-6">
              <Calendar className="w-5 h-5 text-emerald-700" />
              <h2 className="font-serif text-2xl text-emerald-950">Upcoming</h2>
            </div>
            <div className="space-y-3">
              {events.map((e) => (
                <div
                  key={e.title}
                  className="flex items-center gap-4 p-4 bg-white rounded-2xl border border-stone-200"
                >
                  <div className="w-14 h-14 rounded-2xl bg-emerald-950 text-white grid place-items-center shrink-0">
                    <div className="text-center leading-none">
                      <p className="text-[18px] font-semibold">{e.day}</p>
                      <p className="text-[10px] uppercase tracking-wider text-amber-300 mt-0.5">
                        {e.month}
                      </p>
                    </div>
                  </div>
                  <div className="min-w-0">
                    <p className="text-[13px] font-semibold text-emerald-950 leading-tight">
                      {e.title}
                    </p>
                    <p className="text-[11px] text-slate-500 mt-1">{e.time}</p>
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* News */}
          <div className="lg:col-span-8">
            <div className="flex items-center justify-between mb-6">
              <div className="flex items-center gap-3">
                <ScrollText className="w-5 h-5 text-emerald-700" />
                <h2 className="font-serif text-2xl text-emerald-950">Latest from campus</h2>
              </div>
              <Link
                to="/about"
                className="hidden sm:inline-flex items-center gap-1 text-emerald-800 text-[13px] font-semibold hover:underline"
              >
                All news <ArrowUpRight className="w-3.5 h-3.5" />
              </Link>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-5">
              {news.map((n) => (
                <article
                  key={n.title}
                  className="group bg-white rounded-2xl border border-stone-200 overflow-hidden hover:shadow-md transition-all"
                >
                  <div className="aspect-[4/3] overflow-hidden bg-stone-200">
                    <img
                      src={n.image}
                      alt=""
                      className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-700"
                      loading="lazy"
                    />
                  </div>
                  <div className="p-5">
                    <span className="inline-block text-[10px] tracking-wider uppercase text-amber-700 bg-amber-50 px-2 py-1 rounded font-semibold">
                      {n.tag}
                    </span>
                    <h3 className="mt-3 font-serif text-[16px] text-emerald-950 leading-snug">
                      {n.title}
                    </h3>
                    <p className="mt-2 text-[12px] text-slate-600 leading-relaxed line-clamp-3">
                      {n.excerpt}
                    </p>
                  </div>
                </article>
              ))}
            </div>
          </div>
        </div>
      </section>

      {/* -------------------- PORTALS -------------------- */}
      <section className="bg-stone-100/70 border-y border-stone-200">
        <div className="max-w-7xl mx-auto px-6 lg:px-12 py-20 lg:py-24">
          <div className="text-center max-w-2xl mx-auto mb-12">
            <p className="text-[12px] tracking-[0.25em] uppercase text-emerald-700 font-semibold mb-3">
              Already part of DIBA?
            </p>
            <h2 className="font-serif text-3xl sm:text-4xl text-emerald-950 tracking-tight">
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
                  className={`group relative bg-gradient-to-br ${p.bg} rounded-3xl border border-stone-200 p-7 transition-all ${p.ring} hover:shadow-lg hover:-translate-y-1`}
                >
                  <div className={`w-12 h-12 rounded-2xl ${p.iconBg} grid place-items-center mb-6`}>
                    <Icon className="w-5 h-5" strokeWidth={1.8} />
                  </div>
                  <h3 className="font-serif text-[20px] text-emerald-950">{p.label}</h3>
                  <p className="mt-2 text-[13px] text-slate-600 leading-relaxed">{p.body}</p>
                  <div className="mt-6 inline-flex items-center gap-2 text-emerald-800 text-[13px] font-semibold">
                    Continue
                    <ArrowRight className="w-3.5 h-3.5 group-hover:translate-x-0.5 transition-transform" />
                  </div>
                </Link>
              );
            })}
          </div>
        </div>
      </section>

      {/* -------------------- CTA BAND -------------------- */}
      <section className="max-w-7xl mx-auto px-6 lg:px-12 py-20 lg:py-24">
        <div className="relative overflow-hidden rounded-[32px] bg-emerald-950 text-white">
          <div className="absolute inset-0 opacity-20">
            <div className="absolute -top-24 -right-24 w-96 h-96 rounded-full bg-amber-400 blur-3xl" />
            <div className="absolute -bottom-24 -left-24 w-80 h-80 rounded-full bg-emerald-500 blur-3xl" />
          </div>

          <div className="relative z-10 px-8 sm:px-14 lg:px-20 py-16 lg:py-20 grid grid-cols-1 lg:grid-cols-12 gap-10 items-center">
            <div className="lg:col-span-8">
              <div className="flex items-center gap-4 mb-6">
                <SchoolCrest className="w-14 h-14" tone="gold" />
                <div>
                  <p className="text-[11px] tracking-[0.3em] uppercase text-amber-300 font-semibold">
                    2025 / 2026
                  </p>
                  <p className="text-[12px] text-white/70 mt-0.5">Admissions now open</p>
                </div>
              </div>
              <h2 className="font-serif text-3xl sm:text-4xl lg:text-[44px] leading-tight tracking-tight">
                Give your child a school worth{" "}
                <span className="italic text-amber-300">growing into.</span>
              </h2>
              <p className="mt-5 text-white/75 text-[15px] leading-relaxed max-w-2xl">
                Places are limited. Book an entrance assessment, take a tour, or speak with
                our admissions team — we'd love to meet you.
              </p>
            </div>

            <div className="lg:col-span-4 flex flex-col gap-3">
              <Link
                to="/admissions"
                className="inline-flex items-center justify-center gap-2 px-6 py-4 rounded-full bg-amber-400 text-emerald-950 font-semibold text-[14px] hover:bg-amber-300 transition-colors"
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