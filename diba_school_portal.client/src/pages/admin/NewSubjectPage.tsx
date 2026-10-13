import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { BookOpen, ArrowLeft } from "lucide-react";
import PageHeader from "../../components/PageHeader";
import { Button, Card, FormField, Input } from "../../components/ui";
import { useToast } from "../../hooks/useToast";
import api from "../../lib/api";
import type { ApiResponse } from "../../types/common";

export default function NewSubjectPage() {
  const navigate = useNavigate();
  const { toast } = useToast();
  const [name, setName] = useState("");
  const [code, setCode] = useState("");
  const [submitting, setSubmitting] = useState(false);

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    if (!name.trim() || !code.trim()) {
      toast({ variant: "error", title: "Name and code are required" });
      return;
    }

    setSubmitting(true);
    try {
      const { data } = await api.post<ApiResponse<string>>(
        "/subject/create",
        { name: name.trim(), code: code.trim().toUpperCase() }
      );
      if (!data.success) {
        toast({ variant: "error", title: data.message ?? "Failed" });
        return;
      }
      toast({ variant: "success", title: "Subject created" });
      navigate("/admin/subjects");
    } catch (err: unknown) {
      const e2 = err as { response?: { data?: { message?: string } } };
      toast({
        variant: "error",
        title: e2?.response?.data?.message ?? "Could not create subject",
      });
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="max-w-2xl mx-auto space-y-6">
      <button
        type="button"
        onClick={() => navigate("/admin/subjects")}
        className="inline-flex items-center gap-1.5 text-[12px] text-surface-500 hover:text-surface-800"
      >
        <ArrowLeft className="w-3.5 h-3.5" />
        All subjects
      </button>

      <PageHeader
        eyebrow="Academics"
        title="Create new subject"
        description="Add a subject to the school catalogue."
      />

      <form onSubmit={submit} className="space-y-6">
        <Card>
          <div className="flex items-center gap-3 mb-5">
            <div className="w-9 h-9 rounded-xl bg-brand-50 grid place-items-center">
              <BookOpen className="w-4 h-4 text-brand-600" />
            </div>
            <h3 className="text-[15px] font-semibold text-surface-900">
              Subject details
            </h3>
          </div>

          <div className="space-y-4">
            <FormField label="Subject name" required hint="e.g. Mathematics, English Language">
              <Input
                value={name}
                onChange={(e) => setName(e.target.value)}
                placeholder="Mathematics"
              />
            </FormField>

            <FormField label="Subject code" required hint="Short unique code, e.g. MTH, ENG">
              <Input
                value={code}
                onChange={(e) => setCode(e.target.value.toUpperCase())}
                placeholder="MTH"
                className="font-mono"
              />
            </FormField>
          </div>
        </Card>

        <div className="flex justify-end gap-3">
          <Button
            type="button"
            variant="secondary"
            onClick={() => navigate("/admin/subjects")}
          >
            Cancel
          </Button>
          <Button type="submit" icon={BookOpen} loading={submitting}>
            Create subject
          </Button>
        </div>
      </form>
    </div>
  );
}