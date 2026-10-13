import { useEffect, useState } from "react";
import { useParams, useNavigate } from "react-router-dom";
import { ArrowLeft, Mail, Phone, GraduationCap, BookOpen, Users, Calendar } from "lucide-react";
import PageHeader from "../../components/PageHeader";
import { Card, Avatar, Badge, Skeleton, EmptyState, Button } from "../../components/ui";
import { formatDate } from "../../lib/format";
import api from "../../lib/api";
import type { ApiResponse } from "../../types/common";

interface TeacherDetail {
  id: string;
  userId: string;
  staffId: string;
  firstName: string;
  lastName: string;
  email: string | null;
  phoneNumber: string | null;
  profilePictureUrl: string | null;
  qualification: string;
  dateEmployed: string;
  primaryRole: string;
  isActive: boolean;
  subjectCount: number;
  classCount: number;
  subjects: { subjectId: string; subjectName: string; subjectCode: string }[];
  classes: { classId: string; className: string; section: number }[];
}

export default function TeacherDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [teacher, setTeacher] = useState<TeacherDetail | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!id) return;
    (async () => {
      try {
        const { data } = await api.get<ApiResponse<TeacherDetail>>(`/teacher/${id}`);
        if (data.success) setTeacher(data.data);
      } finally {
        setLoading(false);
      }
    })();
  }, [id]);

  if (loading) return <Card><Skeleton className="h-64" /></Card>;

  if (!teacher) {
    return (
      <Card>
        <EmptyState
          icon={Users}
          title="Teacher not found"
          action={<Button onClick={() => navigate("/admin/teachers")}>Back</Button>}
        />
      </Card>
    );
  }

  return (
    <div className="space-y-6">
      <button
        type="button"
        onClick={() => navigate("/admin/teachers")}
        className="inline-flex items-center gap-1.5 text-[12px] text-surface-500 hover:text-surface-800"
      >
        <ArrowLeft className="w-3.5 h-3.5" />
        All staff
      </button>

      <Card>
        <div className="flex flex-col sm:flex-row sm:items-center gap-5">
          <Avatar name={`${teacher.firstName} ${teacher.lastName}`} src={teacher.profilePictureUrl} size="xl" />
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-2 flex-wrap">
              <h1 className="text-2xl font-semibold text-surface-900">
                {teacher.firstName} {teacher.lastName}
              </h1>
              <Badge variant={teacher.isActive ? "emerald" : "neutral"}>
                {teacher.isActive ? "Active" : "Inactive"}
              </Badge>
              <Badge variant="violet">{teacher.primaryRole}</Badge>
            </div>
            <p className="mt-1 text-[13px] text-surface-500 font-mono">{teacher.staffId}</p>
            <div className="mt-3 flex items-center gap-4 flex-wrap text-[12px] text-surface-500">
              {teacher.email && (
                <span className="inline-flex items-center gap-1.5">
                  <Mail className="w-3.5 h-3.5" />{teacher.email}
                </span>
              )}
              {teacher.phoneNumber && (
                <span className="inline-flex items-center gap-1.5">
                  <Phone className="w-3.5 h-3.5" />{teacher.phoneNumber}
                </span>
              )}
              <span className="inline-flex items-center gap-1.5">
                <Calendar className="w-3.5 h-3.5" />Employed {formatDate(teacher.dateEmployed)}
              </span>
              {teacher.qualification && (
                <span className="inline-flex items-center gap-1.5">
                  <GraduationCap className="w-3.5 h-3.5" />{teacher.qualification}
                </span>
              )}
            </div>
          </div>
        </div>
      </Card>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card padding="none">
          <div className="p-6 pb-4">
            <h3 className="text-[15px] font-semibold text-surface-900 flex items-center gap-2">
              <BookOpen className="w-4 h-4 text-surface-500" />
              Subjects taught ({teacher.subjectCount})
            </h3>
          </div>
          {teacher.subjects.length === 0 ? (
            <div className="px-6 pb-6">
              <EmptyState icon={BookOpen} title="No subjects assigned" />
            </div>
          ) : (
            <div className="divide-y divide-surface-100">
              {teacher.subjects.map((s) => (
                <div key={s.subjectId} className="px-6 py-3.5 flex items-center justify-between">
                  <p className="text-[13px] font-medium text-surface-800">{s.subjectName}</p>
                  <span className="text-[11px] text-surface-400 font-mono">{s.subjectCode}</span>
                </div>
              ))}
            </div>
          )}
        </Card>

        <Card padding="none">
          <div className="p-6 pb-4">
            <h3 className="text-[15px] font-semibold text-surface-900 flex items-center gap-2">
              <Users className="w-4 h-4 text-surface-500" />
              Classes assigned ({teacher.classCount})
            </h3>
          </div>
          {teacher.classes.length === 0 ? (
            <div className="px-6 pb-6">
              <EmptyState icon={Users} title="No classes assigned" />
            </div>
          ) : (
            <div className="divide-y divide-surface-100">
              {teacher.classes.map((c) => (
                <button
                  key={c.classId}
                  type="button"
                  onClick={() => navigate(`/admin/classes/${c.classId}`)}
                  className="w-full text-left px-6 py-3.5 hover:bg-surface-50 flex items-center justify-between"
                >
                  <p className="text-[13px] font-medium text-surface-800">{c.className}</p>
                  <Badge variant="sky">View</Badge>
                </button>
              ))}
            </div>
          )}
        </Card>
      </div>
    </div>
  );
}