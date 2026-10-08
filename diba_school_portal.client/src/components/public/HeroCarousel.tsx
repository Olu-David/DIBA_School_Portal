import { useCallback, useEffect, useRef, useState } from "react";
import { Link } from "react-router-dom";
import {
  ArrowRight, ChevronLeft, ChevronRight, Pause, Play,
} from "lucide-react";

interface Slide {
  eyebrow: string;
  title: string;
  accent: string;
  description: string;
  ctaLabel: string;
  ctaTo: string;
  secondary?: { label: string; to: string };
  image: string;
  tone: "emerald" | "amber" | "sky" | "violet";
}

const slides: Slide[] = [
  {
    eyebrow: "Knowledge · Character · Service",
    title: "Building intelligent leaders of tomorrow",
    accent: "one curious mind at a time",
    description:
      "Dominion Intelligent Builders Academy is a co-educational school in Lagos shaping confident, capable, and God-fearing young people from Nursery through Senior Secondary.",
    ctaLabel: "Apply for 2025/2026",
    ctaTo: "/admissions",
    secondary: { label: "Take a campus tour", to: "/contact" },
    image:
      "https://images.unsplash.com/photo-1503676260728-1c00da094a0b?auto=format&fit=crop&w=1920&q=80",
    tone: "emerald",
  },
  {
    eyebrow: "Admissions open",
    title: "Your child's next chapter",
    accent: "starts here",
    description:
      "Limited places available across Nursery, Primary, Junior and Senior Secondary. Entrance assessments hold every Saturday — book a slot online.",
    ctaLabel: "Begin application",
    ctaTo: "/admissions",
    secondary: { label: "Download prospectus", to: "/contact" },
    image:
      "https://images.unsplash.com/photo-1497633762265-9d179a990aa6?auto=format&fit=crop&w=1920&q=80",
    tone: "amber",
  },
  {
    eyebrow: "Academic excellence",
    title: "Results that open doors",
    accent: "year after year",
    description:
      "98% of our WAEC and NECO candidates earned credits in 5+ subjects including English and Mathematics. Six Dominion graduates are studying at Ivy League and Russell Group universities.",
    ctaLabel: "See our results",
    ctaTo: "/about",
    secondary: { label: "Meet the faculty", to: "/about" },
    image:
      "https://images.unsplash.com/photo-1580582932707-520aed937b7b?auto=format&fit=crop&w=1920&q=80",
    tone: "sky",
  },
  {
    eyebrow: "Facilities",
    title: "A campus built for curious minds",
    accent: "and growing bodies",
    description:
      "Science labs, a 400-seat hall, libraries, ICT suite, sports field, sick bay, and a working farm. Every space at DIBA is designed for learning, play, and discovery.",
    ctaLabel: "Explore campus",
    ctaTo: "/about",
    secondary: { label: "Talk to us", to: "/contact" },
    image:
      "https://images.unsplash.com/photo-1523050854058-8df90110c9f1?auto=format&fit=crop&w=1920&q=80",
    tone: "violet",
  },
];

const toneRing: Record<Slide["tone"], string> = {
  emerald: "bg-emerald-500",
  amber: "bg-amber-500",
  sky: "bg-sky-500",
  violet: "bg-violet-500",
};

export default function HeroCarousel() {
  const [index, setIndex] = useState(0);
  const [playing, setPlaying] = useState(true);
  const timer = useRef<number | null>(null);

  const go = useCallback((i: number) => {
    setIndex(((i % slides.length) + slides.length) % slides.length);
  }, []);

  const next = useCallback(() => go(index + 1), [go, index]);
  const prev = useCallback(() => go(index - 1), [go, index]);

  useEffect(() => {
    if (!playing) return;
    timer.current = window.setInterval(next, 6500);
    return () => {
      if (timer.current) window.clearInterval(timer.current);
    };
  }, [playing, next]);

  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === "ArrowRight") next();
      if (e.key === "ArrowLeft") prev();
    };
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [next, prev]);

  return (
    <section
      className="relative h-[88vh] min-h-[620px] w-full overflow-hidden bg-slate-900"
      onMouseEnter={() => setPlaying(false)}
      onMouseLeave={() => setPlaying(true)}
      aria-roledescription="carousel"
      aria-label="Featured"
    >
      {slides.map((s, i) => (
        <div
          key={i}
          className={`absolute inset-0 transition-opacity duration-1000 ${
            i === index ? "opacity-100 z-10" : "opacity-0 z-0"
          }`}
          aria-hidden={i !== index}
        >
          <img
            src={s.image}
            alt=""
            className="absolute inset-0 w-full h-full object-cover"
            loading={i === 0 ? "eager" : "lazy"}
          />
          <div className="absolute inset-0 bg-gradient-to-r from-emerald-950/95 via-emerald-950/75 to-emerald-950/30" />
          <div className="absolute inset-0 bg-gradient-to-t from-black/60 via-transparent to-transparent" />
        </div>
      ))}

      <div className="relative z-20 h-full max-w-7xl mx-auto px-6 lg:px-12 flex items-center">
        <div className="max-w-2xl text-white">
          <div className="flex items-center gap-3 mb-6">
            <span className={`h-1 w-10 rounded-full ${toneRing[slides[index].tone]}`} />
            <p className="text-[12px] tracking-[0.25em] uppercase text-white/80 font-medium">
              {slides[index].eyebrow}
            </p>
          </div>

          <h1 className="font-serif text-4xl sm:text-5xl lg:text-6xl leading-[1.05] tracking-tight">
            {slides[index].title}{" "}
            <span className="italic text-amber-300">{slides[index].accent}</span>
          </h1>

          <p className="mt-6 text-white/80 text-[15px] sm:text-base leading-relaxed max-w-xl">
            {slides[index].description}
          </p>

          <div className="mt-9 flex flex-col sm:flex-row gap-3">
            <Link
              to={slides[index].ctaTo}
              className="inline-flex items-center justify-center gap-2 px-6 py-3.5 rounded-full bg-amber-400 text-emerald-950 font-semibold text-[14px] hover:bg-amber-300 transition-colors"
            >
              {slides[index].ctaLabel}
              <ArrowRight className="w-4 h-4" />
            </Link>
            {slides[index].secondary && (
              <Link
                to={slides[index].secondary.to}
                className="inline-flex items-center justify-center gap-2 px-6 py-3.5 rounded-full border border-white/25 text-white font-medium text-[14px] hover:bg-white/10 transition-colors"
              >
                {slides[index].secondary.label}
              </Link>
            )}
          </div>
        </div>
      </div>

      {/* Controls */}
      <div className="absolute z-30 bottom-8 left-0 right-0 max-w-7xl mx-auto px-6 lg:px-12 flex items-center justify-between">
        <div className="flex items-center gap-3">
          {slides.map((_, i) => (
            <button
              key={i}
              type="button"
              onClick={() => go(i)}
              aria-label={`Go to slide ${i + 1}`}
              className={`transition-all ${
                i === index
                  ? "w-10 h-1.5 bg-amber-400"
                  : "w-4 h-1.5 bg-white/30 hover:bg-white/60"
              } rounded-full`}
            />
          ))}
        </div>

        <div className="flex items-center gap-2">
          <button
            type="button"
            onClick={() => setPlaying((p) => !p)}
            className="w-10 h-10 rounded-full border border-white/25 text-white grid place-items-center hover:bg-white/10 transition"
            aria-label={playing ? "Pause" : "Play"}
          >
            {playing ? <Pause className="w-4 h-4" /> : <Play className="w-4 h-4" />}
          </button>
          <button
            type="button"
            onClick={prev}
            className="w-10 h-10 rounded-full border border-white/25 text-white grid place-items-center hover:bg-white/10 transition"
            aria-label="Previous slide"
          >
            <ChevronLeft className="w-4 h-4" />
          </button>
          <button
            type="button"
            onClick={next}
            className="w-10 h-10 rounded-full border border-white/25 text-white grid place-items-center hover:bg-white/10 transition"
            aria-label="Next slide"
          >
            <ChevronRight className="w-4 h-4" />
          </button>
        </div>
      </div>
    </section>
  );
}