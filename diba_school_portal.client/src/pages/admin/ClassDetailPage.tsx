import { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import {
  ArrowLeft, Users, BookOpen, Settings2, Save, Check,
} from "lucide-react";
import PageHeader from "../../components/PageHeader";
import {
  Button, Card, Avatar, Badge, Skeleton, EmptyState, Checkbox,
} from "../../components/ui";
import { useToast } from "../../hooks/useToast";
import api from "../../lib/api";
import type { ApiResponse } from "../../types/common";

export default function ClassDetailPage() {
  const { classId } = useParams<{ classId: string }>();
  const navigate = useNavigate();
  const { toast } = useToast();

  const [cls, setCls] = useState<any>(null);
  const [students, setStudents] = useState<any[]>([]);
  const [subjects, setSubjects] = useState<any[]>([]);
  const [selectedSubjectIds, setSelectedSubjectIds] = useState<Set<string>>(new Set());
  const [loading, setLoading] = useState(true);
  const [savingSubjects, setSavingSubjects] = useState(false);

  useEffect(() => {
    if (!classId) return;
    (async () => {
      try {
        const [c, s, subj] = await Promise.all([
          api.get<ApiResponse<any>>(`/schoolclass/${classId}`),
          api.get<ApiResponse<any[]>>(`/student/by-class/${classId}`),
          api.get<ApiResponse<any[]>>(`/schoolclass/${classId}/subjects-selection`),
        ]);
        if (c.data.success) setCls(c.data.data);
        if (s.data.success) setStudents(s.data.data);
        if (subj.data.success) {
          setSubjects(subj.data.data);
          setSelectedSubjectIds(
            new Set(subj.data.data.filter((x: any) => x.isAssigned).map((x: any) => x.subjectId))
          );
        }
      } finally {
        setLoading(false);
      }
    })();
  }, [classId]);

  const toggleSubject = (id: string) => {
    const next = new Set(selectedSubjectIds);
    if (next.has(id)) next.delete(id);
    else next.add(id);
    setSelectedSubjectIds(next);
  };

  const saveSubjects = async () => {
    if (!classId) return;
    setSavingSubjects(true);
    try {
      const { data } = await api.put<ApiResponse<any>>(
        `/schoolclass/subjects/${classId}`,
        Array.from(selectedSubjectIds)
      );
      if (data.success) toast({ variant: "success", title: "Subjects updated" });
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
        className="inline-flex items-center gap-1.5 text-[12px] text-slate-500 hover:text-slate-800"
      >
        <ArrowLeft className="w-3.5 h-3.5" />
        All classes
      </button>

      <PageHeader
        eyebrow={cls.academicSection}
        title={cls.name}
        description={`${students.length} student${students.length !== 1 ? "s" : ""} · ${selectedSubjectIds.size} subject${selectedSubjectIds.size !== 1 ? "s" : ""}`}
      />

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Students list */}
        <Card padding="none">
          <div className="p-6 pb-4">
            <h3 className="text-[15px] font-semibold text-slate-900">Students</h3>
            <p className="text-[12px] text-slate-400 mt-0.5">
              Everyone currently enrolled in this class.
            </p>
          </div>
          {students.length === 0 ? (
            <div className="px-6 pb-6">
              <EmptyState icon={Users} title="No students in this class yet" />
            </div>
          ) : (
            <div className="divide-y divide-slate-100 max-h-[520px] overflow-y-auto">
              {students.map((s) => (
                <button
                  key={s.id}
                  type="button"
                  onClick={() => navigate(`/admin/students/${s.id}`)}
                  className="w-full text-left px-6 py-3 flex items-center gap-3 hover:bg-slate-50"
                >
                  <Avatar name={s.fullName} src={s.profilePictureUrl} size="sm" />
                  <div className="flex-1 min-w-0">
                    <p className="text-[13px] font-medium text-slate-800 truncate">{s.fullName}</p>
                    <p className="text-[11px] text-slate-400 font-mono">{s.admissionNumber}</p>
                  </div>
                  {!s.isActive && <Badge variant="neutral">Inactive</Badge>}
                </button>
              ))}
            </div>
          )}
        </Card>

        {/* Subjects selection */}
        <Card padding="none">
          <div className="p-6 pb-4 flex items-start justify-between">
            <div>
              <h3 className="text-[15px] font-semibold text-slate-900">Subjects</h3>
              <p className="text-[12px] text-slate-400 mt-0.5">
                Tick the subjects taught in this class.
              </p>
            </div>
            <Button
              icon={Save}
              loading={savingSubjects}
              onClick={saveSubjects}
            >
              Save
            </Button>
          </div>

          <div className="divide-y divide-slate-100 max-h-[520px] overflow-y-auto">
            {subjects.map((s) => {
              const isOn = selectedSubjectIds.has(s.subjectId);
              return (
                <label
                  key={s.subjectId}
                  className={`px-6 py-3 flex items-center gap-3 cursor-pointer transition-colors ${isOn ? "bg-emerald-50/30" : "hover:bg-slate-50"}`}
                >
                  <Checkbox
                    checked={isOn}
                    onChange={() => toggleSubject(s.subjectId)}
                  />
                  <div className="flex-1 min-w-0">
                    <p className="text-[13px] font-medium text-slate-800">{s.name}</p>
                    {s.code && <p className="text-[11px] text-slate-400 font-mono mt-0.5">{s.code}</p>}
                  </div>
                  {isOn && <Check className="w-4 h-4 text-emerald-600" />}
                </label>
              );
            })}
          </div>
        </Card>
      </div>
    </div>
  );
}