import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import {
  GraduationCap, Plus, Users, BookOpen, Settings2, Baby, Sparkles,
  Trophy, Heart,
} from "lucide-react";
import PageHeader from "../../components/PageHeader";
import { Button, Card, EmptyState, Badge, Skeleton } from "../../components/ui";
import api from "../../lib/api";
import type { ApiResponse } from "../../types/common";

type Section =
  | "PreNursery"
  | "Nursery"
  | "Primary"
  | "JuniorSecondary"
  | "SeniorSecondary";

interface SchoolClass {
  id: string;
  name: string;
  academicSection: Section;
  studentCount?: number;
}

const SECTION_STYLE: Record<
  Section,
  {
    badge: "rose" | "amber" | "sky" | "violet" | "brand";
    ring: string;
    bar: string;
    icon: typeof Baby;
    label: string;
  }
> = {
  PreNursery: {
    badge: "rose",
    ring: "hover:border-rose-200",
    bar: "from-rose-300 to-rose-500",
    icon: Heart,
    label: "Pre-Nursery",
  },
  Nursery: {
    badge: "amber",
    ring: "hover:border-pink-200",
    bar: "from-pink-400 to-pink-600",
    icon: Baby,
    label: "Nursery",
  },
  Primary: {
    badge: "sky",
    ring: "hover:border-sky-200",
    bar: "from-sky-400 to-sky-600",
    icon: Sparkles,
    label: "Primary",
  },
  JuniorSecondary: {
    badge: "violet",
    ring: "hover:border-violet-200",
    bar: "from-violet-400 to-violet-600",
    icon: BookOpen,
    label: "Junior Secondary",
  },
  SeniorSecondary: {
    badge: "brand",
    ring: "hover:border-brand-200",
    bar: "from-brand-400 to-brand-600",
    icon: Trophy,
    label: "Senior Secondary",
  },
};

export default function ClassesPage() {
  const navigate = useNavigate();
  const [classes, setClasses] = useState<SchoolClass[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    (async () => {
      try {
        const { data } = await api.get<ApiResponse<SchoolClass[]>>(
          "/schoolclass/all"
        );
        if (data.success) setClasses(data.data);
      } finally {
        setLoading(false);
      }
    })();
  }, []);

  const grouped = classes.reduce<Record<string, SchoolClass[]>>((acc, c) => {
    (acc[c.academicSection] ??= []).push(c);
    return acc;
  }, {});

  const sections: Section[] = [
    "PreNursery",
    "Nursery",
    "Primary",
    "JuniorSecondary",
    "SeniorSecondary",
  ];

  return (
    <div className="space-y-8">
      <PageHeader
        eyebrow="Academics"
        title="Classes & sections"
        description="Every class group in the school, organised by section."
        actions={
          <Button icon={Plus} onClick={() => navigate("/admin/classes/new")}>
            New class
          </Button>
        }
      />

      {loading ? (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
          {Array.from({ length: 8 }).map((_, i) => (
            <Card key={i}>
              <Skeleton className="h-6 w-24 mb-4" />
              <Skeleton className="h-3 w-32" />
            </Card>
          ))}
        </div>
      ) : classes.length === 0 ? (
        <Card>
          <EmptyState
            icon={GraduationCap}
            title="No classes yet"
            description="Create your first class to start enrolling students."
            action={<Button icon={Plus}>New class</Button>}
          />
        </Card>
      ) : (
        sections.map((section) => {
          const list = grouped[section] ?? [];
          if (!list.length) return null;
          const style = SECTION_STYLE[section];
          const Icon = style.icon;

          return (
            <section key={section}>
              <div className="flex items-center gap-3 mb-4">
                <div
                  className={`w-9 h-9 rounded-xl bg-gradient-to-br ${style.bar} grid place-items-center text-white shadow-sm`}
                >
                  <Icon className="w-4 h-4" />
                </div>
                <div>
                  <h2 className="text-[15px] font-semibold text-surface-900">
                    {style.label}
                  </h2>
                  <p className="text-[12px] text-surface-400">
                    {list.length} class{list.length !== 1 ? "es" : ""}
                  </p>
                </div>
              </div>

              <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-4">
                {list.map((c) => (
                  <button
                    key={c.id}
                    type="button"
                    onClick={() => navigate(`/admin/classes/${c.id}`)}
                    className={`text-left bg-white rounded-2xl border border-surface-100 p-5 shadow-soft transition-all ${style.ring} hover:shadow-md`}
                  >
                    <div
                      className={`h-1 w-12 rounded-full bg-gradient-to-r ${style.bar} mb-4`}
                    />

                    <div className="flex items-start justify-between gap-3">
                      <div>
                        <p className="text-[15px] font-semibold text-surface-900">
                          {c.name}
                        </p>
                        <p className="text-[11px] text-surface-400 mt-0.5">
                          {style.label}
                        </p>
                      </div>
                      <Badge variant={style.badge}>{c.name.split(" ")[0]}</Badge>
                    </div>

                    <div className="mt-5 flex items-center justify-between">
                      <span className="inline-flex items-center gap-1.5 text-[12px] text-surface-500">
                        <Users className="w-3.5 h-3.5" />
                        {c.studentCount ?? 0} students
                      </span>
                      <Settings2 className="w-3.5 h-3.5 text-surface-300" />
                    </div>
                  </button>
                ))}
              </div>
            </section>
          );
        })
      )}
    </div>
  );
}