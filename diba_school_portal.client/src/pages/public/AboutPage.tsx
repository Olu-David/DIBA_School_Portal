import { Link } from "react-router-dom";
import {
  ArrowRight, Target, Eye, Heart, BookOpen, Award, Users,
  Building2, Microscope, Library, Trees, Trophy, Cross,
  Quote, Calendar,
} from "lucide-react";
import SchoolCrest from "../../components/public/SchoolCrest";

const timeline = [
  {
    year: "2010",
    title: "The founding",
    body: "Nine students, three teachers, and one rented hall in Lekki Phase 1. DIBA opens with a Nursery and Primary section.",
  },
  {
    year: "2014",
    title: "Secondary section opens",
    body: "Approved by the Lagos State Ministry of Education. First JSS1 cohort of 48 students enrols.",
  },
  {
    year: "2017",
    title: "Purpose-built campus",
    body: "Move to our current 4-acre campus. Science labs, library, sports field, and a 400-seat hall commissioned.",
  },
  {
    year: "2020",
    title: "Digital pivot",
    body: "During the pandemic, DIBA runs live online classes for every section. The Learning Management System remains in use today.",
  },
  {
    year: "2022",
    title: "First Cambridge cohort",
    body: "Six students sit Cambridge IGCSE and A-Level exams. All earn distinctions in Mathematics and Sciences.",
  },
  {
    year: "2024",
    title: "850 students strong",
    body: "Across four sections. 98% WAEC credit rate. Six alumni at Harvard, Yale, MIT, Imperial, UCL, and Bristol.",
  },
];

const values = [
  {
    icon: Target,
    title: "Knowledge",
    body: "We pursue academic excellence with discipline. Curiosity is rewarded, questions are welcome, and laziness has no home here.",
  },
  {
    icon: Heart,
    title: "Character",
    body: "The classroom builds the mind. The house system, sports, and service build the person. We care about who your child becomes.",
  },
  {
    icon: Users,
    title: "Service",
    body: "Every senior student completes 40 hours of community service. Leadership is not a title — it is what you give away.",
  },
];

const facilities = [
  { icon: Microscope, name: "Science laboratories", body: "Two fully equipped labs — Physics, Chemistry, and Biology." },
  { icon: Library, name: "Library & reading room", body: "8,000+ volumes. Digital catalogue. Open until 5pm." },
  { icon: Building2, name: "ICT suite", body: "40 workstations. Coding, robotics, and digital literacy." },
  { icon: Trees, name: "Sports field & courts", body: "Full-size football pitch, basketball, and volleyball courts." },
  { icon: Trophy, name: "400-seat hall", body: "Assemblies, theatre, prize-giving, and inter-house competitions." },
  { icon: Cross, name: "Chapel & counselling", body: "Weekly services. Full-time guidance counsellor on staff." },
];

const leadership = [
  { name: "Dr. Emmanuel Adekunle", role: "Principal", initials: "EA", bio: "PhD Education (UNILAG), 22 years in secondary education." },
  { name: "Mrs. Ngozi Okafor", role: "Vice Principal (Academics)", initials: "NO", bio: "M.Ed Curriculum Studies. Former WAEC examiner." },
  { name: "Mr. Tunde Balogun", role: "Head, Junior Secondary", initials: "TB", bio: "B.Sc Mathematics, PGDE. Coordinates the BECE programme." },
  { name: "Mrs. Folake Ajayi", role: "Head, Primary", initials: "FA", bio: "B.Ed Early Childhood. Leads our foundational literacy programme." },
];

export default function AboutPage() {
  return (
    <>
      {/* Hero */}
      <section className="bg-emerald-950 text-white">
        <div className="max-w-7xl mx-auto px-6 lg:px-12 py-20 lg:py-28 grid grid-cols-1 lg:grid-cols-12 gap-10 items-center">
          <div className="lg:col-span-7">
            <p className="text-[12px] tracking-[0.25em] uppercase text-amber-300 font-semibold mb-4">
              About us
            </p>
            <h1 className="font-serif text-4xl sm:text-5xl lg:text-6xl leading-[1.05] tracking-tight">
              Fifteen years of building{" "}
              <span className="italic text-amber-300">intelligent</span> men and women.
            </h1>
            <p className="mt-6 text-white/75 text-[16px] leading-relaxed max-w-2xl">
              Since 2010, Dominion Intelligent Builders Academy has quietly grown
              into one of Lekki's most respected independent schools — not because
              of grand marketing, but because parents talk, and alumni come back.
            </p>
          </div>
          <div className="lg:col-span-5 flex justify-center lg:justify-end">
            <div className="w-52 h-52 rounded-full bg-white/5 backdrop-blur grid place-items-center border border-white/10">
              <SchoolCrest className="w-32 h-32" tone="gold" />
            </div>
          </div>
        </div>
      </section>

      {/* Motto band */}
      <section className="bg-amber-400">
        <div className="max-w-7xl mx-auto px-6 lg:px-12 py-4 flex items-center justify-center">
          <p className="text-emerald-950 font-serif text-[15px] sm:text-base tracking-wide text-center">
            Knowledge<span className="mx-4 opacity-40">·</span>
            Character<span className="mx-4 opacity-40">·</span>
            Service
          </p>
        </div>
      </section>

      {/* Mission & Vision */}
      <section className="max-w-7xl mx-auto px-6 lg:px-12 py-20 lg:py-24">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
          <div className="bg-white rounded-3xl border border-stone-200 p-8 lg:p-10">
            <div className="w-12 h-12 rounded-2xl bg-emerald-50 grid place-items-center mb-6">
              <Target className="w-5 h-5 text-emerald-700" strokeWidth={1.8} />
            </div>
            <p className="text-[11px] tracking-[0.25em] uppercase text-slate-500 font-semibold mb-3">
              Our mission
            </p>
            <h2 className="font-serif text-2xl text-emerald-950 leading-snug">
              To develop intelligent, disciplined, and God-fearing young people
              equipped for lifelong learning.
            </h2>
            <p className="mt-5 text-[14px] text-slate-600 leading-relaxed">
              We provide a rigorous, balanced education that honours both the mind
              and the character — preparing each student for the examinations of
              life, not just the examinations of the term.
            </p>
          </div>

          <div className="bg-white rounded-3xl border border-stone-200 p-8 lg:p-10">
            <div className="w-12 h-12 rounded-2xl bg-amber-50 grid place-items-center mb-6">
              <Eye className="w-5 h-5 text-amber-700" strokeWidth={1.8} />
            </div>
            <p className="text-[11px] tracking-[0.25em] uppercase text-slate-500 font-semibold mb-3">
              Our vision
            </p>
            <h2 className="font-serif text-2xl text-emerald-950 leading-snug">
              To be West Africa's most trusted school for building intelligent
              leaders.
            </h2>
            <p className="mt-5 text-[14px] text-slate-600 leading-relaxed">
              A school where admissions are earned, not bought. Where every
              graduate leaves knowing how to think, how to work, and how to serve.
            </p>
          </div>
        </div>
      </section>

      {/* Core values */}
      <section className="bg-stone-100/70 border-y border-stone-200">
        <div className="max-w-7xl mx-auto px-6 lg:px-12 py-20 lg:py-24">
          <div className="mb-12 max-w-2xl">
            <p className="text-[12px] tracking-[0.25em] uppercase text-emerald-700 font-semibold mb-3">
              What we stand for
            </p>
            <h2 className="font-serif text-3xl sm:text-4xl text-emerald-950 tracking-tight">
              Three words we live by.
            </h2>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            {values.map((v) => {
              const Icon = v.icon;
              return (
                <div key={v.title} className="bg-white rounded-3xl border border-stone-200 p-8">
                  <div className="w-12 h-12 rounded-2xl bg-emerald-50 grid place-items-center mb-6">
                    <Icon className="w-5 h-5 text-emerald-700" strokeWidth={1.8} />
                  </div>
                  <h3 className="font-serif text-2xl text-emerald-950">{v.title}</h3>
                  <p className="mt-4 text-[14px] text-slate-600 leading-relaxed">{v.body}</p>
                </div>
              );
            })}
          </div>
        </div>
      </section>

      {/* Timeline */}
      <section className="max-w-7xl mx-auto px-6 lg:px-12 py-20 lg:py-28">
        <div className="mb-12 max-w-2xl">
          <p className="text-[12px] tracking-[0.25em] uppercase text-emerald-700 font-semibold mb-3">
            Our story
          </p>
          <h2 className="font-serif text-3xl sm:text-4xl text-emerald-950 tracking-tight">
            Fifteen years, one direction.
          </h2>
        </div>

        <div className="relative">
          <div className="absolute left-[7px] sm:left-[9px] top-3 bottom-3 w-px bg-stone-300" />
          <div className="space-y-12">
            {timeline.map((t) => (
              <div key={t.year} className="relative pl-10 sm:pl-14">
                <div className="absolute left-0 top-1.5 w-4 h-4 sm:w-5 sm:h-5 rounded-full bg-amber-400 border-4 border-stone-50" />
                <p className="font-serif text-[22px] text-emerald-800">{t.year}</p>
                <h3 className="mt-1 font-serif text-[22px] text-emerald-950 leading-snug">
                  {t.title}
                </h3>
                <p className="mt-3 text-[14px] text-slate-600 leading-relaxed max-w-2xl">
                  {t.body}
                </p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Facilities */}
      <section className="bg-stone-100/70 border-y border-stone-200">
        <div className="max-w-7xl mx-auto px-6 lg:px-12 py-20 lg:py-24">
          <div className="mb-12 max-w-2xl">
            <p className="text-[12px] tracking-[0.25em] uppercase text-emerald-700 font-semibold mb-3">
              Campus
            </p>
            <h2 className="font-serif text-3xl sm:text-4xl text-emerald-950 tracking-tight">
              Four acres, built for learning.
            </h2>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-5">
            {facilities.map((f) => {
              const Icon = f.icon;
              return (
                <div key={f.name} className="bg-white rounded-2xl border border-stone-200 p-6">
                  <div className="w-11 h-11 rounded-xl bg-emerald-50 grid place-items-center mb-5">
                    <Icon className="w-5 h-5 text-emerald-700" strokeWidth={1.8} />
                  </div>
                  <h3 className="font-serif text-[18px] text-emerald-950 leading-snug">
                    {f.name}
                  </h3>
                  <p className="mt-2 text-[13px] text-slate-600 leading-relaxed">{f.body}</p>
                </div>
              );
            })}
          </div>
        </div>
      </section>

      {/* Leadership */}
      <section className="max-w-7xl mx-auto px-6 lg:px-12 py-20 lg:py-28">
        <div className="mb-12 max-w-2xl">
          <p className="text-[12px] tracking-[0.25em] uppercase text-emerald-700 font-semibold mb-3">
            Leadership
          </p>
          <h2 className="font-serif text-3xl sm:text-4xl text-emerald-950 tracking-tight">
            The people in charge.
          </h2>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
          {leadership.map((p) => (
            <div key={p.name} className="bg-white rounded-2xl border border-stone-200 p-6">
              <div className="w-16 h-16 rounded-2xl bg-emerald-950 text-white grid place-items-center mb-5">
                <span className="font-serif text-[20px]">{p.initials}</span>
              </div>
              <h3 className="font-serif text-[17px] text-emerald-950 leading-snug">{p.name}</h3>
              <p className="mt-1 text-[11px] tracking-wider uppercase text-amber-700 font-semibold">
                {p.role}
              </p>
              <p className="mt-4 text-[12px] text-slate-600 leading-relaxed">{p.bio}</p>
            </div>
          ))}
        </div>
      </section>

      {/* Quote band */}
      <section className="bg-emerald-950 text-white">
        <div className="max-w-5xl mx-auto px-6 lg:px-12 py-20 lg:py-28 text-center">
          <Quote className="w-10 h-10 text-amber-400 mx-auto mb-8 opacity-60" />
          <blockquote className="font-serif text-2xl sm:text-3xl lg:text-4xl leading-snug tracking-tight">
            "A child educated only at school is an uneducated child. At DIBA, we
            partner with the home to build the whole person."
          </blockquote>
          <p className="mt-8 text-white/60 text-[12px] tracking-[0.25em] uppercase font-medium">
            Dr. Emmanuel Adekunle · Principal
          </p>
        </div>
      </section>

      {/* CTA */}
      <section className="max-w-7xl mx-auto px-6 lg:px-12 py-20 lg:py-24">
        <div className="text-center max-w-2xl mx-auto">
          <h2 className="font-serif text-3xl sm:text-4xl text-emerald-950 tracking-tight leading-tight">
            Come and see for yourself.
          </h2>
          <p className="mt-4 text-[15px] text-slate-600 leading-relaxed">
            Nothing beats walking the corridors, meeting our teachers, and watching
            a class in session. Tours run every weekday at 10am.
          </p>
          <div className="mt-8 flex flex-col sm:flex-row gap-3 justify-center">
            <Link
              to="/admissions"
              className="inline-flex items-center justify-center gap-2 px-7 py-3.5 rounded-full bg-emerald-800 text-white font-semibold text-[14px] hover:bg-emerald-900 transition-colors"
            >
              Begin application
              <ArrowRight className="w-4 h-4" />
            </Link>
            <Link
              to="/contact"
              className="inline-flex items-center justify-center gap-2 px-7 py-3.5 rounded-full border border-emerald-900/20 text-emerald-900 font-medium text-[14px] hover:bg-emerald-50 transition-colors"
            >
              Book a tour
            </Link>
          </div>
        </div>
      </section>
    </>
  );
}