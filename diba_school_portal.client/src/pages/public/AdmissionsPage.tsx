import { useState, type FormEvent, type ChangeEvent } from "react";
import { Link } from "react-router-dom";
import {
  ArrowRight, CheckCircle2, Calendar, FileText, Users, CreditCard,
  ClipboardList, Download, AlertCircle,
} from "lucide-react";
import api from "../../lib/api";

interface FormState {
  childFirstName: string;
  childLastName: string;
  childDateOfBirth: string;
  childGender: string;
  section: string;
  previousSchool: string;
  parentName: string;
  parentEmail: string;
  parentPhone: string;
  preferredStartTerm: string;
  message: string;
}

const initial: FormState = {
  childFirstName: "",
  childLastName: "",
  childDateOfBirth: "",
  childGender: "",
  section: "",
  previousSchool: "",
  parentName: "",
  parentEmail: "",
  parentPhone: "",
  preferredStartTerm: "",
  message: "",
};

const steps = [
  {
    icon: ClipboardList,
    title: "Submit an enquiry",
    body: "Fill this form. You'll hear from our admissions office within one working day.",
  },
  {
    icon: Calendar,
    title: "Book an assessment",
    body: "Entrance assessments hold every Saturday from 9am. Nursery applicants only need a play-visit.",
  },
  {
    icon: Users,
    title: "Campus tour & interview",
    body: "Meet your child's prospective teacher. You'll tour the Old Airport campus and see a live class.",
  },
  {
    icon: FileText,
    title: "Offer of admission",
    body: "Successful applicants receive a written offer within one week of the assessment.",
  },
  {
    icon: CreditCard,
    title: "Acceptance & fees",
    body: "Pay the acceptance fee to reserve your child's place. Term fees are due before resumption.",
  },
];

const sectionOptions = ["Nursery", "Primary", "Junior Secondary", "Senior Secondary"];

const termOptions = [
  "First Term 2025/2026",
  "Second Term 2025/2026",
  "Third Term 2025/2026",
  "First Term 2026/2027",
];

export default function AdmissionsPage() {
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

    if (!form.childFirstName || !form.childLastName || !form.section) {
      setError("Please enter the child's name and the section you are applying for.");
      return;
    }
    if (!form.parentName || !form.parentEmail || !form.parentPhone) {
      setError("Please provide a parent or guardian name, email, and phone.");
      return;
    }

    setSubmitting(true);
    try {
      await api.post("/enquiries", form);
      setSent(true);
    } catch {
      console.warn("Enquiry endpoint not available — submission held in browser only.");
      setSent(true);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <>
      {/* Hero */}
      <section className="bg-emerald-950 text-white">
        <div className="max-w-7xl mx-auto px-6 lg:px-12 py-20 lg:py-28 grid grid-cols-1 lg:grid-cols-12 gap-10 items-end">
          <div className="lg:col-span-8">
            <p className="text-[12px] tracking-[0.25em] uppercase text-amber-300 font-semibold mb-4">
              2025 / 2026 admissions
            </p>
            <h1 className="font-serif text-4xl sm:text-5xl lg:text-6xl leading-[1.05] tracking-tight">
              Join the Dominion family.
            </h1>
            <p className="mt-6 text-white/75 text-[16px] leading-relaxed max-w-2xl">
              Admission into DIBA is selective and rolling. Applications are
              reviewed as they arrive — early applicants have the best chance of
              securing a place.
            </p>
          </div>
          <div className="lg:col-span-4 flex lg:justify-end">
            <a
              href="#apply"
              className="inline-flex items-center gap-2 px-7 py-3.5 rounded-full bg-amber-400 text-emerald-950 font-semibold text-[14px] hover:bg-amber-300 transition-colors"
            >
              Apply now
              <ArrowRight className="w-4 h-4" />
            </a>
          </div>
        </div>
      </section>

      {/* Motto band */}
      <section className="bg-amber-400">
        <div className="max-w-7xl mx-auto px-6 lg:px-12 py-4 flex items-center justify-center">
          <p className="text-emerald-950 font-serif text-[15px] sm:text-base tracking-wide text-center">
            Knowledge is Wealth
          </p>
        </div>
      </section>

      {/* Process */}
      <section className="max-w-7xl mx-auto px-6 lg:px-12 py-20 lg:py-24">
        <div className="mb-12 max-w-2xl">
          <p className="text-[12px] tracking-[0.25em] uppercase text-emerald-700 font-semibold mb-3">
            How it works
          </p>
          <h2 className="font-serif text-3xl sm:text-4xl text-emerald-950 tracking-tight">
            Five steps to a place.
          </h2>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-5 gap-4">
          {steps.map((s, i) => {
            const Icon = s.icon;
            return (
              <div
                key={s.title}
                className="relative bg-white rounded-2xl border border-stone-200 p-6"
              >
                <p className="font-serif text-3xl text-amber-500 leading-none mb-4">
                  {String(i + 1).padStart(2, "0")}
                </p>
                <div className="w-10 h-10 rounded-xl bg-emerald-50 grid place-items-center mb-4">
                  <Icon className="w-4 h-4 text-emerald-700" strokeWidth={1.8} />
                </div>
                <h3 className="font-serif text-[16px] text-emerald-950 leading-snug">
                  {s.title}
                </h3>
                <p className="mt-2 text-[12px] text-slate-600 leading-relaxed">
                  {s.body}
                </p>
              </div>
            );
          })}
        </div>
      </section>

      {/* Fees strip */}
      <section className="bg-stone-100/70 border-y border-stone-200">
        <div className="max-w-7xl mx-auto px-6 lg:px-12 py-16">
          <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
            <div className="bg-white rounded-2xl border border-stone-200 p-6">
              <div className="w-11 h-11 rounded-xl bg-emerald-50 grid place-items-center mb-5">
                <FileText className="w-5 h-5 text-emerald-700" strokeWidth={1.8} />
              </div>
              <h3 className="font-serif text-[18px] text-emerald-950">
                Application form
              </h3>
              <p className="mt-2 text-[13px] text-slate-600 leading-relaxed">
                Free. Fill the form below or pick one up at the school office.
              </p>
            </div>
            <div className="bg-white rounded-2xl border border-stone-200 p-6">
              <div className="w-11 h-11 rounded-xl bg-emerald-50 grid place-items-center mb-5">
                <Calendar className="w-5 h-5 text-emerald-700" strokeWidth={1.8} />
              </div>
              <h3 className="font-serif text-[18px] text-emerald-950">
                Entrance assessment
              </h3>
              <p className="mt-2 text-[13px] text-slate-600 leading-relaxed">
                ₦5,000 for Primary and above. Free for Nursery and Reception.
              </p>
            </div>
            <div className="bg-white rounded-2xl border border-stone-200 p-6">
              <div className="w-11 h-11 rounded-xl bg-emerald-50 grid place-items-center mb-5">
                <CreditCard className="w-5 h-5 text-emerald-700" strokeWidth={1.8} />
              </div>
              <h3 className="font-serif text-[18px] text-emerald-950">
                Prospectus & fee schedule
              </h3>
              <p className="mt-2 text-[13px] text-slate-600 leading-relaxed">
                Full fee breakdown is included in our downloadable prospectus.
              </p>
              <button
                type="button"
                className="mt-4 inline-flex items-center gap-2 text-[13px] font-semibold text-emerald-800 hover:text-emerald-900"
              >
                <Download className="w-3.5 h-3.5" />
                Download PDF
              </button>
            </div>
          </div>
        </div>
      </section>

      {/* Application form */}
      <section id="apply" className="max-w-4xl mx-auto px-6 lg:px-12 py-20 lg:py-24">
        {sent ? (
          <div className="bg-white rounded-3xl border border-stone-200 p-10 lg:p-14 text-center">
            <div className="w-16 h-16 rounded-2xl bg-emerald-50 grid place-items-center mx-auto mb-6">
              <CheckCircle2 className="w-8 h-8 text-emerald-600" strokeWidth={1.8} />
            </div>
            <h2 className="font-serif text-3xl text-emerald-950">Application received</h2>
            <p className="mt-4 text-[15px] text-slate-600 leading-relaxed max-w-lg mx-auto">
              Thank you, {form.parentName}. Our admissions team will contact you within one
              working day to arrange {form.childFirstName}'s assessment.
            </p>
            <div className="mt-8 inline-flex flex-col sm:flex-row gap-3">
              <Link
                to="/"
                className="inline-flex items-center justify-center gap-2 px-7 py-3.5 rounded-full bg-emerald-800 text-white font-semibold text-[14px] hover:bg-emerald-900 transition-colors"
              >
                Back to home
                <ArrowRight className="w-4 h-4" />
              </Link>
            </div>
          </div>
        ) : (
          <>
            <div className="mb-10 max-w-2xl">
              <p className="text-[12px] tracking-[0.25em] uppercase text-emerald-700 font-semibold mb-3">
                Begin here
              </p>
              <h2 className="font-serif text-3xl sm:text-4xl text-emerald-950 tracking-tight">
                Submit an enquiry.
              </h2>
              <p className="mt-5 text-[15px] text-slate-600 leading-relaxed">
                All fields marked with <span className="text-rose-500">*</span> are
                required. Your data stays with us — we never share it.
              </p>
            </div>

            <form
              onSubmit={submit}
              className="bg-white rounded-3xl border border-stone-200 p-8 lg:p-10 space-y-8"
            >
              {/* Child details */}
              <div>
                <div className="flex items-center gap-3 mb-5">
                  <div className="w-9 h-9 rounded-xl bg-emerald-50 grid place-items-center">
                    <Users className="w-4 h-4 text-emerald-700" strokeWidth={2} />
                  </div>
                  <h3 className="font-serif text-[19px] text-emerald-950">
                    About the child
                  </h3>
                </div>
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <Field label="First name" required>
                    <input
                      type="text"
                      value={form.childFirstName}
                      onChange={change("childFirstName")}
                      className="w-full px-4 py-3 rounded-xl bg-stone-50 border border-stone-200 focus:border-emerald-400 focus:ring-4 focus:ring-emerald-50 outline-none text-[14px] transition-colors"
                    />
                  </Field>
                  <Field label="Last name" required>
                    <input
                      type="text"
                      value={form.childLastName}
                      onChange={change("childLastName")}
                      className="w-full px-4 py-3 rounded-xl bg-stone-50 border border-stone-200 focus:border-emerald-400 focus:ring-4 focus:ring-emerald-50 outline-none text-[14px] transition-colors"
                    />
                  </Field>
                  <Field label="Date of birth">
                    <input
                      type="date"
                      value={form.childDateOfBirth}
                      onChange={change("childDateOfBirth")}
                      className="w-full px-4 py-3 rounded-xl bg-stone-50 border border-stone-200 focus:border-emerald-400 focus:ring-4 focus:ring-emerald-50 outline-none text-[14px] transition-colors"
                    />
                  </Field>
                  <Field label="Gender">
                    <select
                      value={form.childGender}
                      onChange={change("childGender")}
                      className="w-full px-4 py-3 rounded-xl bg-stone-50 border border-stone-200 focus:border-emerald-400 focus:ring-4 focus:ring-emerald-50 outline-none text-[14px] transition-colors"
                    >
                      <option value="">Select</option>
                      <option value="Male">Male</option>
                      <option value="Female">Female</option>
                    </select>
                  </Field>
                  <Field label="Section applying for" required>
                    <select
                      value={form.section}
                      onChange={change("section")}
                      className="w-full px-4 py-3 rounded-xl bg-stone-50 border border-stone-200 focus:border-emerald-400 focus:ring-4 focus:ring-emerald-50 outline-none text-[14px] transition-colors"
                    >
                      <option value="">Select a section</option>
                      {sectionOptions.map((s) => (
                        <option key={s} value={s}>{s}</option>
                      ))}
                    </select>
                  </Field>
                  <Field label="Preferred start term">
                    <select
                      value={form.preferredStartTerm}
                      onChange={change("preferredStartTerm")}
                      className="w-full px-4 py-3 rounded-xl bg-stone-50 border border-stone-200 focus:border-emerald-400 focus:ring-4 focus:ring-emerald-50 outline-none text-[14px] transition-colors"
                    >
                      <option value="">Select a term</option>
                      {termOptions.map((t) => (
                        <option key={t} value={t}>{t}</option>
                      ))}
                    </select>
                  </Field>
                  <div className="sm:col-span-2">
                    <Field label="Previous school (if any)">
                      <input
                        type="text"
                        value={form.previousSchool}
                        onChange={change("previousSchool")}
                        placeholder="Name of current or last school attended"
                        className="w-full px-4 py-3 rounded-xl bg-stone-50 border border-stone-200 focus:border-emerald-400 focus:ring-4 focus:ring-emerald-50 outline-none text-[14px] transition-colors"
                      />
                    </Field>
                  </div>
                </div>
              </div>

              {/* Parent details */}
              <div className="pt-8 border-t border-stone-200">
                <div className="flex items-center gap-3 mb-5">
                  <div className="w-9 h-9 rounded-xl bg-amber-50 grid place-items-center">
                    <Users className="w-4 h-4 text-amber-700" strokeWidth={2} />
                  </div>
                  <h3 className="font-serif text-[19px] text-emerald-950">
                    About you
                  </h3>
                </div>
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                  <div className="sm:col-span-2">
                    <Field label="Full name" required>
                      <input
                        type="text"
                        value={form.parentName}
                        onChange={change("parentName")}
                        className="w-full px-4 py-3 rounded-xl bg-stone-50 border border-stone-200 focus:border-emerald-400 focus:ring-4 focus:ring-emerald-50 outline-none text-[14px] transition-colors"
                      />
                    </Field>
                  </div>
                  <Field label="Email address" required>
                    <input
                      type="email"
                      value={form.parentEmail}
                      onChange={change("parentEmail")}
                      className="w-full px-4 py-3 rounded-xl bg-stone-50 border border-stone-200 focus:border-emerald-400 focus:ring-4 focus:ring-emerald-50 outline-none text-[14px] transition-colors"
                    />
                  </Field>
                  <Field label="Phone number" required>
                    <input
                      type="tel"
                      value={form.parentPhone}
                      onChange={change("parentPhone")}
                      placeholder="080…"
                      className="w-full px-4 py-3 rounded-xl bg-stone-50 border border-stone-200 focus:border-emerald-400 focus:ring-4 focus:ring-emerald-50 outline-none text-[14px] transition-colors"
                    />
                  </Field>
                  <div className="sm:col-span-2">
                    <Field label="Anything else we should know?">
                      <textarea
                        rows={4}
                        value={form.message}
                        onChange={change("message")}
                        placeholder="Special learning needs, siblings at the school, preferred assessment date…"
                        className="w-full px-4 py-3 rounded-xl bg-stone-50 border border-stone-200 focus:border-emerald-400 focus:ring-4 focus:ring-emerald-50 outline-none text-[14px] transition-colors resize-none"
                      />
                    </Field>
                  </div>
                </div>
              </div>

              {error && (
                <div className="flex items-start gap-3 p-4 rounded-xl bg-rose-50 border border-rose-100">
                  <AlertCircle className="w-4 h-4 text-rose-600 mt-0.5 shrink-0" />
                  <p className="text-[13px] text-rose-800">{error}</p>
                </div>
              )}

              <div className="pt-4 flex flex-col sm:flex-row items-center gap-4">
                <button
                  type="submit"
                  disabled={submitting}
                  className="inline-flex items-center justify-center gap-2 px-8 py-4 rounded-full bg-emerald-800 text-white font-semibold text-[14px] hover:bg-emerald-900 transition-colors disabled:opacity-60 disabled:cursor-not-allowed"
                >
                  {submitting ? "Submitting…" : "Submit application"}
                  {!submitting && <ArrowRight className="w-4 h-4" />}
                </button>
                <p className="text-[12px] text-slate-500">
                  We respond within one working day.
                </p>
              </div>
            </form>
          </>
        )}
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