import { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import {
  ArrowLeft, GraduationCap, Users, BookOpen, Plus, X, Save,
  Check, Pencil, AlertCircle, RefreshCw,
} from "lucide-react";
import PageHeader from "../../components/PageHeader";
import {
  Button, Card, Badge, Skeleton, EmptyState, Select,
} from "../../components/ui";
import { useToast } from "../../hooks/useToast";
import { useRole } from "../../hooks/useRole";
import api from "../../lib/api";
import type { ApiResponse } from "../../types/common";

type Section =
  | "PreNursery" | "Nursery" | "Primary"
  | "JuniorSecondary" | "SeniorSecondary";

type ClassStream = "Science" | "Arts" | "Commercial";

interface SchoolClass {
  id: string;
  name: string;
  academicSection: Section;
  displayOrder: number;
}

interface StudentRow {
  id: string;
  admissionNumber: string;
  fullName: string;
  stream: ClassStream | null;
}

interface SubjectOption {
  id: string;
  name: string;
  code: string;
}

interface AssignedSubject {
  subjectId: string;
  subjectName: string;
  subjectCode: string;
  stream: ClassStream | null;
}

const SECTION_LABEL: Record<Section, string> = {
  PreNursery: "Pre-Nursery",
  Nursery: "Nursery",
  Primary: "Primary",
  JuniorSecondary: "Junior Secondary",
  SeniorSecondary: "Senior Secondary",
};

const STREAM_LABEL: Record<string, string> = {
  Science: "Science",
  Arts: "Arts",
  Commercial: "Commercial",
};

const EDIT_ROLES = ["SuperAdmin", "Principal", "SchoolAdmin"];

export default function ClassDetailPage() {
  const { classId } = useParams<{ classId: string }>();
  const navigate = useNavigate();
  const { toast } = useToast();
  const { role } = useRole();

  const canEdit = role != null && EDIT_ROLES.includes(role);
  const isSenior = (section: Section | undefined) =>
    section === "SeniorSecondary";

  const [schoolClass, setSchoolClass] = useState<SchoolClass | null>(null);
  const [students, setStudents] = useState<StudentRow[]>([]);
  const [assigned, setAssigned] = useState<AssignedSubject[]>([]);
  const [allSubjects, setAllSubjects] = useState<SubjectOption[]>([]);
  const [loading, setLoading] = useState(true);

  const [editOpen, setEditOpen] = useState(false);
  const [editSubjects, setEditSubjects] = useState<
    { subjectId: string; stream: ClassStream | null }[]
  >([]);
  const [saving, setSaving] = useState(false);

  const load = async () => {
    if (!classId) return;
    setLoading(true);
    try {
      const [classRes, studentRes, subjectRes, allSubjectRes] =
        await Promise.allSettled([
          api.get<ApiResponse<SchoolClass>>(`/schoolclass/${classId}`),
          api.get<ApiResponse<StudentRow[]>>(
            `/student/by-class/${classId}?onlyActive=true`
          ),
          api.get<ApiResponse<AssignedSubject[]>>(
            `/schoolclass/${classId}/subjects`
          ),
          api.get<ApiResponse<SubjectOption[]>>("/subject/all"),
        ]);

      if (classRes.status === "fulfilled" && classRes.value.data.success) {
        setSchoolClass(classRes.value.data.data);
      }

      if (studentRes.status === "fulfilled" && studentRes.value.data.success) {
        setStudents(studentRes.value.data.data ?? []);
      }

      if (subjectRes.status === "fulfilled" && subjectRes.value.data.success) {
        setAssigned(subjectRes.value.data.data ?? []);
      }

      if (
        allSubjectRes.status === "fulfilled" &&
        allSubjectRes.value.data.success
      ) {
        setAllSubjects(allSubjectRes.value.data.data ?? []);
      }
    } catch {
      toast({ variant: "error", title: "Could not load class data." });
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [classId]);

  const openEdit = () => {
    setEditSubjects(
      assigned.map((a) => ({
        subjectId: a.subjectId,
        stream: a.stream,
      }))
    );
    setEditOpen(true);
  };

  const toggleSubject = (subjectId: string) => {
    setEditSubjects((prev) =>
      prev.some((e) => e.subjectId === subjectId)
        ? prev.filter((e) => e.subjectId !== subjectId)
        : [...prev, { subjectId, stream: null }]
    );
  };

  const setSubjectStream = (
    subjectId: string,
    stream: ClassStream | null
  ) => {
    setEditSubjects((prev) =>
      prev.map((e) =>
        e.subjectId === subjectId ? { ...e, stream } : e
      )
    );
  };

  const saveSubjects = async () => {
    if (!classId) return;
    setSaving(true);
    try {
      const { data } = await api.put<ApiResponse<number>>(
        `/schoolclass/${classId}/subjects`,
        {
          subjects: editSubjects.map((e) => ({
            subjectId: e.subjectId,
            stream: e.stream,
          })),
        }
      );

      if (!data.success) {
        toast({ variant: "error", title: data.message ?? "Failed to save." });
        return;
      }

      toast({
        variant: "success",
        title: `Saved ${data.data ?? editSubjects.length} subject(s).`,
      });
      setEditOpen(false);
      await load();
    } catch (err: unknown) {
      const e2 = err as { response?: { data?: { message?: string } } };
      toast({
        variant: "error",
        title: e2?.response?.data?.message ?? "Could not save subjects.",
      });
    } finally {
      setSaving(false);
    }
  };

  if (loading) {
    return (
      <div className="space-y-6">
        <Skeleton className="h-8 w-40" />
        <Card><Skeleton className="h-24" /></Card>
      </div>
    );
  }

  if (!schoolClass) {
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
        <Card>
          <EmptyState
            icon={GraduationCap}
            title="Class not found"
            action={
              <Button onClick={() => navigate("/admin/classes")}>Back</Button>
            }
          />
        </Card>
      </div>
    );
  }

  const senior = isSenior(schoolClass.academicSection);

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
        eyebrow={SECTION_LABEL[schoolClass.academicSection]}
        title={schoolClass.name}
        description={`${students.length} student(s) · ${assigned.length} subject(s) assigned`}
        actions={
          canEdit && (
            <Button variant="secondary" icon={Pencil} onClick={openEdit}>
              Edit subjects
            </Button>
          )
        }
      />

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* STUDENTS */}
        <Card padding="none">
          <div className="p-6 pb-4 flex items-center justify-between">
            <div className="flex items-center gap-2">
              <Users className="w-4 h-4 text-surface-500" />
              <h3 className="text-[15px] font-semibold text-surface-900">
                Students
              </h3>
              <Badge variant="sky">{students.length}</Badge>
            </div>
          </div>

          {students.length === 0 ? (
            <div className="px-6 pb-6">
              <EmptyState
                icon={Users}
                title="No students in this class"
                description="Enrol students via the Students page."
              />
            </div>
          ) : (
            <div className="divide-y divide-surface-100 max-h-[420px] overflow-y-auto">
              {students.map((s) => (
                <button
                  key={s.id}
                  type="button"
                  onClick={() => navigate(`/admin/students/${s.id}`)}
                  className="w-full text-left px-6 py-3 hover:bg-surface-50/60 flex items-center gap-3"
                >
                  <div className="flex-1 min-w-0">
                    <p className="text-[13px] font-medium text-surface-900 truncate">
                      {s.fullName}
                    </p>
                    <p className="text-[11px] text-surface-400 font-mono">
                      {s.admissionNumber}
                    </p>
                  </div>
                  {s.stream && (
                    <Badge
                      variant={
                        s.stream === "Science"
                          ? "sky"
                          : s.stream === "Arts"
                          ? "violet"
                          : "amber"
                      }
                    >
                      {STREAM_LABEL[s.stream]}
                    </Badge>
                  )}
                </button>
              ))}
            </div>
          )}
        </Card>

        {/* SUBJECTS */}
        <Card padding="none">
          <div className="p-6 pb-4 flex items-center justify-between">
            <div className="flex items-center gap-2">
              <BookOpen className="w-4 h-4 text-surface-500" />
              <h3 className="text-[15px] font-semibold text-surface-900">
                Subjects
              </h3>
              <Badge variant="violet">{assigned.length}</Badge>
            </div>
          </div>

          {assigned.length === 0 ? (
            <div className="px-6 pb-6">
              <EmptyState
                icon={BookOpen}
                title="No subjects assigned"
                description={
                  canEdit
                    ? "Click Edit subjects to assign."
                    : "The admin hasn't assigned subjects yet."
                }
                action={
                  canEdit ? (
                    <Button variant="secondary" icon={Pencil} onClick={openEdit}>
                      Edit subjects
                    </Button>
                  ) : undefined
                }
              />
            </div>
          ) : (
            <div className="divide-y divide-surface-100 max-h-[420px] overflow-y-auto">
              {assigned.map((a) => (
                <div
                  key={a.subjectId}
                  className="px-6 py-3 flex items-center gap-3"
                >
                  <div className="flex-1 min-w-0">
                    <p className="text-[13px] font-medium text-surface-900 truncate">
                      {a.subjectName}
                    </p>
                    <p className="text-[11px] text-surface-400 font-mono">
                      {a.subjectCode}
                    </p>
                  </div>
                  {senior &&
                    (a.stream ? (
                      <Badge
                        variant={
                          a.stream === "Science"
                            ? "sky"
                            : a.stream === "Arts"
                            ? "violet"
                            : "amber"
                        }
                      >
                        {STREAM_LABEL[a.stream]}
                      </Badge>
                    ) : (
                      <Badge variant="neutral">All streams</Badge>
                    ))}
                </div>
              ))}
            </div>
          )}
        </Card>
      </div>

      {/* EDIT MODAL */}
      {canEdit && editOpen && (
        <>
          <div
            className="fixed inset-0 bg-surface-950/50 z-40"
            onClick={() => !saving && setEditOpen(false)}
            aria-hidden
          />
          <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
            <div className="bg-white rounded-2xl shadow-xl w-full max-w-3xl max-h-[90vh] flex flex-col overflow-hidden">
              <div className="p-5 flex items-center justify-between border-b border-surface-100 shrink-0">
                <div>
                  <h2 className="text-[15px] font-semibold text-surface-900">
                    Edit subjects
                  </h2>
                  <p className="text-[12px] text-surface-500 mt-0.5">
                    {senior
                      ? "Pick subjects and their stream. Leave stream blank for core subjects (all students take them)."
                      : "Pick subjects offered to this class."}
                  </p>
                </div>
                <button
                  type="button"
                  onClick={() => !saving && setEditOpen(false)}
                  className="p-2 rounded-lg text-surface-400 hover:bg-surface-100"
                >
                  <X className="w-4 h-4" />
                </button>
              </div>

              <div className="flex-1 overflow-y-auto p-5">
                {allSubjects.length === 0 ? (
                  <EmptyState
                    icon={BookOpen}
                    title="No subjects in catalogue"
                    description="Create subjects first."
                  />
                ) : (
                  <div className="space-y-2">
                    {allSubjects.map((s) => {
                      const entry = editSubjects.find(
                        (e) => e.subjectId === s.id
                      );
                      const isAssigned = !!entry;
                      return (
                        <div
                          key={s.id}
                          className={`p-3 rounded-xl border flex items-center gap-3 ${
                            isAssigned
                              ? "border-violet-200 bg-violet-50/30"
                              : "border-surface-100"
                          }`}
                        >
                          <button
                            type="button"
                            onClick={() => toggleSubject(s.id)}
                            className={`w-5 h-5 rounded grid place-items-center shrink-0 ${
                              isAssigned
                                ? "bg-violet-600"
                                : "bg-surface-100"
                            }`}
                          >
                            {isAssigned && (
                              <Check className="w-3 h-3 text-white" />
                            )}
                          </button>

                          <div className="flex-1 min-w-0">
                            <p className="text-[13px] font-medium text-surface-800 truncate">
                              {s.name}
                            </p>
                            <p className="text-[11px] text-surface-400 font-mono">
                              {s.code}
                            </p>
                          </div>

                          {isAssigned && senior && (
                            <div className="w-40 shrink-0">
                              <Select
                                value={entry.stream ?? ""}
                                onChange={(e) =>
                                  setSubjectStream(
                                    s.id,
                                    (e.target.value || null) as ClassStream | null
                                  )
                                }
                                options={[
                                  { value: "", label: "All streams" },
                                  { value: "Science", label: "Science" },
                                  { value: "Arts", label: "Arts" },
                                  { value: "Commercial", label: "Commercial" },
                                ]}
                              />
                            </div>
                          )}
                        </div>
                      );
                    })}
                  </div>
                )}
              </div>

              <div className="p-5 border-t border-surface-100 flex items-center justify-between shrink-0">
                <p className="text-[12px] text-surface-500">
                  {editSubjects.length} subject(s) selected
                </p>
                <div className="flex gap-3">
                  <Button
                    variant="secondary"
                    onClick={() => setEditOpen(false)}
                    disabled={saving}
                  >
                    Cancel
                  </Button>
                  <Button icon={Save} loading={saving} onClick={saveSubjects}>
                    Save subjects
                  </Button>
                </div>
              </div>
            </div>
          </div>
        </>
      )}
    </div>
  );
}