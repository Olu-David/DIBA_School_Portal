import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { BookOpen, ClipboardCheck } from "lucide-react";
import PageHeader from "../../components/PageHeader";
import { Card, EmptyState, Spinner, Badge } from "../../components/ui";
import api from "../../lib/api";
import type { ApiResponse } from "../../types/common";

interface Assignment {
  classId: string;
  className: string;
  section: number;
  subjectId: string;
  subjectName: string;
  subjectCode: string;
  studentCount: number;
  scoredCount: number;
  submissionStatus: string;
}

export default function MyAssignmentsPage() {
  const navigate = useNavigate();
  const [rows, setRows] = useState<Assignment[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    (async () => {
      try {
        const { data } = await api.get<ApiResponse<Assignment[]>>(
          "/termsubjectresult/my-assignments"
        );
        if (data.success) setRows(data.data);
      } catch {
        /* endpoint may not exist yet - empty list */
      } finally {
        setLoading(false);
      }
    })();
  }, []);

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Teaching"
        title="My assignments"
        description="Every class and subject you teach this term."
      />

      {loading ? (
        <Card><div className="py-16 flex justify-center"><Spinner size="lg" label="Loading..." /></div></Card>
      ) : rows.length === 0 ? (
        <Card>
          <EmptyState
            icon={BookOpen}
            title="No assignments yet"
            description="You have not been assigned to any class or subject for the current term."
          />
        </Card>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
          {rows.map((a, i) => (
            <button
              key={i}
              type="button"
              onClick={() =>
                navigate(
                  `/admin/score-entry?classId=${a.classId}&subjectId=${a.subjectId}`
                )
              }
              className="text-left bg-white rounded-2xl border border-surface-100 p-5 shadow-soft hover:border-brand-200 hover:shadow-md transition-all"
            >
              <div className="flex items-start justify-between gap-3">
                <div className="min-w-0">
                  <p className="text-[15px] font-semibold text-surface-900 truncate">
                    {a.className}
                  </p>
                  <p className="text-[12px] text-surface-500 mt-0.5 truncate">
                    {a.subjectName} - <span className="font-mono text-surface-400">{a.subjectCode}</span>
                  </p>
                </div>
                <div className="w-9 h-9 rounded-xl bg-brand-50 grid place-items-center shrink-0">
                  <ClipboardCheck className="w-4 h-4 text-brand-600" />
                </div>
              </div>

              <div className="mt-5 flex items-center justify-between">
                <span className="text-[12px] text-surface-500">
                  {a.scoredCount} / {a.studentCount} scored
                </span>
                <Badge
                  variant={
                    a.submissionStatus === "Submitted" ? "emerald" :
                    a.submissionStatus === "Draft" ? "amber" : "neutral"
                  }
                >
                  {a.submissionStatus === "Submitted" ? "Submitted" :
                   a.submissionStatus === "Draft" ? "Draft" : "Not started"}
                </Badge>
              </div>
            </button>
          ))}
        </div>
      )}
    </div>
  );
}