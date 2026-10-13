import { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import {
  ArrowLeft, Users, Save, Check, BookOpen,
} from "lucide-react";
import PageHeader from "../../components/PageHeader";
import {
  Button, Card, Avatar, Badge, Skeleton, EmptyState, Checkbox,
} from "../../components/ui";
import { useToast } from "../../hooks/useToast";
import { useRole } from "../../hooks/useRole";
import api from "../../lib/api";
import type { ApiResponse } from "../../types/common";

interface SubjectRow {
  subjectId: string;
  name: string;
  code: string;
  isAssigned: boolean;
}

export default function ClassDetailPage() {
  const { classId } = useParams<{ classId: string }>();
  const navigate = useNavigate();
  const { toast } = useToast();
  const { isTeacher, isAdmin } = useRole();

  // Only admins can tick / save subjects
  const canEditSubjects = !isTeacher && isAdmin;

  const [cls, setCls] = useState<any>(null);
  const [students, setStudents] = useState<any[]>([]);
  const [subjects, setSubjects] = useState<SubjectRow[]>([]);
  const [selectedSubjectIds, setSelectedSubjectIds] = useState<Set<string>>(new Set());
  const [loading, setLoading] = useState(true);
  const [savingSubjects, setSavingSubjects] = useState(false);

  useEffect(() => {
    if (!classId) return;
    (async () => {
      try {
        const requests: Promise<any>[] = [
          api.get<ApiResponse<any>>(`/schoolclass/${classId}`),
          api.get<ApiResponse<any[]>>(`/student/by-class/${classId}`),
        ];

        // Subjects-selection endpoint is admin-only. Only call it if we can edit.
        if (canEditSubjects) {
          requests.push(
            api.get<ApiResponse<SubjectRow[]>>(
              `/schoolclass/${classId}/subjects-selection`
            )
          );
        } else {
          requests.push(
            api.get<ApiResponse<any[]>>(`/schoolclass/${classId}/subjects`)
          );
        }

        const [c, s, subj] = await Promise.all(requests);

        if (c.data.success) setCls(c.data.data);
        if (s.data.success) setStudents(s.data.data);

        if (subj.data.success) {
          const rows: SubjectRow[] = subj.data.data.map((x: any) => ({
            subjectId: x.subjectId ?? x.id,
            name: x.name,
            code: x.code ?? "",
            isAssigned: x.isAssigned ?? true,
          }));
          setSubjects(rows);
          setSelectedSubjectIds(
            new Set(rows.filter((r) => r.isAssigned).map((r) => r.subjectId))
          );
        }
      } finally {
        setLoading(false);
      }
    })();
  }, [classId, canEditSubjects]);

  const toggleSubject = (id: string) => {
    if (!canEditSubjects) return;
    const next = new Set(selectedSubjectIds);
    if (next.has(id)) next.delete(id);
    else next.add(id);
    setSelectedSubjectIds(next);
  };

  const selectAll = () => {
    if (!canEditSubjects) return;
    setSelectedSubjectIds(new Set(subjects.map((s) => s.subjectId)));
  };

  const clearAll = () => {
    if (!canEditSubjects) return;
    setSelectedSubjectIds(new Set());
  };

  const saveSubjects = async () => {
    if (!classId || !canEditSubjects) return;
    setSavingSubjects(true);
    try {
      const { data } = await api.put<ApiResponse<any>>(
        `/schoolclass/subjects/${classId}`,
        Array.from(selectedSubjectIds)
      );
      if (data.success) {
        toast({
          variant: "success",
          title: `${selectedSubjectIds.size} subject(s) saved`,
        });
      } else {
        toast({ variant: "error", title: data.message ?? "Failed" });
      }
    } catch {
      toast({ variant: "error", title: "Could not save subjects" });
    } finally {
      setSavingSubjects(false);
    }
  };

  if (loading) return <Card><Skeleton className="h-64" /></Card>;

  if (!cls) {
    return (
      <Card>
        <EmptyState
          icon={Users}
          title="Class not found"
          action={<Button onClick={() => navigate("/admin/classes")}>Back</Button>}
        />
      </Card>
    );
  }

  return (
    <div className="space-y-6">
      <button
        type="button"
        onClick={() => navigate("/admin/classes")}
        className="inline-flex items-center gap-1.5 text-[12px] text-surface-500 hover:text-surface-800"
      >
        <ArrowLeft className="w-3.5 h-3.5" />
        All classes
      </button>

      <PageHeader
        eyebrow={cls.academicSection}
        title={cls.name}
        description={`${students.length} student${students.length !== 1 ? "s" : ""} - ${selectedSubjectIds.size} subject${selectedSubjectIds.size !== 1 ? "s" : ""}`}
      />

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* -------- Students list -------- */}
        <Card padding="none">
          <div className="p-6 pb-4">
            <h3 className="text-[15px] font-semibold text-surface-900">
              Students in this class
            </h3>
            <p className="text-[12px] text-surface-400 mt-0.5">
              Everyone currently enrolled.
            </p>
          </div>
          {students.length === 0 ? (
            <div className="px-6 pb-6">
              <EmptyState icon={Users} title="No students enrolled yet" />
            </div>
          ) : (
            <div className="divide-y divide-surface-100 max-h-[560px] overflow-y-auto">
              {students.map((s) => (
                <button
                  key={s.id}
                  type="button"
                  onClick={() => navigate(`/admin/students/${s.id}`)}
                  className="w-full text-left px-6 py-3 flex items-center gap-3 hover:bg-surface-50"
                >
                  <Avatar name={s.fullName} src={s.profilePictureUrl} size="sm" />
                  <div className="flex-1 min-w-0">
                    <p className="text-[13px] font-medium text-surface-800 truncate">
                      {s.fullName}
                    </p>
                    <p className="text-[11px] text-surface-400 font-mono">
                      {s.admissionNumber}
                    </p>
                  </div>
                  {!s.isActive && <Badge variant="neutral">Inactive</Badge>}
                </button>
              ))}
            </div>
          )}
        </Card>

        {/* -------- Subject picker -------- */}
        <Card padding="none">
          <div className="p-6 pb-4">
            <div className="flex items-start justify-between gap-3">
              <div>
                <h3 className="text-[15px] font-semibold text-surface-900 flex items-center gap-2">
                  <BookOpen className="w-4 h-4 text-surface-500" />
                  Subjects taught
                </h3>
                <p className="text-[12px] text-surface-400 mt-0.5">
                  {canEditSubjects
                    ? "Tick every subject this class teaches, then Save."
                    : "Subjects assigned to this class."}
                </p>
              </div>

              {canEditSubjects && (
                <div className="flex items-center gap-2">
                  <button
                    type="button"
                    onClick={selectAll}
                    className="text-[11px] font-medium text-brand-600 hover:text-brand-700"
                  >
                    Select all
                  </button>
                  <span className="text-surface-300">|</span>
                  <button
                    type="button"
                    onClick={clearAll}
                    className="text-[11px] font-medium text-surface-500 hover:text-surface-700"
                  >
                    Clear
                  </button>
                </div>
              )}
            </div>
          </div>

          {subjects.length === 0 ? (
            <div className="px-6 pb-6">
              <EmptyState
                icon={BookOpen}
                title="No subjects in the catalogue"
                description="Add subjects first under Academics, then come back here."
              />
            </div>
          ) : (
            <div className="divide-y divide-surface-100 max-h-[560px] overflow-y-auto">
              {subjects.map((s) => {
                const isOn = selectedSubjectIds.has(s.subjectId);
                return (
                  <label
                    key={s.subjectId}
                    className={`px-6 py-3 flex items-center gap-3 ${
                      canEditSubjects ? "cursor-pointer" : "cursor-default"
                    } transition-colors ${
                      isOn ? "bg-brand-50/40" : "hover:bg-surface-50"
                    }`}
                  >
                    <Checkbox
                      checked={isOn}
                      disabled={!canEditSubjects}
                      onChange={() => toggleSubject(s.subjectId)}
                    />
                    <div className="flex-1 min-w-0">
                      <p className="text-[13px] font-medium text-surface-800">
                        {s.name}
                      </p>
                      {s.code && (
                        <p className="text-[11px] text-surface-400 font-mono mt-0.5">
                          {s.code}
                        </p>
                      )}
                    </div>
                    {isOn && <Check className="w-4 h-4 text-brand-600 shrink-0" />}
                  </label>
                );
              })}
            </div>
          )}

          {canEditSubjects && subjects.length > 0 && (
            <div className="p-6 pt-4 border-t border-surface-100 flex items-center justify-between gap-3">
              <p className="text-[12px] text-surface-500">
                {selectedSubjectIds.size} of {subjects.length} selected
              </p>
              <Button
                icon={Save}
                loading={savingSubjects}
                onClick={saveSubjects}
              >
                Save subjects
              </Button>
            </div>
          )}
        </Card>
      </div>
    </div>
  );
}