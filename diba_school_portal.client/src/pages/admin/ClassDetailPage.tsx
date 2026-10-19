import { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { ArrowLeft, Users, Save, Check, BookOpen } from "lucide-react";
import PageHeader from "../../components/PageHeader";
import {
  Button,
  Card,
  Avatar,
  Badge,
  Skeleton,
  EmptyState,
  Checkbox,
} from "../../components/ui";
import { useToast } from "../../hooks/useToast";
import { useRole } from "../../hooks/useRole";
import api from "../../lib/api";
import type { ApiResponse } from "../../types/common";

/* 0 = Science, 1 = Arts, 2 = Commercial */
type StreamNum = 0 | 1 | 2;
const STREAM_OPTIONS: { value: StreamNum; label: string }[] = [
  { value: 0, label: "Science" },
  { value: 1, label: "Arts" },
  { value: 2, label: "Commercial" },
];

/* Only these roles can edit class-subject assignments */
const EDIT_SUBJECTS_ROLES = ["SuperAdmin", "Principal", "SchoolAdmin"];

interface SubjectRow {
  subjectId: string;
  name: string;
  code: string;
  isAssigned: boolean;
  stream: StreamNum | null;
}

interface AssignmentItem {
  subjectId: string;
  stream: StreamNum | null;
}

export default function ClassDetailPage() {
  const { classId } = useParams<{ classId: string }>();
  const navigate = useNavigate();
  const { toast } = useToast();
  const { role } = useRole();

  const canEditSubjects = !!role && EDIT_SUBJECTS_ROLES.includes(role);

  const [cls, setCls] = useState<any>(null);
  const [students, setStudents] = useState<any[]>([]);
  const [subjects, setSubjects] = useState<SubjectRow[]>([]);
  const [assignments, setAssignments] = useState<Map<string, StreamNum | null>>(
    new Map()
  );

  const [loading, setLoading] = useState(true);
  const [savingSubjects, setSavingSubjects] = useState(false);

  const isSSClass = cls?.academicSection === "SeniorSecondary";

  useEffect(() => {
    if (!classId) return;
    (async () => {
      try {
        const requests: Promise<any>[] = [
          api.get<ApiResponse<any>>(`/schoolclass/${classId}`),
          api.get<ApiResponse<any[]>>(`/student/by-class/${classId}`),
        ];

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
            stream:
              typeof x.stream === "number" ? (x.stream as StreamNum) : null,
          }));
          setSubjects(rows);

          const map = new Map<string, StreamNum | null>();
          rows.filter((r) => r.isAssigned).forEach((r) => {
            map.set(r.subjectId, r.stream);
          });
          setAssignments(map);
        }
      } finally {
        setLoading(false);
      }
    })();
  }, [classId, canEditSubjects]);

  const toggleSubject = (id: string) => {
    if (!canEditSubjects) return;
    const next = new Map(assignments);
    if (next.has(id)) next.delete(id);
    else next.set(id, null);
    setAssignments(next);
  };

  const setStreamFor = (id: string, stream: StreamNum | null) => {
    if (!canEditSubjects) return;
    const next = new Map(assignments);
    if (next.has(id)) {
      next.set(id, stream);
      setAssignments(next);
    }
  };

  const selectAll = () => {
    if (!canEditSubjects) return;
    const next = new Map<string, StreamNum | null>();
    subjects.forEach((s) => next.set(s.subjectId, null));
    setAssignments(next);
  };

  const clearAll = () => {
    if (!canEditSubjects) return;
    setAssignments(new Map());
  };

  const saveSubjects = async () => {
    if (!classId || !canEditSubjects) return;
    setSavingSubjects(true);
    try {
      const payload: AssignmentItem[] = Array.from(assignments.entries()).map(
        ([subjectId, stream]) => ({ subjectId, stream })
      );

      const { data } = await api.put<ApiResponse<any>>(
        `/schoolclass/subjects/${classId}`,
        payload
      );
      if (data.success) {
        toast({
          variant: "success",
          title: `${payload.length} subject(s) saved`,
        });
      } else {
        toast({ variant: "error", title: data.message ?? "Failed" });
      }
    } catch (err: unknown) {
      const e2 = err as { response?: { data?: { message?: string } } };
      toast({
        variant: "error",
        title: e2?.response?.data?.message ?? "Could not save subjects",
      });
    } finally {
      setSavingSubjects(false);
    }
  };

  if (loading)
    return (
      <Card>
        <Skeleton className="h-64" />
      </Card>
    );

  if (!cls) {
    return (
      <Card>
        <EmptyState
          icon={Users}
          title="Class not found"
          action={
            <Button onClick={() => navigate("/admin/classes")}>Back</Button>
          }
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
        description={`${students.length} student${students.length !== 1 ? "s" : ""} - ${assignments.size} subject${assignments.size !== 1 ? "s" : ""}`}
      />

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Students */}
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
                  <Avatar
                    name={s.fullName}
                    src={s.profilePictureUrl}
                    size="sm"
                  />
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

        {/* Subjects */}
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
                    ? isSSClass
                      ? "Tick subjects. Pick a stream for SS-specific subjects, or leave 'All' for shared subjects."
                      : "Tick every subject this class teaches, then Save."
                    : "Subjects assigned to this class (read-only for your role)."}
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
                const isOn = assignments.has(s.subjectId);
                const currentStream = assignments.get(s.subjectId) ?? null;

                return (
                  <div
                    key={s.subjectId}
                    className={`px-6 py-3 flex items-center gap-3 ${
                      isOn ? "bg-brand-50/40" : ""
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

                    {isSSClass && isOn && canEditSubjects && (
                      <select
                        value={
                          currentStream === null ? "" : String(currentStream)
                        }
                        onChange={(e) =>
                          setStreamFor(
                            s.subjectId,
                            e.target.value === ""
                              ? null
                              : (Number(e.target.value) as StreamNum)
                          )
                        }
                        className="text-[11px] rounded-lg border border-surface-200 px-2 py-1 bg-white focus:outline-none focus:ring-2 focus:ring-brand-200"
                      >
                        <option value="">All students</option>
                        {STREAM_OPTIONS.map((o) => (
                          <option key={o.value} value={String(o.value)}>
                            {o.label} only
                          </option>
                        ))}
                      </select>
                    )}

                    {isSSClass &&
                      isOn &&
                      !canEditSubjects &&
                      currentStream !== null && (
                        <Badge variant="violet">
                          {
                            STREAM_OPTIONS.find(
                              (o) => o.value === currentStream
                            )?.label
                          }
                        </Badge>
                      )}

                    {isOn && (
                      <Check className="w-4 h-4 text-brand-600 shrink-0" />
                    )}
                  </div>
                );
              })}
            </div>
          )}

          {canEditSubjects && subjects.length > 0 && (
            <div className="p-6 pt-4 border-t border-surface-100 flex items-center justify-between gap-3">
              <p className="text-[12px] text-surface-500">
                {assignments.size} of {subjects.length} selected
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