import { Link } from "react-router-dom";
import {
  ArrowRight, Target, Eye, Heart, BookOpen, Award, Users,
  Building2, Microscope, Library, Trees, Trophy, BookMarked,
  Quote, Church,
} from "lucide-react";

const timeline = [
  {
    year: "2015",
    title: "The founding",
    body: "On 6 November 2015, the Evangelical Church International, Sokoto, registered Dominion Intelligent Builders' Academy (RC-2372959). Twenty-four pupils, four teachers, and one rented bungalow off Trade Fair Road.",
  },
  {
    year: "2017",
    title: "The Old Airport campus",
    body: "The school moved to its purpose-built site beside the RCCG headquarters at Old Airport Area. Six new classrooms, an assembly ground, and a small library.",
  },
  {
    year: "2019",
    title: "Junior Secondary opens",
    body: "Approved by the Sokoto State Ministry of Education. Our first JSS1 cohort of thirty-eight pupils began the BECE pathway.",
  },
  {
    year: "2021",
    title: "Senior Secondary opens",
    body: "DIBA became a full Nursery to SS institution. The first SS1 stream of twenty-two pupils began the Science pathway.",
  },
  {
    year: "2023",
    title: "First WAEC cohort",
    body: "Twenty-four candidates sat the WASSCE. Twenty-one earned credits in five or more subjects including English Language and Mathematics.",
  },
  {
    year: "2025",
    title: "Five hundred pupils strong",
    body: "Across four sections with forty teaching and support staff. The school now serves families from across Sokoto and neighbouring Kebbi State.",
  },
];

const values = [
  {
    icon: Heart,
    title: "Faith",
    body: "We are a school founded by the Evangelical Church International. Weekly devotion, moral instruction, and pastoral care shape the character of every pupil - Christian and Muslim alike.",
  },
  {
    icon: BookMarked,
    title: "Knowledge",
    body: "Academic excellence pursued with discipline and honesty. Curiosity is rewarded, questions are welcome, and no pupil is left behind.",
  },
  {
    icon: Users,
    title: "Service",
    body: "Every senior pupil completes community service hours before graduation. Leadership is not a title - it is what you give away.",
  },
];

const facilities = [
  { icon: Microscope, name: "Science laboratories", body: "Physics, Chemistry, and Biology labs with functioning apparatus and a fume hood." },
  { icon: Library, name: "Library & reading room", body: "Several thousand titles covering Nigerian and international curricula." },
  { icon: Building2, name: "ICT suite", body: "Thirty workstations for Computer Studies, coding, and digital literacy." },
  { icon: Trees, name: "Sports field", body: "Football pitch, volleyball court, and open space for athletics." },
  { icon: Trophy, name: "Assembly hall", body: "Seats five hundred. Used for assemblies, drama, prize-giving, and chapel." },
  { icon: Church, name: "Chapel & prayer rooms", body: "A chapel for Christian worship, plus a quiet room for Muslim prayers on campus." },
];

const leadership = [
  {
    name: "[Founder's Name]",
    role: "Founder & Proprietor",
    initials: "FN",
    bio: "Evangelical Church International, Sokoto. Registered the school in November 2015.",
  },
  {
    name: "[Principal's Name]",
    role: "Principal",
    initials: "PN",
    bio: "Academic leadership across all four sections. Placeholder - to be updated.",
  },
  {
    name: "[VP Academics' Name]",
    role: "Vice Principal (Academics)",
    initials: "VP",
    bio: "Curriculum, examinations, and teacher development. Placeholder - to be updated.",
  },
  {
    name: "[Head Primary's Name]",
    role: "Head, Nursery & Primary",
    initials: "HP",
    bio: "Leads the foundational years and the early literacy programme. Placeholder - to be updated.",
  },
];

export default function AboutPage() {
  return (
    <>
      <section className="bg-surface-950 text-white">
        <div className="max-w-7xl mx-auto px-6 lg:px-12 py-20 lg:py-28 grid grid-cols-1 lg:grid-cols-12 gap-10 items-center">
          <div className="lg:col-span-7">
            <p className="text-[12px] tracking-[0.25em] uppercase text-flame-300 font-semibold mb-4">
              About us
            </p>
            <h1 className="font-serif text-4xl sm:text-5xl lg:text-6xl leading-[1.05] tracking-tight">
              Ten years of building{" "}
              <span className="italic text-flame-300">intelligent</span> men and women.
            </h1>
            <p className="mt-6 text-white/75 text-[16px] leading-relaxed max-w-2xl">
              Founded in Sokoto in November 2015 by the Evangelical Church
              International, Dominion Intelligent Builders' Academy has grown
              quietly and steadily into one of the most trusted private schools
              in the Old Airport area.
            </p>
          </div>
          <div className="lg:col-span-5 flex justify-center lg:justify-end">
            <div className="w-52 h-52 rounded-full bg-white/5 backdrop-blur grid place-items-center border border-white/10 overflow-hidden">
              <img  
                src="/images/logo1.png"  
                alt="Dominion Intelligent Builders' Academy"
                className="w-32 h-32 object-contain"
              />
            </div>
          </div>
        </div>
      </section>

      <section className="bg-flame-400">
        <div className="max-w-7xl mx-auto px-6 lg:px-12 py-4 flex items-center justify-center">
          <p className="text-surface-950 font-serif text-[15px] sm:text-base tracking-wide text-center">
            Knowledge is Wealth
          </p>
        </div>
      </section>

      <section className="max-w-7xl mx-auto px-6 lg:px-12 py-16">
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          <Fact label="Registered" value="RC-2372959" />
          <Fact label="Date of Registration" value="6 November 2015" />
          <Fact label="Founding Body" value="Evangelical Church International, Sokoto" />
          <Fact label="Postal Address" value="P.M.B 3848, Sokoto" />
        </div>
      </section>

      <section className="max-w-7xl mx-auto px-6 lg:px-12 pb-20 lg:pb-24">
        <div className="grid grid-cols-1 md:grid-cols-2 gap-8">
          <div className="bg-white rounded-3xl border border-surface-200 p-8 lg:p-10">
            <div className="w-12 h-12 rounded-2xl bg-brand-50 grid place-items-center mb-6">
              <Target className="w-5 h-5 text-brand-700" strokeWidth={1.8} />
            </div>
            <p className="text-[11px] tracking-[0.25em] uppercase text-surface-500 font-semibold mb-3">
              Our mission
            </p>
            <h2 className="font-serif text-2xl text-surface-950 leading-snug">
              To develop intelligent, disciplined, and God-fearing young people
              equipped for lifelong learning.
            </h2>
            <p className="mt-5 text-[14px] text-surface-600 leading-relaxed">
              We provide a rigorous, balanced education that honours both the mind
              and the character - preparing each pupil for the examinations of
              life, not just the examinations of the term.
            </p>
          </div>

          <div className="bg-white rounded-3xl border border-surface-200 p-8 lg:p-10">
            <div className="w-12 h-12 rounded-2xl bg-flame-50 grid place-items-center mb-6">
              <Eye className="w-5 h-5 text-flame-700" strokeWidth={1.8} />
            </div>
            <p className="text-[11px] tracking-[0.25em] uppercase text-surface-500 font-semibold mb-3">
              Our vision
            </p>
            <h2 className="font-serif text-2xl text-surface-950 leading-snug">
              To be Sokoto State's most trusted school for building intelligent
              leaders.
            </h2>
            <p className="mt-5 text-[14px] text-surface-600 leading-relaxed">
              A school where admissions are earned, not bought. Where every
              graduate leaves knowing how to think, how to work, and how to serve
              their community.
            </p>
          </div>
        </div>
      </section>

      <section className="bg-surface-100/70 border-y border-surface-200">
        <div className="max-w-7xl mx-auto px-6 lg:px-12 py-20 lg:py-24">
          <div className="mb-12 max-w-2xl">
            <p className="text-[12px] tracking-[0.25em] uppercase text-brand-600 font-semibold mb-3">
              What we stand for
            </p>
            <h2 className="font-serif text-3xl sm:text-4xl text-surface-950 tracking-tight">
              Three words we live by.
            </h2>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            {values.map((v) => {
              const Icon = v.icon;
              return (
                <div key={v.title} className="bg-white rounded-3xl border border-surface-200 p-8">
                  <div className="w-12 h-12 rounded-2xl bg-brand-50 grid place-items-center mb-6">
                    <Icon className="w-5 h-5 text-brand-700" strokeWidth={1.8} />
                  </div>
                  <h3 className="font-serif text-2xl text-surface-950">{v.title}</h3>
                  <p className="mt-4 text-[14px] text-surface-600 leading-relaxed">{v.body}</p>
                </div>
              );
            })}
          </div>
        </div>
      </section>

      <section className="max-w-7xl mx-auto px-6 lg:px-12 py-20 lg:py-28">
        <div className="mb-12 max-w-2xl">
          <p className="text-[12px] tracking-[0.25em] uppercase text-brand-600 font-semibold mb-3">
            Our story
          </p>
          <h2 className="font-serif text-3xl sm:text-4xl text-surface-950 tracking-tight">
            Ten years, one direction.
          </h2>
        </div>

        <div className="relative">
          <div className="absolute left-[7px] sm:left-[9px] top-3 bottom-3 w-px bg-surface-300" />
          <div className="space-y-12">
            {timeline.map((t) => (
              <div key={t.year} className="relative pl-10 sm:pl-14">
                <div className="absolute left-0 top-1.5 w-4 h-4 sm:w-5 sm:h-5 rounded-full bg-flame-400 border-4 border-surface-50" />
                <p className="font-serif text-[22px] text-flame-600">{t.year}</p>
                <h3 className="mt-1 font-serif text-[22px] text-surface-950 leading-snug">
                  {t.title}
                </h3>
                <p className="mt-3 text-[14px] text-surface-600 leading-relaxed max-w-2xl">
                  {t.body}
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
              Campus
            </p>
            <h2 className="font-serif text-3xl sm:text-4xl text-surface-950 tracking-tight">
              A campus built for learning.
            </h2>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-5">
            {facilities.map((f) => {
              const Icon = f.icon;
              return (
                <div key={f.name} className="bg-white rounded-2xl border border-surface-200 p-6">
                  <div className="w-11 h-11 rounded-xl bg-brand-50 grid place-items-center mb-5">
                    <Icon className="w-5 h-5 text-brand-700" strokeWidth={1.8} />
                  </div>
                  <h3 className="font-serif text-[18px] text-surface-950 leading-snug">
                    {f.name}
                  </h3>
                  <p className="mt-2 text-[13px] text-surface-600 leading-relaxed">{f.body}</p>
                </div>
              );
            })}
          </div>
        </div>
      </section>

      <section className="max-w-7xl mx-auto px-6 lg:px-12 py-20 lg:py-28">
        <div className="mb-12 max-w-2xl">
          <p className="text-[12px] tracking-[0.25em] uppercase text-brand-600 font-semibold mb-3">
            Leadership
          </p>
          <h2 className="font-serif text-3xl sm:text-4xl text-surface-950 tracking-tight">
            The people in charge.
          </h2>
        </div>

        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-6">
          {leadership.map((p) => (
            <div key={p.name} className="bg-white rounded-2xl border border-surface-200 p-6">
              <div className="w-16 h-16 rounded-2xl bg-surface-950 text-white grid place-items-center mb-5">
                <span className="font-serif text-[20px]">{p.initials}</span>
              </div>
              <h3 className="font-serif text-[17px] text-surface-950 leading-snug">{p.name}</h3>
              <p className="mt-1 text-[11px] tracking-wider uppercase text-flame-700 font-semibold">
                {p.role}
              </p>
              <p className="mt-4 text-[12px] text-surface-600 leading-relaxed">{p.bio}</p>
            </div>
          ))}
        </div>
      </section>

      <section className="bg-surface-950 text-white">
        <div className="max-w-5xl mx-auto px-6 lg:px-12 py-20 lg:py-28 text-center">
          <Quote className="w-10 h-10 text-flame-400 mx-auto mb-8 opacity-60" />
          <blockquote className="font-serif text-2xl sm:text-3xl lg:text-4xl leading-snug tracking-tight">
            "A child educated only at school is an uneducated child. At DIBA, we
            partner with the home to build the whole person."
          </blockquote>
          <p className="mt-8 text-white/60 text-[12px] tracking-[0.25em] uppercase font-medium">
            The Management - Dominion Intelligent Builders' Academy
          </p>
        </div>
      </section>

      <section className="max-w-7xl mx-auto px-6 lg:px-12 py-20 lg:py-24">
        <div className="text-center max-w-2xl mx-auto">
          <h2 className="font-serif text-3xl sm:text-4xl text-surface-950 tracking-tight leading-tight">
            Come and see for yourself.
          </h2>
          <p className="mt-4 text-[15px] text-surface-600 leading-relaxed">
            Nothing beats walking the corridors, meeting our teachers, and watching
            a class in session. Tours run every weekday at 10am.
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
              Book a tour
            </Link>
          </div>
        </div>
      </section>
    </>
  );
}

function Fact({ label, value }: { label: string; value: string }) {
  return (
    <div className="bg-white rounded-2xl border border-surface-200 p-5">
      <p className="text-[11px] tracking-[0.2em] uppercase text-surface-400 font-semibold">
        {label}
      </p>
      <p className="mt-2 text-[14px] font-medium text-surface-950 leading-snug">
        {value}
      </p>
    </div>
  );
}