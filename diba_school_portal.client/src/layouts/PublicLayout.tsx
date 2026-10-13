import { useState } from "react";
import { Link, Outlet, useLocation } from "react-router-dom";
import {
  Menu, X, Phone, Mail, MapPin, Globe, BookOpen, Users, Send,
  ArrowRight,
} from "lucide-react";
import SchoolCrest from "../components/public/SchoolCrest";

const nav = [
  { to: "/", label: "Home" },
  { to: "/about", label: "About" },
  { to: "/academics", label: "Academics" },
  { to: "/admissions", label: "Admissions" },
  { to: "/contact", label: "Contact" },
];

const socialLinks = [
  { href: "#", label: "Facebook", Icon: Globe },
  { href: "#", label: "Instagram", Icon: BookOpen },
  { href: "#", label: "Twitter", Icon: Users },
  { href: "#", label: "YouTube", Icon: Send },
];

export default function PublicLayout() {
  const [open, setOpen] = useState(false);
  const { pathname } = useLocation();

  return (
    <div className="min-h-screen bg-stone-50 text-slate-800 font-sans">
      {/* Utility bar */}
      <div className="bg-emerald-950 text-white/80 text-[12px]">
        <div className="max-w-7xl mx-auto px-6 lg:px-12 h-10 flex items-center justify-between">
          <div className="flex items-center gap-6">
            <span className="hidden sm:inline-flex items-center gap-1.5">
              <Phone className="w-3 h-3" /> +234 (0) 812 345 6789
            </span>
            <span className="hidden md:inline-flex items-center gap-1.5">
              <Mail className="w-3 h-3" /> hello@diba.edu.ng
            </span>
          </div>
          <div className="flex items-center gap-4">
            <Link to="/auth/student" className="hover:text-white transition">Student</Link>
            <span className="text-white/20">Â·</span>
            <Link to="/auth/parent" className="hover:text-white transition">Parent</Link>
            <span className="text-white/20">Â·</span>
            <Link to="/auth/staff" className="hover:text-white transition">Staff</Link>
          </div>
        </div>
      </div>

      {/* Header */}
      <header className="sticky top-0 z-50 bg-stone-50/90 backdrop-blur border-b border-stone-200">
        <div className="max-w-7xl mx-auto px-6 lg:px-12 h-20 flex items-center justify-between gap-6">
          <Link to="/" className="flex items-center gap-3 shrink-0">
            <SchoolCrest className="w-11 h-11" />
            <div className="leading-tight">
              <p className="font-serif text-[15px] font-semibold text-emerald-950 tracking-tight">
                Dominion Intelligent Builders
              </p>
              <p className="text-[10px] tracking-[0.3em] uppercase text-emerald-700 font-medium">
                Academy Sokoto
              </p>
            </div>
          </Link>

          <nav className="hidden lg:flex items-center gap-1">
            {nav.map((n) => {
              const active = pathname === n.to;
              return (
                <Link
                  key={n.to}
                  to={n.to}
                  className={`px-4 py-2 rounded-full text-[13px] font-medium transition-colors ${
                    active
                      ? "bg-emerald-100 text-emerald-900"
                      : "text-slate-700 hover:bg-stone-200/60"
                  }`}
                >
                  {n.label}
                </Link>
              );
            })}
          </nav>

          <div className="flex items-center gap-3">
            <Link
              to="/admissions"
              className="hidden sm:inline-flex items-center gap-2 px-5 py-2.5 rounded-full bg-emerald-800 text-white text-[13px] font-semibold hover:bg-emerald-900 transition-colors"
            >
              Apply now
              <ArrowRight className="w-3.5 h-3.5" />
            </Link>
            <button
              type="button"
              onClick={() => setOpen((v) => !v)}
              className="lg:hidden p-2 rounded-lg hover:bg-stone-200"
              aria-label="Menu"
            >
              {open ? <X className="w-5 h-5" /> : <Menu className="w-5 h-5" />}
            </button>
          </div>
        </div>

        {/* Mobile drawer */}
        {open && (
          <div className="lg:hidden border-t border-stone-200 bg-stone-50">
            <nav className="px-6 py-4 flex flex-col">
              {nav.map((n) => (
                <Link
                  key={n.to}
                  to={n.to}
                  onClick={() => setOpen(false)}
                  className={`py-3 text-[14px] font-medium border-b border-stone-200 last:border-0 ${
                    pathname === n.to ? "text-emerald-800" : "text-slate-700"
                  }`}
                >
                  {n.label}
                </Link>
              ))}
              <Link
                to="/admissions"
                onClick={() => setOpen(false)}
                className="mt-4 inline-flex items-center justify-center gap-2 px-5 py-3 rounded-full bg-emerald-800 text-white text-[13px] font-semibold"
              >
                Apply now
                <ArrowRight className="w-3.5 h-3.5" />
              </Link>
            </nav>
          </div>
        )}
      </header>

      <main>
        <Outlet />
      </main>

      {/* Footer */}
      <footer className="bg-emerald-950 text-white/70 mt-24">
        <div className="max-w-7xl mx-auto px-6 lg:px-12 pt-16 pb-10">
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-10">
            <div>
              <div className="flex items-center gap-3 mb-5">
                <SchoolCrest className="w-12 h-12" tone="white" />
                <div className="leading-tight">
                  <p className="font-serif text-[15px] font-semibold text-white">
                    Dominion Intelligent
                  </p>
                  <p className="font-serif text-[15px] font-semibold text-white">
                    Builders Academy
                  </p>
                </div>
              </div>
              <p className="text-[13px] leading-relaxed">
                Building intelligent leaders since 2010. Knowledge Â· Character Â· Service.
              </p>
              <div className="mt-5 flex items-center gap-3">
                {socialLinks.map(({ href, label, Icon }) => (
                  <a
                    key={label}
                    href={href}
                    aria-label={label}
                    className="w-9 h-9 rounded-full border border-white/15 grid place-items-center hover:bg-white/10 transition"
                  >
                    <Icon className="w-4 h-4" />
                  </a>
                ))}
              </div>
            </div>

            <div>
              <h4 className="text-white font-semibold text-[13px] tracking-wider uppercase mb-5">
                Explore
              </h4>
              <ul className="space-y-3 text-[13px]">
                <li><Link to="/about" className="hover:text-white">About the school</Link></li>
                <li><Link to="/academics" className="hover:text-white">Academics</Link></li>
                <li><Link to="/admissions" className="hover:text-white">Admissions</Link></li>
                <li><Link to="/contact" className="hover:text-white">Contact us</Link></li>
              </ul>
            </div>

            <div>
              <h4 className="text-white font-semibold text-[13px] tracking-wider uppercase mb-5">
                Portals
              </h4>
              <ul className="space-y-3 text-[13px]">
                <li><Link to="/auth/student" className="hover:text-white">Student portal</Link></li>
                <li><Link to="/auth/parent" className="hover:text-white">Parent portal</Link></li>
                <li><Link to="/auth/staff" className="hover:text-white">Staff portal</Link></li>
              </ul>
            </div>

            <div>
              <h4 className="text-white font-semibold text-[13px] tracking-wider uppercase mb-5">
                Visit us
              </h4>
              <ul className="space-y-3 text-[13px]">
                <li className="flex gap-2">
                  <MapPin className="w-4 h-4 shrink-0 mt-0.5" />
                  <span>
                    Off Trade Fair Complex, beside RCCG H/Q
                    <br />
                    Old Airport Area, Sokoto
                    <br />
                    P.M.B 3848, Sokoto State, Nigeria
                  </span>
                </li>
                <li className="flex gap-2">
                  <Phone className="w-4 h-4 shrink-0 mt-0.5" />
                  <span>+234 (0) 812 345 6789</span>
                </li>
                <li className="flex gap-2">
                  <Mail className="w-4 h-4 shrink-0 mt-0.5" />
                  <span>hello@diba.edu.ng</span>
                </li>
              </ul>
            </div>
          </div>

          <div className="mt-14 pt-6 border-t border-white/10 flex flex-col sm:flex-row items-center justify-between gap-3 text-[12px]">
            <p>Â© {new Date().getFullYear()} Dominion Intelligent Builders Academy. All rights reserved.</p>
            {/* <p className="text-white/50">CAC RC-1234567 Â· Lagos State Ministry of Education Approved</p> */}
          </div>
        </div>
      </footer>
    </div>
  );
}

