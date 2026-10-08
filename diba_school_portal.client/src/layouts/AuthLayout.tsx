import { Outlet } from "react-router-dom";
import { GraduationCap, BookOpen, Users, Wallet } from "lucide-react";

export default function AuthLayout() {
  return (
    <div className="min-h-screen flex bg-slate-50">
      {/* Brand panel */}
      <div className="hidden lg:flex lg:w-1/2 bg-gradient-to-br from-emerald-600 via-emerald-700 to-emerald-900 p-12 text-white relative overflow-hidden">
        <div className="absolute -top-32 -right-32 w-96 h-96 rounded-full bg-white/5" />
        <div className="absolute bottom-0 -left-24 w-80 h-80 rounded-full bg-white/5" />

        <div className="relative z-10 flex flex-col justify-between w-full">
          <div className="flex items-center gap-3">
            <div className="w-11 h-11 rounded-2xl bg-white/15 backdrop-blur grid place-items-center">
              <GraduationCap className="w-5 h-5" />
            </div>
            <div>
              <p className="text-[13px] font-semibold">DIBA School</p>
              <p className="text-[11px] text-white/70">Portal</p>
            </div>
          </div>

          <div>
            <h1 className="text-4xl font-semibold leading-tight tracking-tight">
              Modern school
              <br />
              management.
            </h1>
            <p className="mt-4 text-white/70 max-w-sm text-[14px] leading-relaxed">
              One portal for students, staff, and parents — attendance, results,
              fees, and payments.
            </p>

            <div className="mt-12 grid grid-cols-3 gap-6 max-w-sm">
              <Stat icon={Users} label="Students" value="1,200+" />
              <Stat icon={BookOpen} label="Classes" value="36" />
              <Stat icon={Wallet} label="Collected" value="₦42M" />
            </div>
          </div>

          <p className="text-[11px] text-white/50">
            © {new Date().getFullYear()} DIBA School Portal
          </p>
        </div>
      </div>

      {/* Form panel */}
      <div className="flex-1 flex items-center justify-center p-6">
        <div className="w-full max-w-md">
          <Outlet />
        </div>
      </div>
    </div>
  );
}

function Stat({
  icon: Icon,
  label,
  value,
}: {
  icon: typeof Users;
  label: string;
  value: string;
}) {
  return (
    <div>
      <Icon className="w-4 h-4 text-white/60 mb-2" />
      <p className="text-xl font-semibold">{value}</p>
      <p className="text-[11px] text-white/60 mt-0.5">{label}</p>
    </div>
  );
}