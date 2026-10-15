import { Link } from "react-router-dom";
import {
  ArrowRight, BookOpen, Baby, Sparkles, GraduationCap, FlaskConical,
  Calculator, Languages, Globe2, Palette, Music, Dumbbell, Cpu,
  ScrollText, CheckCircle2, type LucideIcon,
} from "lucide-react";

interface SectionBlock {
  key: "earlyYears" | "primary" | "jss" | "sss";
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
    key: "earlyYears",
    title: "Early Years",
    ages: "18 months - 5",
    bar: "from-rose-300 to-rose-500",
    badgeBg: "bg-rose-50",
    badgeText: "text-rose-700",
    intro:
      "Pre-Nursery and Nursery in one place. Small groups, patient carers, songs in Hausa and English, and a nap room that actually gets used. The work of this stage is play, and we take it seriously.",
    curriculum: [
      "Early literacy & phonics (Jolly Phonics)",
      "Early numeracy & pattern work",
      "Sensory play & fine-motor skills",
      "Songs, rhymes & story-time",
      "Sharing, waiting & gentle routines",
      "Hausa & English language exposure",
    ],
    examPath: "Observation-based continuous assessment - No formal testing",
  },
  {
    key: "primary",
    title: "Primary",
    ages: "Primary 1 - 6",
    bar: "from-sky-400 to-sky-600",
    badgeBg: "bg-sky-50",
    badgeText: "text-sky-700",
    intro:
      "Six years to build fluent readers and confident mathematicians. We mark every exercise. Twice a week, every week. By Primary 6, your child will write, count, and reason without hesitation.",
    curriculum: [
      "English Language & Reading",
      "Mathematics & Mental Maths",
      "Basic Science & Technology",
      "Social Studies & Civic Education",
      "Hausa Language",
      "Computer Studies & Coding basics",
      "Christian or Islamic Religious Knowledge",
      "Cultural & Creative Arts",
      "Physical & Health Education",
    ],
    examPath: "Continuous assessment + End-of-Term examinations",
  },
  {
    key: "jss",
    title: "Junior Secondary",
    ages: "JSS 1 - 3",
    bar: "from-violet-400 to-violet-600",
    badgeBg: "bg-violet-50",
    badgeText: "text-violet-700",
    intro:
      "The BECE years. A broad curriculum, guided choices, and a proper introduction to labs — not just a diagram of one. Three years to find out what your child is good at, before choosing a stream.",
    curriculum: [
      "English Language",
      "Mathematics",
      "Basic Science & Technology",
      "Business Studies & Book-keeping",
      "Social Studies & Civic Education",
      "Computer Studies (ICT)",
      "Agricultural Science",
      "Home Economics",
      "Christian or Islamic Religious Knowledge",
      "Hausa, French & Cultural Arts",
      "Physical & Health Education",
    ],
    examPath: "BECE - Sokoto State Ministry of Education",
  },
  {
    key: "sss",
    title: "Senior Secondary",
    ages: "SS 1 - 3",
    bar: "from-brand-400 to-brand-600",
    badgeBg: "bg-brand-50",
    badgeText: "text-brand-700",
    intro:
      "Science, Commercial, or Arts. Three streams, one standard. WAEC and NECO — and after that, university or trade or wherever they're headed. The choice is theirs; the preparation is ours.",
    curriculum: [
      "English Language & Literature",
      "Mathematics (General & Further)",
      "Biology - Chemistry - Physics",
      "Economics - Accounting - Commerce",
      "Government - History - Geography",
      "Civic Education",
      "Computer Science & Data Processing",
      "Christian or Islamic Religious Knowledge",
      "Hausa, French & Fine Art",
    ],
    examPath: "WAEC (WASSCE) - NECO (SSCE)",
  },
];

const streams: { name: string; icon: LucideIcon; subjects: string; tone: string }[] = [
  { name: "Science", icon: FlaskConical, subjects: "Physics, Chemistry, Biology, Further Mathematics", tone: "brand" },
  { name: "Commercial", icon: Calculator, subjects: "Accounting, Commerce, Economics, Book-keeping", tone: "flame" },
  { name: "Arts & Humanities", icon: Languages, subjects: "Literature, Government, History, Islamic Studies / CRK, French", tone: "violet" },
];

const coCurricular: { name: string; icon: LucideIcon }[] = [
  { name: "Robotics & Coding Club", icon: Cpu },
  { name: "Debate & Public Speaking", icon: ScrollText },
  { name: "Music & Choir", icon: Music },
  { name: "Visual Arts", icon: Palette },
  { name: "Football & Athletics", icon: Dumbbell },
  { name: "French Club", icon: Globe2 },
  { name: "Press & Literary Club", icon: BookOpen },
  { name: "Young Farmers", icon: Sparkles },
];

const results = [
  { stat: "21 of 24", label: "WAEC candidates with 5+ credits (2024)" },
  { stat: "88%", label: "BECE pass rate (2024)" },
  { stat: "12", label: "Federal university admissions (2023 cohort)" },
  { stat: "100%", label: "Pupils who sat JSS1 stream guidance" },
];

export default function AcademicsPage() {
  return (
    <>
      <section className="bg-surface-950 text-white">
        <div className="max-w-7xl mx-auto px-6 lg:px-12 py-20 lg:py-28">
          <p className="text-[12px] tracking-[0.25em] uppercase text-flame-300 font-semibold mb-4">
            Academics
          </p>
          <h1 className="font-serif text-4xl sm:text-5xl lg:text-6xl leading-[1.05] tracking-tight max-w-4xl">
            What we teach, by year group —{" "}
            <span className="italic text-flame-300">no surprises.</span>
          </h1>
          <p className="mt-6 text-white/75 text-[16px] leading-relaxed max-w-2xl">
            From the Pre-Nursery sandbox in Old Airport to the Senior Secondary
            science laboratory, every stage at DIBA is intentional - designed to
            stretch, support, and prepare our pupils for Sokoto and beyond.
          </p>
        </div>
      </section>

      <section className="bg-flame-400">
        <div className="max-w-7xl mx-auto px-6 lg:px-12 py-4 flex items-center justify-center">
          <p className="text-surface-950 font-serif text-[15px] sm:text-base tracking-wide text-center">
            Knowledge is Wealth
          </p>
        </div>
      </section>

      {blocks.map((b, idx) => (
        <section
          key={b.key}
          className={
            idx % 2 === 0
              ? "max-w-7xl mx-auto px-6 lg:px-12 py-20 lg:py-24"
              : "bg-surface-100/70 border-y border-surface-200"
          }
        >
          <div className={idx % 2 === 0 ? "" : "max-w-7xl mx-auto px-6 lg:px-12 py-20 lg:py-24"}>
            <div className="grid grid-cols-1 lg:grid-cols-12 gap-12">
              <div className="lg:col-span-5">
                <div className={`h-1 w-16 rounded-full bg-gradient-to-r ${b.bar} mb-6`} />
                <span className={`inline-block text-[11px] tracking-wider uppercase ${b.badgeText} ${b.badgeBg} px-3 py-1.5 rounded-full font-semibold`}>
                  {b.ages}
                </span>
                <h2 className="mt-5 font-serif text-3xl sm:text-4xl text-surface-950 tracking-tight">
                  {b.title}
                </h2>
                <p className="mt-5 text-[15px] text-surface-700 leading-relaxed">
                  {b.intro}
                </p>

                <div className="mt-8 p-5 bg-white rounded-2xl border border-surface-200">
                  <p className="text-[11px] tracking-[0.2em] uppercase text-surface-500 font-semibold mb-2">
                    Assessment path
                  </p>
                  <p className="text-[13px] text-brand-700 font-medium">{b.examPath}</p>
                </div>
              </div>

              <div className="lg:col-span-7">
                <p className="text-[11px] tracking-[0.2em] uppercase text-surface-500 font-semibold mb-5">
                  Curriculum highlights
                </p>
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-x-8 gap-y-3">
                  {b.curriculum.map((c) => (
                    <div key={c} className="flex items-start gap-3">
                      <CheckCircle2 className="w-4 h-4 text-brand-600 mt-0.5 shrink-0" />
                      <span className="text-[14px] text-surface-700 leading-relaxed">{c}</span>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          </div>
        </section>
      ))}

      <section className="max-w-7xl mx-auto px-6 lg:px-12 py-20 lg:py-24">
        <div className="mb-12 max-w-2xl">
          <p className="text-[12px] tracking-[0.25em] uppercase text-brand-600 font-semibold mb-3">
            Senior Secondary
          </p>
          <h2 className="font-serif text-3xl sm:text-4xl text-surface-950 tracking-tight">
            Choose a stream in SS1. Choose it properly.
          </h2>
          <p className="mt-5 text-[15px] text-surface-600 leading-relaxed">
            Every SS1 pupil sits with our careers counsellor before picking a
            stream. We aim for fit — not prestige. A pupil who thrives in
            Commercial beats one who struggles through Science.
          </p>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          {streams.map((s) => {
            const Icon = s.icon;
            const toneRing: Record<string, string> = {
              brand: "hover:border-brand-300",
              flame: "hover:border-flame-300",
              violet: "hover:border-violet-300",
            };
            const toneBg: Record<string, string> = {
              brand: "bg-brand-50 text-brand-700",
              flame: "bg-flame-50 text-flame-700",
              violet: "bg-violet-50 text-violet-700",
            };
            return (
              <div
                key={s.name}
                className={`bg-white rounded-3xl border border-surface-200 p-8 transition-all ${toneRing[s.tone]} hover:shadow-md`}
              >
                <div className={`w-12 h-12 rounded-2xl ${toneBg[s.tone]} grid place-items-center mb-6`}>
                  <Icon className="w-5 h-5" strokeWidth={1.8} />
                </div>
                <h3 className="font-serif text-2xl text-surface-950">{s.name}</h3>
                <p className="mt-3 text-[14px] text-surface-600 leading-relaxed">{s.subjects}</p>
              </div>
            );
          })}
        </div>
      </section>

      <section className="bg-surface-950 text-white">
        <div className="max-w-7xl mx-auto px-6 lg:px-12 py-20 lg:py-24">
          <div className="mb-12 max-w-2xl">
            <p className="text-[12px] tracking-[0.25em] uppercase text-flame-300 font-semibold mb-3">
              Results
            </p>
            <h2 className="font-serif text-3xl sm:text-4xl tracking-tight">
              Numbers that keep us honest.
            </h2>
          </div>

          <div className="grid grid-cols-2 lg:grid-cols-4 gap-8">
            {results.map((r) => (
              <div key={r.label}>
                <p className="font-serif text-4xl sm:text-5xl text-flame-300 leading-none">
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

      <section className="bg-surface-100/70 border-y border-surface-200">
        <div className="max-w-7xl mx-auto px-6 lg:px-12 py-20 lg:py-24">
          <div className="mb-12 max-w-2xl">
            <p className="text-[12px] tracking-[0.25em] uppercase text-brand-600 font-semibold mb-3">
              Beyond the classroom
            </p>
            <h2 className="font-serif text-3xl sm:text-4xl text-surface-950 tracking-tight">
              What happens after 3pm.
            </h2>
          </div>

          <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4">
            {coCurricular.map((c) => {
              const Icon = c.icon;
              return (
                <div
                  key={c.name}
                  className="bg-white rounded-2xl border border-surface-200 p-5 hover:border-flame-200 hover:shadow-sm transition-all"
                >
                  <Icon className="w-5 h-5 text-brand-700 mb-4" strokeWidth={1.8} />
                  <p className="text-[13px] font-medium text-surface-950 leading-snug">
                    {c.name}
                  </p>
                </div>
              );
            })}
          </div>
        </div>
      </section>

      <section className="max-w-7xl mx-auto px-6 lg:px-12 py-20 lg:py-24">
        <div className="text-center max-w-2xl mx-auto">
          <h2 className="font-serif text-3xl sm:text-4xl text-surface-950 tracking-tight leading-tight">
            Ready to see it in person?
          </h2>
          <p className="mt-4 text-[15px] text-surface-600 leading-relaxed">
            Every applicant is invited for a classroom visit and a short
            assessment. It helps us place your child well - and helps you decide.
          </p>
          <div className="mt-8 flex flex-col sm:flex-row gap-3 justify-center">
            <Link
              to="/admissions"
              className="inline-flex items-center justify-center gap-2 px-7 py-3.5 rounded-full bg-brand-600 text-white font-semibold text-[14px] hover:bg-brand-700 transition-colors"
            >
              Begin application
              <ArrowRight className="w-4 h-4" />
            </Link>
            <Link
              to="/contact"
              className="inline-flex items-center justify-center gap-2 px-7 py-3.5 rounded-full border border-surface-900/20 text-surface-900 font-medium text-[14px] hover:bg-surface-50 transition-colors"
            >
              Talk to us
            </Link>
          </div>
        </div>
      </section>
    </>
  );
}