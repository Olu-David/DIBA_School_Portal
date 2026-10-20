import { useEffect, useMemo, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import {
  ArrowLeft, Users, Save, Check, BookOpen, Lock, Unlock, X,
} from "lucide-react";
import PageHeader from "../../components/PageHeader";
import {
  Button, Card, Avatar, Badge, Skeleton, EmptyState, Checkbox,
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
  isAssigned: boolean;   // ← comes from server now
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

  const canEditRole = !!role && EDIT_SUBJECTS_ROLES.includes(role);

  const [cls, setCls] = useState<any>(null);
  const [students, setStudents] = useState<any[]>([]);
  const [subjects, setSubjects] = useState<SubjectRow[]>([]);

  /* assignments = what's currently ticked (before save) */
  const [assignments, setAssignments] = useState<Map<string, StreamNum | null>>(
    () => new Map()
  );

  /* savedSubjectIds = what was last persisted */
  const [savedSubjectIds, setSavedSubjectIds] = useState<Set<string>>(
    () => new Set()
  );

  /* editSaved = unlock saved rows for re-editing */
  const [editSaved, setEditSaved] = useState(false);

  const [loading, setLoading] = useState(true);
  const [savingSubjects, setSavingSubjects] = useState(false);

  const isSSClass = cls?.academicSection === "SeniorSecondary";

  const isLocked = (subjectId: string) =>
    savedSubjectIds.has(subjectId) && !editSaved;

  const hasSavedSubjects = savedSubjectIds.size > 0;

  /* ---------------- Load ---------------- */
  useEffect(() => {
    if (!classId) return;
    let cancelled = false;

    (async () => {
      setLoading(true);
      try {
        const requests: Promise<any>[] = [
          api.get<ApiResponse<any>>(`/schoolclass/${classId}`),
          api.get<ApiResponse<any[]>>(`/student/by-class/${classId}`),
        ];

        if (canEditRole) {
          requests.push(
            api.get<ApiResponse<any[]>>(
              `/schoolclass/${classId}/subjects-selection`
            )
          );
        } else {
          requests.push(
            api.get<ApiResponse<any[]>>(`/schoolclass/${classId}/subjects`)
          );
        }

        const [c, s, subj] = await Promise.all(requests);
        if (cancelled) return;

        if (c.data.success) setCls(c.data.data);
        if (s.data.success) setStudents(s.data.data);

        if (subj.data.success) {
          /* The read-only endpoint returns only assigned subjects,
             so default `isAssigned` to true for that path. */
          const isReadOnlyList = !canEditRole;

          const rows: SubjectRow[] = subj.data.data.map((x: any) => ({
            subjectId: x.subjectId ?? x.id,
            name: x.name,
            code: x.code ?? "",
            isAssigned: isReadOnlyList ? true : !!x.isAssigned,
            stream:
              typeof x.stream === "number" ? (x.stream as StreamNum) : null,
          }));

          setSubjects(rows);

          const map = new Map<string, StreamNum | null>();
          const saved = new Set<string>();
          rows.forEach((r) => {
            if (r.isAssigned) {
              map.set(r.subjectId, r.stream);
              saved.add(r.subjectId);
            }
          });
          setAssignments(map);
          setSavedSubjectIds(saved);
        }
      } catch (err: unknown) {
        const e2 = err as { response?: { data?: { message?: string } } };
        if (!cancelled)
          toast({
            variant: "error",
            title: e2?.response?.data?.message ?? "Failed to load class",
          });
      } finally {
        if (!cancelled) setLoading(false);
      }
    })();

    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [classId, canEditRole]);

  /* ---------------- Editor actions ---------------- */
  const toggleSubject = (id: string) => {
    if (!canEditRole || isLocked(id)) return;

    setAssignments((prev) => {
      const next = new Map(prev);
      if (next.has(id)) next.delete(id);
      else next.set(id, null);
      return next;
    });
  };

  const setStreamFor = (id: string, stream: StreamNum | null) => {
    if (!canEditRole || isLocked(id)) return;

    setAssignments((prev) => {
      if (!prev.has(id)) return prev;
      const next = new Map(prev);
      next.set(id, stream);
      return next;
    });
  };

  const selectAll = () => {
    if (!canEditRole) return;
    setAssignments((prev) => {
      const next = new Map(prev);
      subjects.forEach((s) => {
        if (!isLocked(s.subjectId)) next.set(s.subjectId, next.get(s.subjectId) ?? null);
      });
      return next;
    });
  };

  const clearAll = () => {
    if (!canEditRole) return;
    setAssignments((prev) => {
      const next = new Map<string, StreamNum | null>();
      prev.forEach((stream, id) => {
        if (isLocked(id)) next.set(id, stream);
      });
      return next;
    });
  };

  const unlockSaved = () => canEditRole && setEditSaved(true);

  const relockSaved = () => {
    const map = new Map<string, StreamNum | null>();
    subjects.forEach((s) => {
      if (s.isAssigned) map.set(s.subjectId, s.stream);
    });
    setAssignments(map);
    setEditSaved(false);
  };

  const saveSubjects = async () => {
    if (!classId || !canEditRole) return;
    setSavingSubjects(true);
    try {
      const payload: AssignmentItem[] = Array.from(assignments.entries()).map(
        ([subjectId, stream]) => ({ subjectId, stream })
      );

      const { data } = await api.put<ApiResponse<any>>(
        `/schoolclass/subjects/${classId}`,
        payload
      );

      if (!data.success) {
        toast({ variant: "error", title: data.message ?? "Failed to save." });
        return;
      }

      const newSaved = new Set<string>(assignments.keys());
      setSavedSubjectIds(newSaved);
      setEditSaved(false);

      setSubjects((prev) =>
        prev.map((s) => ({
          ...s,
          isAssigned: newSaved.has(s.subjectId),
          stream: newSaved.has(s.subjectId)
            ? assignments.get(s.subjectId) ?? null
            : s.stream,
        }))
      );

      toast({
        variant: "success",
        title: `${payload.length} subject(s) saved`,
      });
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

  /* ---------------- Derived UI values ---------------- */
  const unsavedCount = useMemo(() => {
    let n = 0;
    assignments.forEach((_, id) => {
      if (!savedSubjectIds.has(id)) n++;
    });
    return n;
  }, [assignments, savedSubjectIds]);

  /* ---------------- Render guards ---------------- */
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
        description={`${students.length} student${
          students.length !== 1 ? "s" : ""
        } - ${savedSubjectIds.size} subject${
          savedSubjectIds.size !== 1 ? "s" : ""
        } saved`}
      />

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* ---------------- Students ---------------- */}
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

        {/* ---------------- Subjects ---------------- */}
        <Card padding="none">
          <div className="p-6 pb-4">
            <div className="flex items-start justify-between gap-3">
              <div>
                <h3 className="text-[15px] font-semibold text-surface-900 flex items-center gap-2">
                  <BookOpen className="w-4 h-4 text-surface-500" />
                  Subjects taught
                </h3>
                <p className="text-[12px] text-surface-400 mt-0.5">
                  {!canEditRole
                    ? "Read-only for your role."
                    : editSaved
                    ? "Editing saved subjects. Re-lock or save when done."
                    : hasSavedSubjects
                    ? "Saved subjects are locked. Click 'Edit saved' to change them."
                    : isSSClass
                    ? "Tick subjects. Pick a stream for SS-specific subjects."
                    : "Tick every subject this class teaches, then Save."}
                </p>
              </div>

              {canEditRole && (
                <div className="flex items-center gap-2 flex-wrap">
                  {hasSavedSubjects && !editSaved && (
                    <button
                      type="button"
                      onClick={unlockSaved}
                      className="text-[11px] font-medium text-brand-600 hover:text-brand-700 inline-flex items-center gap-1"
                    >
                      <Unlock className="w-3 h-3" />
                      Edit saved
                    </button>
                  )}
                  {hasSavedSubjects && editSaved && (
                    <button
                      type="button"
                      onClick={relockSaved}
                      className="text-[11px] font-medium text-surface-500 hover:text-surface-700 inline-flex items-center gap-1"
                    >
                      <X className="w-3 h-3" />
                      Cancel edits
                    </button>
                  )}
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
                const wasSaved = s.isAssigned;
                const rowLocked = wasSaved && !editSaved;

                return (
                  <div
                    key={s.subjectId}
                    className={`px-6 py-3 flex items-center gap-3 ${
                      isOn ? "bg-brand-50/40" : ""
                    } ${rowLocked ? "bg-emerald-50/30" : ""}`}
                  >
                    <Checkbox
                      checked={isOn}
                      disabled={!canEditRole || rowLocked}
                      onChange={() => toggleSubject(s.subjectId)}
                    />

                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2">
                        <p className="text-[13px] font-medium text-surface-800">
                          {s.name}
                        </p>
                        {wasSaved && (
                          <Badge variant="emerald">
                            <Lock className="w-2.5 h-2.5" />
                            Saved
                          </Badge>
                        )}
                      </div>
                      {s.code && (
                        <p className="text-[11px] text-surface-400 font-mono mt-0.5">
                          {s.code}
                        </p>
                      )}
                    </div>

                    {isSSClass && isOn && canEditRole && !rowLocked && (
                      <select
                        value={currentStream === null ? "" : String(currentStream)}
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

                    {isSSClass && isOn && rowLocked && currentStream !== null && (
                      <Badge variant="violet">
                        {STREAM_OPTIONS.find((o) => o.value === currentStream)?.label}
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

          {canEditRole && subjects.length > 0 && (
            <div className="p-6 pt-4 border-t border-surface-100 flex items-center justify-between gap-3">
              <p className="text-[12px] text-surface-500">
                {assignments.size} selected · {savedSubjectIds.size} saved
                {unsavedCount > 0 && (
                  <span className="text-brand-600 ml-1">
                    · {unsavedCount} unsaved
                  </span>
                )}
              </p>
              <Button icon={Save} loading={savingSubjects} onClick={saveSubjects}>
                Save subjects
              </Button>
            </div>
          )}
        </Card>
      </div>
    </div>
  );
}