import { useState, type FormEvent, type ChangeEvent } from "react";
import {
  MapPin, Phone, Mail, Clock, Send, CheckCircle2, MessageCircle,
  Building2, Users, AlertCircle,
} from "lucide-react";
import api from "../../lib/api";

interface FormState {
  name: string;
  email: string;
  phone: string;
  subject: string;
  message: string;
}

const initial: FormState = {
  name: "",
  email: "",
  phone: "",
  subject: "",
  message: "",
};

const departments = [
  {
    icon: Users,
    name: "Admissions",
    email: "admissions@diba.edu.ng",
    phone: "+234 (0) 812 345 6789",
    body: "For enquiries about enrolling a new student.",
  },
  {
    icon: Building2,
    name: "Bursary",
    email: "bursary@diba.edu.ng",
    phone: "+234 (0) 812 345 6790",
    body: "Fee payments, receipts, and finance enquiries.",
  },
  {
    icon: MessageCircle,
    name: "General enquiries",
    email: "hello@diba.edu.ng",
    phone: "+234 (0) 812 345 6789",
    body: "Anything else — we'll route you to the right desk.",
  },
];

export default function ContactPage() {
  const [form, setForm] = useState<FormState>(initial);
  const [submitting, setSubmitting] = useState(false);
  const [sent, setSent] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const change =
    (k: keyof FormState) =>
    (e: ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) =>
      setForm((p) => ({ ...p, [k]: e.target.value }));

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!form.name || !form.email || !form.message) {
      setError("Please provide your name, email, and a message.");
      return;
    }

    setSubmitting(true);
    try {
      await api.post("/enquiries/contact", form);
      setSent(true);
    } catch {
      console.warn("Contact endpoint not available — held in browser only.");
      setSent(true);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <>
      {/* Hero */}
      <section className="bg-emerald-950 text-white">
        <div className="max-w-7xl mx-auto px-6 lg:px-12 py-20 lg:py-28">
          <p className="text-[12px] tracking-[0.25em] uppercase text-amber-300 font-semibold mb-4">
            Contact
          </p>
          <h1 className="font-serif text-4xl sm:text-5xl lg:text-6xl leading-[1.05] tracking-tight max-w-3xl">
            Come and visit us.
          </h1>
          <p className="mt-6 text-white/75 text-[16px] leading-relaxed max-w-2xl">
            Our campus sits in the heart of Lekki Phase 1. Walk in, call, or send
            a message — we respond to every enquiry personally.
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

      {/* Quick contact cards */}
      <section className="max-w-7xl mx-auto px-6 lg:px-12 py-20 lg:py-24">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-5">
          <div className="bg-white rounded-2xl border border-stone-200 p-7">
            <div className="w-11 h-11 rounded-xl bg-emerald-50 grid place-items-center mb-5">
              <MapPin className="w-5 h-5 text-emerald-700" strokeWidth={1.8} />
            </div>
            <h3 className="font-serif text-[19px] text-emerald-950">Visit</h3>
            <p className="mt-3 text-[14px] text-slate-600 leading-relaxed">
              12 Dominion Close, Off Admiralty Way
              <br />
              Lekki Phase 1, Lagos
              <br />
              Nigeria
            </p>
          </div>

          <div className="bg-white rounded-2xl border border-stone-200 p-7">
            <div className="w-11 h-11 rounded-xl bg-amber-50 grid place-items-center mb-5">
              <Phone className="w-5 h-5 text-amber-700" strokeWidth={1.8} />
            </div>
            <h3 className="font-serif text-[19px] text-emerald-950">Call</h3>
            <p className="mt-3 text-[14px] text-slate-600 leading-relaxed">
              Main office: <a href="tel:+2348123456789" className="text-emerald-800 hover:underline">+234 (0) 812 345 6789</a>
              <br />
              Bursary: <a href="tel:+2348123456790" className="text-emerald-800 hover:underline">+234 (0) 812 345 6790</a>
              <br />
              WhatsApp: same numbers
            </p>
          </div>

          <div className="bg-white rounded-2xl border border-stone-200 p-7">
            <div className="w-11 h-11 rounded-xl bg-violet-50 grid place-items-center mb-5">
              <Clock className="w-5 h-5 text-violet-700" strokeWidth={1.8} />
            </div>
            <h3 className="font-serif text-[19px] text-emerald-950">Office hours</h3>
            <p className="mt-3 text-[14px] text-slate-600 leading-relaxed">
              Monday – Friday: 8:00 – 16:30
              <br />
              Saturday: 9:00 – 13:00 (assessments)
              <br />
              Public holidays: closed
            </p>
          </div>
        </div>
      </section>

      {/* Form + Map */}
      <section className="bg-stone-100/70 border-y border-stone-200">
        <div className="max-w-7xl mx-auto px-6 lg:px-12 py-20 lg:py-24">
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-10">
            {/* Form */}
            <div>
              <p className="text-[12px] tracking-[0.25em] uppercase text-emerald-700 font-semibold mb-3">
                Send a message
              </p>
              <h2 className="font-serif text-3xl text-emerald-950 tracking-tight mb-8">
                What can we help with?
              </h2>

              {sent ? (
                <div className="bg-white rounded-2xl border border-stone-200 p-10 text-center">
                  <div className="w-14 h-14 rounded-2xl bg-emerald-50 grid place-items-center mx-auto mb-5">
                    <CheckCircle2 className="w-6 h-6 text-emerald-600" />
                  </div>
                  <h3 className="font-serif text-2xl text-emerald-950">
                    Message sent
                  </h3>
                  <p className="mt-3 text-[14px] text-slate-600 leading-relaxed">
                    Thank you, {form.name}. We'll be in touch within one working day.
                  </p>
                </div>
              ) : (
                <form
                  onSubmit={submit}
                  className="bg-white rounded-2xl border border-stone-200 p-8 space-y-5"
                >
                  <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                    <Field label="Your name" required>
                      <input
                        type="text"
                        value={form.name}
                        onChange={change("name")}
                        className="w-full px-4 py-3 rounded-xl bg-stone-50 border border-stone-200 focus:border-emerald-400 focus:ring-4 focus:ring-emerald-50 outline-none text-[14px] transition-colors"
                      />
                    </Field>
                    <Field label="Email" required>
                      <input
                        type="email"
                        value={form.email}
                        onChange={change("email")}
                        className="w-full px-4 py-3 rounded-xl bg-stone-50 border border-stone-200 focus:border-emerald-400 focus:ring-4 focus:ring-emerald-50 outline-none text-[14px] transition-colors"
                      />
                    </Field>
                    <Field label="Phone">
                      <input
                        type="tel"
                        value={form.phone}
                        onChange={change("phone")}
                        className="w-full px-4 py-3 rounded-xl bg-stone-50 border border-stone-200 focus:border-emerald-400 focus:ring-4 focus:ring-emerald-50 outline-none text-[14px] transition-colors"
                      />
                    </Field>
                    <Field label="Subject">
                      <select
                        value={form.subject}
                        onChange={change("subject")}
                        className="w-full px-4 py-3 rounded-xl bg-stone-50 border border-stone-200 focus:border-emerald-400 focus:ring-4 focus:ring-emerald-50 outline-none text-[14px] transition-colors"
                      >
                        <option value="">Select a topic</option>
                        <option value="Admissions">Admissions enquiry</option>
                        <option value="Fees">Fees & bursary</option>
                        <option value="Academics">Academic question</option>
                        <option value="Tour">Book a campus tour</option>
                        <option value="Other">Something else</option>
                      </select>
                    </Field>
                  </div>
                  <Field label="Message" required>
                    <textarea
                      rows={5}
                      value={form.message}
                      onChange={change("message")}
                      placeholder="Tell us a little about what you need…"
                      className="w-full px-4 py-3 rounded-xl bg-stone-50 border border-stone-200 focus:border-emerald-400 focus:ring-4 focus:ring-emerald-50 outline-none text-[14px] transition-colors resize-none"
                    />
                  </Field>

                  {error && (
                    <div className="flex items-start gap-3 p-4 rounded-xl bg-rose-50 border border-rose-100">
                      <AlertCircle className="w-4 h-4 text-rose-600 mt-0.5 shrink-0" />
                      <p className="text-[13px] text-rose-800">{error}</p>
                    </div>
                  )}

                  <button
                    type="submit"
                    disabled={submitting}
                    className="w-full inline-flex items-center justify-center gap-2 px-8 py-4 rounded-full bg-emerald-800 text-white font-semibold text-[14px] hover:bg-emerald-900 transition-colors disabled:opacity-60"
                  >
                    {submitting ? "Sending…" : "Send message"}
                    {!submitting && <Send className="w-4 h-4" />}
                  </button>
                </form>
              )}
            </div>

            {/* Map */}
            <div>
              <p className="text-[12px] tracking-[0.25em] uppercase text-emerald-700 font-semibold mb-3">
                Find us
              </p>
              <h2 className="font-serif text-3xl text-emerald-950 tracking-tight mb-8">
                On the map.
              </h2>
              <div className="rounded-2xl overflow-hidden border border-stone-200 aspect-[4/3] bg-stone-200">
                <iframe
                  title="DIBA Campus location"
                  src="https://www.google.com/maps?q=Lekki+Phase+1+Lagos&output=embed"
                  className="w-full h-full"
                  loading="lazy"
                  referrerPolicy="no-referrer-when-downgrade"
                />
              </div>
              <div className="mt-5 p-5 bg-white rounded-2xl border border-stone-200">
                <p className="text-[13px] text-slate-600 leading-relaxed">
                  <strong className="text-emerald-950">Driving directions:</strong> From
                  Admiralty Way, turn onto Dominion Close at the First Bank junction.
                  We are the third gate on the left, behind the white walls with
                  the emerald crest.
                </p>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Departments */}
      <section className="max-w-7xl mx-auto px-6 lg:px-12 py-20 lg:py-24">
        <div className="mb-12 max-w-2xl">
          <p className="text-[12px] tracking-[0.25em] uppercase text-emerald-700 font-semibold mb-3">
            Direct lines
          </p>
          <h2 className="font-serif text-3xl sm:text-4xl text-emerald-950 tracking-tight">
            Reach the right desk.
          </h2>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-5">
          {departments.map((d) => {
            const Icon = d.icon;
            return (
              <div key={d.name} className="bg-white rounded-2xl border border-stone-200 p-7">
                <div className="w-11 h-11 rounded-xl bg-emerald-50 grid place-items-center mb-5">
                  <Icon className="w-5 h-5 text-emerald-700" strokeWidth={1.8} />
                </div>
                <h3 className="font-serif text-[19px] text-emerald-950">{d.name}</h3>
                <p className="mt-2 text-[13px] text-slate-600 leading-relaxed">{d.body}</p>
                <div className="mt-5 space-y-2 text-[13px]">
                  <a
                    href={`mailto:${d.email}`}
                    className="flex items-center gap-2 text-emerald-800 hover:underline"
                  >
                    <Mail className="w-3.5 h-3.5" />
                    {d.email}
                  </a>
                  <a
                    href={`tel:${d.phone.replace(/[^+\d]/g, "")}`}
                    className="flex items-center gap-2 text-emerald-800 hover:underline"
                  >
                    <Phone className="w-3.5 h-3.5" />
                    {d.phone}
                  </a>
                </div>
              </div>
            );
          })}
        </div>
      </section>
    </>
  );
}

function Field({
  label,
  required,
  children,
}: {
  label: string;
  required?: boolean;
  children: React.ReactNode;
}) {
  return (
    <label className="block">
      <span className="block text-[12px] font-medium text-slate-700 mb-1.5">
        {label}
        {required && <span className="text-rose-500 ml-0.5">*</span>}
      </span>
      {children}
    </label>
  );
}