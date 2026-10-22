import { useEffect, useState, type ChangeEvent } from "react";
import { useNavigate } from "react-router-dom";
import { BookOpen, Plus, Hash, Pencil, Trash2, X, AlertTriangle, Check } from "lucide-react";
import PageHeader from "../../components/PageHeader";
import { Button, Card, EmptyState, SearchInput, Skeleton, FormField, Input } from "../../components/ui";
import { useDebounce } from "../../hooks/useDebounce";
import { useToast } from "../../hooks/useToast";
import { useRole } from "../../hooks/useRole";
import api from "../../lib/api";
import type { ApiResponse } from "../../types/common";

interface Subject {
  id: string;
  name: string;
  code: string;
}

interface SubjectDto {
  id: string;
  name: string;
  code: string;
}

// Roles allowed to create/edit/delete subjects — mirrors backend [Authorize]
const SUBJECT_EDITOR_ROLES = ["SuperAdmin", "Principal"];

export default function SubjectsPage() {
  const navigate = useNavigate();
  const { toast } = useToast();
  const { role } = useRole();

  const canEdit = role != null && SUBJECT_EDITOR_ROLES.includes(role);

  const [subjects, setSubjects] = useState<Subject[]>([]);
  const [loading, setLoading] = useState(true);
  const [search, setSearch] = useState("");
  const debounced = useDebounce(search, 300);

  const [editing, setEditing] = useState<Subject | null>(null);
  const [editName, setEditName] = useState("");
  const [editCode, setEditCode] = useState("");
  const [saving, setSaving] = useState(false);

  const [deleting, setDeleting] = useState<Subject | null>(null);
  const [deletingBusy, setDeletingBusy] = useState(false);

  const load = async () => {
    setLoading(true);
    try {
      const { data } = await api.get<ApiResponse<Subject[]>>("/subject/all");
      if (data.success && Array.isArray(data.data)) setSubjects(data.data);
    } catch {
      /* silent */
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    load();
  }, []);

  const openEdit = (s: Subject) => {
    if (!canEdit) return;
    setEditing(s);
    setEditName(s.name);
    setEditCode(s.code);
  };

  const saveEdit = async () => {
    if (!canEdit || !editing) return;

    if (!editName.trim()) {
      toast({ variant: "error", title: "Name is required." });
      return;
    }
    if (!editCode.trim()) {
      toast({ variant: "error", title: "Code is required." });
      return;
    }

    setSaving(true);
    try {
      const payload: SubjectDto = {
        id: editing.id,
        name: editName.trim(),
        code: editCode.trim().toUpperCase(),
      };

      const { data } = await api.put<ApiResponse<Subject>>(
        "/subject/update",
        payload
      );

      if (!data.success) {
        toast({ variant: "error", title: data.message ?? "Failed to update." });
        return;
      }

      toast({ variant: "success", title: "Subject updated." });
      setEditing(null);
      await load();
    } catch (err: unknown) {
      const e2 = err as { response?: { data?: { message?: string } } };
      toast({
        variant: "error",
        title: e2?.response?.data?.message ?? "Could not update subject.",
      });
    } finally {
      setSaving(false);
    }
  };

  const confirmDelete = async () => {
    if (!canEdit || !deleting) return;

    setDeletingBusy(true);
    try {
      await api.delete(`/subject/soft-delete/${deleting.id}`);
      toast({ variant: "success", title: "Subject removed." });
      setDeleting(null);
      await load();
    } catch (err: unknown) {
      const e2 = err as {
        response?: { status?: number; data?: { message?: string } };
        message?: string;
      };
      const status = e2?.response?.status;
      toast({
        variant: "error",
        title:
          status === 404
            ? "Subject not found."
            : e2?.response?.data?.message ?? "Could not delete subject.",
      });
    } finally {
      setDeletingBusy(false);
    }
  };

  const filtered = debounced
    ? subjects.filter(
        (s) =>
          s.name.toLowerCase().includes(debounced.toLowerCase()) ||
          s.code.toLowerCase().includes(debounced.toLowerCase())
      )
    : subjects;

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Academics"
        title="Subjects"
        description="The full subject catalogue available across the school."
        actions={
          canEdit ? (
            <Button
              icon={Plus}
              onClick={() => navigate("/admin/subjects/new")}
            >
              New subject
            </Button>
          ) : undefined
        }
      />

      <div className="max-w-md">
        <SearchInput
          placeholder="Search subjects or codes..."
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
      </div>

      <Card padding="none">
        {loading ? (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4 p-6">
            {Array.from({ length: 6 }).map((_, i) => (
              <Skeleton key={i} className="h-14" />
            ))}
          </div>
        ) : filtered.length === 0 ? (
          <EmptyState
            icon={BookOpen}
            title={search ? "No subjects found" : "No subjects yet"}
            description={
              search
                ? `Nothing matches "${search}".`
                : canEdit
                ? "Add subjects to get started."
                : "The admin hasn't added any subjects yet."
            }
            action={
              canEdit ? (
                <Button
                  icon={Plus}
                  onClick={() => navigate("/admin/subjects/new")}
                >
                  New subject
                </Button>
              ) : undefined
            }
          />
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 divide-x divide-y divide-surface-100">
            {filtered.map((s) => (
              <div
                key={s.id}
                className="p-5 hover:bg-violet-50/40 transition-colors group"
              >
                <div className="flex items-start gap-3">
                  <div className="w-10 h-10 rounded-xl bg-violet-50 grid place-items-center shrink-0">
                    <BookOpen className="w-4 h-4 text-violet-600" strokeWidth={2} />
                  </div>
                  <div className="min-w-0 flex-1">
                    <p className="text-[14px] font-semibold text-surface-900 truncate">
                      {s.name}
                    </p>
                    {s.code && (
                      <p className="text-[11px] text-surface-400 font-mono mt-0.5 inline-flex items-center gap-1">
                        <Hash className="w-3 h-3" />
                        {s.code}
                      </p>
                    )}
                  </div>

                  {canEdit && (
                    <div className="flex items-center gap-1 shrink-0 opacity-100 lg:opacity-0 lg:group-hover:opacity-100 transition-opacity">
                      <button
                        type="button"
                        onClick={() => openEdit(s)}
                        title="Edit subject"
                        className="p-1.5 rounded-lg text-surface-400 hover:bg-white hover:text-violet-600 transition-colors"
                      >
                        <Pencil className="w-4 h-4" />
                      </button>
                      <button
                        type="button"
                        onClick={() => setDeleting(s)}
                        title="Delete subject"
                        className="p-1.5 rounded-lg text-surface-400 hover:bg-white hover:text-flame-600 transition-colors"
                      >
                        <Trash2 className="w-4 h-4" />
                      </button>
                    </div>
                  )}
                </div>
              </div>
            ))}
          </div>
        )}
      </Card>

      {canEdit && editing && (
        <>
          <div
            className="fixed inset-0 bg-surface-950/50 z-40"
            onClick={() => !saving && setEditing(null)}
            aria-hidden
          />
          <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
            <div className="bg-white rounded-2xl shadow-xl w-full max-w-md overflow-hidden">
              <div className="p-5 flex items-center justify-between border-b border-surface-100">
                <h2 className="text-[15px] font-semibold text-surface-900">
                  Edit subject
                </h2>
                <button
                  type="button"
                  onClick={() => !saving && setEditing(null)}
                  className="p-2 rounded-lg text-surface-400 hover:bg-surface-100"
                >
                  <X className="w-4 h-4" />
                </button>
              </div>

              <div className="p-5 space-y-4">
                <FormField label="Subject name" required>
                  <Input
                    value={editName}
                    onChange={(e: ChangeEvent<HTMLInputElement>) =>
                      setEditName(e.target.value)
                    }
                    placeholder="e.g. Mathematics"
                  />
                </FormField>

                <FormField label="Subject code" required>
                  <Input
                    value={editCode}
                    onChange={(e: ChangeEvent<HTMLInputElement>) =>
                      setEditCode(e.target.value.toUpperCase())
                    }
                    placeholder="e.g. MTH"
                    className="font-mono"
                  />
                </FormField>
              </div>

              <div className="p-5 border-t border-surface-100 flex justify-end gap-3">
                <Button
                  variant="secondary"
                  onClick={() => setEditing(null)}
                  disabled={saving}
                >
                  Cancel
                </Button>
                <Button icon={Check} loading={saving} onClick={saveEdit}>
                  Save changes
                </Button>
              </div>
            </div>
          </div>
        </>
      )}

      {canEdit && deleting && (
        <>
          <div
            className="fixed inset-0 bg-surface-950/50 z-40"
            onClick={() => !deletingBusy && setDeleting(null)}
            aria-hidden
          />
          <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
            <div className="bg-white rounded-2xl shadow-xl w-full max-w-md overflow-hidden">
              <div className="p-6">
                <div className="w-12 h-12 rounded-2xl bg-flame-50 grid place-items-center mb-4">
                  <AlertTriangle className="w-5 h-5 text-flame-600" />
                </div>
                <h2 className="text-[16px] font-semibold text-surface-900">
                  Delete this subject?
                </h2>
                <p className="mt-2 text-[13px] text-surface-600 leading-relaxed">
                  <strong className="text-surface-900">{deleting.name}</strong>{" "}
                  ({deleting.code}) will be soft-deleted. Existing student
                  enrolments and results are kept, but it will no longer appear
                  in subject lists or dropdowns.
                </p>
              </div>

              <div className="p-5 border-t border-surface-100 flex justify-end gap-3">
                <Button
                  variant="secondary"
                  onClick={() => setDeleting(null)}
                  disabled={deletingBusy}
                >
                  Cancel
                </Button>
                <Button
                  variant="danger"
                  icon={Trash2}
                  loading={deletingBusy}
                  onClick={confirmDelete}
                >
                  Yes, delete
                </Button>
              </div>
            </div>
          </div>
        </>
      )}
    </div>
  );
}