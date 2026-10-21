import { useEffect, useState } from "react";
import { BookOpen, UserCog, AlertCircle, RefreshCw } from "lucide-react";
import { useNavigate } from "react-router-dom";
import PageHeader from "../../components/PageHeader";
import { Button, Card, Badge, Skeleton } from "../../components/ui";
import api from "../../lib/api";
import type { ApiResponse } from "../../types/common";

interface StudentSubject {
  subjectId: string;
  subjectName: string;
  subjectCode: string;
  teacherName: string;
}

export default function MySubjects() {
  const navigate = useNavigate();

  const [subjects, setSubjects] = useState<StudentSubject[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const load = async () => {
    setLoading(true);
    setError(null);
    try {
      const { data } = await api.get<ApiResponse<StudentSubject[]>>(
        "/student/me/subjects"
      );
      if (!data.success || !data.data) {
        setError(data.message ?? "Could not load your subjects.");
        return;
      }
      setSubjects(data.data);
    } catch (err: unknown) {
      const e2 = err as {
        response?: { data?: { message?: string } };
        message?: string;
      };
      setError(
        e2?.response?.data?.message ??
          e2?.message ??
          "Could not load your subjects."
      );
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return (
    <div className="space-y-6 lg:space-y-8">
      <PageHeader
        eyebrow="Academics"
        title="My subjects"
        description="The subjects you're enrolled in this session."
        actions={
          <Button
            variant="secondary"
            icon={RefreshCw}
            onClick={load}
            disabled={loading}
          >
            Refresh
          </Button>
        }
      />

      {loading ? (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
          {Array.from({ length: 6 }).map((_, i) => (
            <Card key={i}>
              <Skeleton className="h-24" />
            </Card>
          ))}
        </div>
      ) : error ? (
        <Card>
          <div className="flex items-start gap-3 py-4">
            <AlertCircle className="w-5 h-5 text-red-600 mt-0.5 shrink-0" />
            <div className="flex-1">
              <p className="text-[14px] font-semibold text-red-900">
                Couldn't load your subjects
              </p>
              <p className="text-[13px] text-red-800/80 mt-1">{error}</p>
              <Button variant="secondary" className="mt-3" onClick={load}>
                Retry
              </Button>
            </div>
          </div>
        </Card>
      ) : subjects.length === 0 ? (
        <Card>
          <div className="text-center py-12">
            <div className="w-12 h-12 rounded-xl bg-surface-50 grid place-items-center mx-auto mb-3">
              <BookOpen className="w-6 h-6 text-surface-400" />
            </div>
            <p className="text-[14px] font-medium text-surface-700">
              No subjects yet
            </p>
            <p className="text-[13px] text-surface-500 mt-1 max-w-sm mx-auto">
              Your subjects will appear here once they're assigned. Check back
              after your class schedule is published.
            </p>
            <Button
              variant="secondary"
              className="mt-4"
              onClick={() => navigate("/student")}
            >
              Back to dashboard
            </Button>
          </div>
        </Card>
      ) : (
        <>
          <div className="flex items-center gap-2 text-[12px] text-surface-500">
            <span className="font-medium text-surface-700">
              {subjects.length}
            </span>
            <span>{subjects.length === 1 ? "subject" : "subjects"} enrolled</span>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
            {subjects.map((s) => (
              <Card key={s.subjectId}>
                <div className="flex items-start gap-3">
                  <div className="w-10 h-10 rounded-xl bg-violet-50 grid place-items-center shrink-0">
                    <BookOpen className="w-5 h-5 text-violet-600" />
                  </div>
                  <div className="min-w-0 flex-1">
                    <p className="text-[14px] font-semibold text-surface-900 truncate">
                      {s.subjectName}
                    </p>
                    {s.subjectCode && (
                      <div className="mt-1">
                        <Badge variant="violet">{s.subjectCode}</Badge>
                      </div>
                    )}
                  </div>
                </div>
                <div className="mt-4 flex items-center gap-2 text-[12px] text-surface-500">
                  <UserCog className="w-3.5 h-3.5 shrink-0" />
                  <span className="truncate">
                    {s.teacherName && s.teacherName !== "—"
                      ? s.teacherName
                      : "Teacher not assigned"}
                  </span>
                </div>
              </Card>
            ))}
          </div>
        </>
      )}
    </div>
  );
}