import { Link } from "react-router-dom";
import {
  ArrowRight, BookOpen, Baby, Sparkles, GraduationCap, FlaskConical,
  Calculator, Languages, Globe2, Palette, Music, Dumbbell, Cpu,
  ScrollText, Award, CheckCircle2, type LucideIcon,
} from "lucide-react";

interface SectionBlock {
  key: "nursery" | "primary" | "jss" | "sss";
  title: string;
  ages: string;
  bar: string;
  badgeBg: string;
  badgeText: string;
  intro: string;
  curriculum: string[];
  examPath: string;
}

const blocks: SectionBlock[] = [
  {
    key: "nursery",
    title: "Nursery",
    ages: "Ages 2 – 5",
    bar: "from-pink-400 to-pink-600",
    badgeBg: "bg-pink-50",
    badgeText: "text-pink-700",
    intro:
      "Play is the work of childhood. Our Nursery programme channels natural curiosity into early literacy, numeracy, motor skills, and social confidence.",
    curriculum: [
      "Early literacy & phonics (Jolly Phonics)",
      "Early numeracy & pattern work",
      "Creative arts, music & movement",
      "Sensorial play & fine-motor skills",
      "Social & emotional learning",
      "Story-time and library visits",
    ],
    examPath: "Continuous assessment · No formal examinations",
  },
  {
    key: "primary",
    title: "Primary",
    ages: "Ages 6 – 11",
    bar: "from-sky-400 to-sky-600",
    badgeBg: "bg-sky-50",
    badgeText: "text-sky-700",
    intro:
      "The foundational years. We build fluent readers, confident mathematicians, and curious scientists — while never losing the joy of learning.",
    curriculum: [
      "English Language & Literature",
      "Mathematics & Mental Maths",
      "Basic Science & Technology",
      "Social Studies & Civic Education",
      "Computer Studies & Coding basics",
      "French, Yoruba, and Cultural Arts",
      "Bible Knowledge & Moral Instruction",
      "Physical & Health Education",
    ],
    examPath: "Continuous assessment + End-of-Term examinations",
  },
  {
    key: "jss",
    title: "Junior Secondary",
    ages: "Ages 12 – 14",
    bar: "from-violet-400 to-violet-600",
    badgeBg: "bg-violet-50",
    badgeText: "text-violet-700",
    intro:
      "Three years preparing for the BECE. Broad enough to keep every door open, rigorous enough to hold real academic weight.",
    curriculum: [
      "English Language",
      "Mathematics",
      "Basic Science & Technology",
      "Business Studies & Book-keeping",
      "Social Studies & Civic Education",
      "Computer Studies (ICT)",
      "Agricultural Science",
      "Home Economics",
      "Fine Art & Music",
      "French & Yoruba",
      "Physical & Health Education",
    ],
    examPath: "BECE (Basic Education Certificate Examination) · Lagos State",
  },
  {
    key: "sss",
    title: "Senior Secondary",
    ages: "Ages 15 – 17",
    bar: "from-emerald-400 to-emerald-600",
    badgeBg: "bg-emerald-50",
    badgeText: "text-emerald-700",
    intro:
      "Three streams. One high standard. Whether Science, Arts, or Commercial, our SS students leave prepared for university and for life.",
    curriculum: [
      "English Language & Literature",
      "Mathematics (General & Further)",
      "Biology · Chemistry · Physics",
      "Economics · Accounting · Commerce",
      "Government · History · Geography",
      "Civic Education",
      "Computer Science & Data Processing",
      "Fine Art & Music",
      "French & Yoruba",
    ],
    examPath: "WAEC (WASSCE) · NECO (SSCE) · Cambridge IGCSE & A-Level (optional)",
  },
];

const streams: { name: string; icon: LucideIcon; subjects: string; tone: string }[] = [
  { name: "Science", icon: FlaskConical, subjects: "Physics, Chemistry, Biology, Further Maths", tone: "emerald" },
  { name: "Commercial", icon: Calculator, subjects: "Accounting, Commerce, Economics, Book-keeping", tone: "amber" },
  { name: "Arts & Humanities", icon: Languages, subjects: "Literature, Government, History, French", tone: "violet" },
];

const coCurricular: { name: string; icon: LucideIcon }[] = [
  { name: "Robotics Club", icon: Cpu },
  { name: "Debate & Public Speaking", icon: ScrollText },
  { name: "Music & Choir", icon: Music },
  { name: "Visual Arts", icon: Palette },
  { name: "Football & Athletics", icon: Dumbbell },
  { name: "French Club", icon: Globe2 },
  { name: "Press Club", icon: BookOpen },
  { name: "Young Farmers", icon: Sparkles },
];

const results = [
  { stat: "98%", label: "WAEC credit rate (5+ subjects, 2024)" },
  { stat: "94%", label: "BECE distinction rate (2024)" },
  { stat: "42", label: "Distinctions in 2024 Cambridge IGCSE" },
  { stat: "6", label: "Ivy League & Russell Group admits (last 3 years)" },
];

export default function AcademicsPage() {
  return (
    <>
      {/* Hero */}
      <section className="bg-emerald-950 text-white">
        <div className="max-w-7xl mx-auto px-6 lg:px-12 py-20 lg:py-28">
          <p className="text-[12px] tracking-[0.25em] uppercase text-amber-300 font-semibold mb-4">
            Academics
          </p>
          <h1 className="font-serif text-4xl sm:text-5xl lg:text-6xl leading-[1.05] tracking-tight max-w-4xl">
            A curriculum built for{" "}
            <span className="italic text-amber-300">the whole child.</span>
          </h1>
          <p className="mt-6 text-white/75 text-[16px] leading-relaxed max-w-2xl">
            From the Nursery sandbox to the Senior Secondary science lab, every
            stage at DIBA is intentional — designed to stretch, support, and
            prepare.
          </p>
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

      {/* Section blocks */}
      {blocks.map((b, idx) => (
        <section
          key={b.key}
          className={
            idx % 2 === 0
              ? "max-w-7xl mx-auto px-6 lg:px-12 py-20 lg:py-24"
              : "bg-stone-100/70 border-y border-stone-200"
          }
        >
          <div className={idx % 2 === 0 ? "" : "max-w-7xl mx-auto px-6 lg:px-12 py-20 lg:py-24"}>
            <div className="grid grid-cols-1 lg:grid-cols-12 gap-12">
              <div className="lg:col-span-5">
                <div className={`h-1 w-16 rounded-full bg-gradient-to-r ${b.bar} mb-6`} />
                <span className={`inline-block text-[11px] tracking-wider uppercase ${b.badgeText} ${b.badgeBg} px-3 py-1.5 rounded-full font-semibold`}>
                  {b.ages}
                </span>
                <h2 className="mt-5 font-serif text-3xl sm:text-4xl text-emerald-950 tracking-tight">
                  {b.title}
                </h2>
                <p className="mt-5 text-[15px] text-slate-700 leading-relaxed">
                  {b.intro}
                </p>

                <div className="mt-8 p-5 bg-white rounded-2xl border border-stone-200">
                  <p className="text-[11px] tracking-[0.2em] uppercase text-slate-500 font-semibold mb-2">
                    Assessment path
                  </p>
                  <p className="text-[13px] text-emerald-900 font-medium">{b.examPath}</p>
                </div>
              </div>

              <div className="lg:col-span-7">
                <p className="text-[11px] tracking-[0.2em] uppercase text-slate-500 font-semibold mb-5">
                  Curriculum highlights
                </p>
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-x-8 gap-y-3">
                  {b.curriculum.map((c) => (
                    <div key={c} className="flex items-start gap-3">
                      <CheckCircle2 className="w-4 h-4 text-emerald-600 mt-0.5 shrink-0" />
                      <span className="text-[14px] text-slate-700 leading-relaxed">{c}</span>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          </div>
        </section>
      ))}

      {/* Streams */}
      <section className="max-w-7xl mx-auto px-6 lg:px-12 py-20 lg:py-24">
        <div className="mb-12 max-w-2xl">
          <p className="text-[12px] tracking-[0.25em] uppercase text-emerald-700 font-semibold mb-3">
            Senior Secondary
          </p>
          <h2 className="font-serif text-3xl sm:text-4xl text-emerald-950 tracking-tight">
            Three streams. One standard.
          </h2>
          <p className="mt-5 text-[15px] text-slate-600 leading-relaxed">
            SS1 students choose a stream at the start of the academic year after
            a guided conversation with our careers counsellor. Our aim is fit — not
            prestige.
          </p>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          {streams.map((s) => {
            const Icon = s.icon;
            const toneRing: Record<string, string> = {
              emerald: "hover:border-emerald-300",
              amber: "hover:border-amber-300",
              violet: "hover:border-violet-300",
            };
            const toneBg: Record<string, string> = {
              emerald: "bg-emerald-50 text-emerald-700",
              amber: "bg-amber-50 text-amber-700",
              violet: "bg-violet-50 text-violet-700",
            };
            return (
              <div
                key={s.name}
                className={`bg-white rounded-3xl border border-stone-200 p-8 transition-all ${toneRing[s.tone]} hover:shadow-md`}
              >
                <div className={`w-12 h-12 rounded-2xl ${toneBg[s.tone]} grid place-items-center mb-6`}>
                  <Icon className="w-5 h-5" strokeWidth={1.8} />
                </div>
                <h3 className="font-serif text-2xl text-emerald-950">{s.name}</h3>
                <p className="mt-3 text-[14px] text-slate-600 leading-relaxed">{s.subjects}</p>
              </div>
            );
          })}
        </div>
      </section>

      {/* Results */}
      <section className="bg-emerald-950 text-white">
        <div className="max-w-7xl mx-auto px-6 lg:px-12 py-20 lg:py-24">
          <div className="mb-12 max-w-2xl">
            <p className="text-[12px] tracking-[0.25em] uppercase text-amber-300 font-semibold mb-3">
              Results
            </p>
            <h2 className="font-serif text-3xl sm:text-4xl tracking-tight">
              Numbers that keep us honest.
            </h2>
          </div>

          <div className="grid grid-cols-2 lg:grid-cols-4 gap-8">
            {results.map((r) => (
              <div key={r.label}>
                <p className="font-serif text-4xl sm:text-5xl text-amber-300 leading-none">
                  {r.stat}
                </p>
                <p className="mt-3 text-[12px] uppercase tracking-[0.2em] text-white/70 font-medium leading-snug">
                  {r.label}
                </p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Co-curricular */}
      <section className="bg-stone-100/70 border-y border-stone-200">
        <div className="max-w-7xl mx-auto px-6 lg:px-12 py-20 lg:py-24">
          <div className="mb-12 max-w-2xl">
            <p className="text-[12px] tracking-[0.25em] uppercase text-emerald-700 font-semibold mb-3">
              Beyond the classroom
            </p>
            <h2 className="font-serif text-3xl sm:text-4xl text-emerald-950 tracking-tight">
              Clubs, societies, and everything after 3pm.
            </h2>
          </div>

          <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4">
            {coCurricular.map((c) => {
              const Icon = c.icon;
              return (
                <div
                  key={c.name}
                  className="bg-white rounded-2xl border border-stone-200 p-5 hover:border-amber-200 hover:shadow-sm transition-all"
                >
                  <Icon className="w-5 h-5 text-emerald-700 mb-4" strokeWidth={1.8} />
                  <p className="text-[13px] font-medium text-emerald-950 leading-snug">
                    {c.name}
                  </p>
                </div>
              );
            })}
          </div>
        </div>
      </section>

      {/* CTA */}
      <section className="max-w-7xl mx-auto px-6 lg:px-12 py-20 lg:py-24">
        <div className="text-center max-w-2xl mx-auto">
          <h2 className="font-serif text-3xl sm:text-4xl text-emerald-950 tracking-tight leading-tight">
            Ready to see it in person?
          </h2>
          <p className="mt-4 text-[15px] text-slate-600 leading-relaxed">
            Every applicant is invited for a classroom visit and a short
            assessment. It helps us place your child well — and helps you
            decide.
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
              Talk to us
            </Link>
          </div>
        </div>
      </section>
    </>
  );
}