import { Outlet } from "react-router-dom";
import { BookOpen, Users, Wallet } from "lucide-react";

export default function AuthLayout() {
  return (
    <div className="min-h-screen flex bg-surface-50">
      <div className="hidden lg:flex lg:w-1/2 bg-gradient-to-br from-brand-700 via-brand-800 to-surface-950 p-12 text-white relative overflow-hidden">
        <div className="absolute -top-32 -right-32 w-96 h-96 rounded-full bg-white/5" />
        <div className="absolute bottom-0 -left-24 w-80 h-80 rounded-full bg-white/5" />

        <div className="relative z-10 flex flex-col justify-between w-full">
          <div className="flex items-center gap-3">
            <img
               src="/images/logo1.png"
              alt="Dominion Intelligent Builders Academy"
              className="w-14 h-14 object-contain"
            />
            <div className="leading-tight">
              <p className="text-[13px] font-semibold">
                Dominion Intelligent Builders
              </p>
              <p className="text-[11px] text-white/70">Academy - Sokoto</p>
            </div>
          </div>

          <div>
            <h1 className="text-4xl font-semibold leading-tight tracking-tight">
              Building intelligent
              <br />
              men and women.
            </h1>
            <p className="mt-4 text-white/70 max-w-sm text-[14px] leading-relaxed">
              One portal for students, staff, and parents - Sokoto's trusted
              private school since 2015.
            </p>

            <div className="mt-12 grid grid-cols-3 gap-6 max-w-sm">
              <Stat icon={Users} label="Pupils" value="500+" />
              <Stat icon={BookOpen} label="Classes" value="14" />
              <Stat icon={Wallet} label="Fees" value="Online" />
            </div>
          </div>

          <p className="text-[11px] text-white/50">
            RC-2372959 - DIBA - Knowledge is Wealth
          </p>
        </div>
      </div>

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
      <Icon className="w-4 h-4 text-brand-300 mb-2" />
      <p className="text-xl font-semibold">{value}</p>
      <p className="text-[11px] text-white/60 mt-0.5">{label}</p>
    </div>
  );
}