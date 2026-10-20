import { GraduationCap, PartyPopper } from "lucide-react";
import { useAuth } from "../hooks/useAuth";

export default function GraduationBanner() {
  const { user } = useAuth();

  if (!user?.isGraduated) return null;

  const year = user.graduationDate
    ? new Date(user.graduationDate).getFullYear()
    : new Date().getFullYear();

  return (
    <div className="relative overflow-hidden rounded-2xl bg-gradient-to-br from-violet-600 via-fuchsia-600 to-rose-500 text-white p-6 shadow-xl">
      <PartyPopper className="absolute top-3 right-3 w-10 h-10 opacity-40" />
      <GraduationCap className="absolute -bottom-4 -right-4 w-32 h-32 opacity-10" />

      <div className="flex items-start gap-4">
        <div className="w-14 h-14 rounded-2xl bg-white/20 backdrop-blur grid place-items-center shrink-0">
          <GraduationCap className="w-7 h-7" />
        </div>
        <div className="flex-1">
          <p className="text-[11px] uppercase tracking-wider font-semibold opacity-80">
            Class of {year}
          </p>
          <h2 className="text-2xl font-bold mt-1">
            🎓 Congratulations, {user.firstName}!
          </h2>
          <p className="mt-2 text-[13px] opacity-90 max-w-xl leading-relaxed">
            You've successfully completed your secondary education
            {user.graduationSession && ` (${user.graduationSession} session)`}.
            We're proud of you — go shine!
          </p>
        </div>
      </div>
    </div>
  );
}