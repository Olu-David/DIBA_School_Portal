import { useState, type FormEvent, type ChangeEvent } from "react";
import { useNavigate } from "react-router-dom";
import { GraduationCap, ArrowLeft } from "lucide-react";
import PageHeader from "../../components/PageHeader";
import { Button, Card, FormField, Input, Select } from "../../components/ui";
import { useToast } from "../../hooks/useToast";
import api from "../../lib/api";
import type { ApiResponse } from "../../types/common";

const SECTIONS = [
  { value: "Nursery", label: "Nursery" },
  { value: "Primary", label: "Primary" },
  { value: "JuniorSecondary", label: "Junior Secondary" },
  { value: "SeniorSecondary", label: "Senior Secondary" },
];

export default function NewClassPage() {
  const navigate = useNavigate();
  const { toast } = useToast();
  const [name, setName] = useState("");
  const [section, setSection] = useState("");
  const [submitting, setSubmitting] = useState(false);

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    if (!name.trim() || !section) {
      toast({ variant: "error", title: "Name and section are required" });
      return;
    }

    setSubmitting(true);
    try {
      const { data } = await api.post<ApiResponse<string>>(
        "/schoolclass/create",
        { name: name.trim(), academicSection: section }
      );
      if (!data.success) {
        toast({ variant: "error", title: data.message ?? "Failed" });
        return;
      }
      toast({ variant: "success", title: "Class created" });
      navigate("/admin/classes");
    } catch (err: unknown) {
      const e2 = err as { response?: { data?: { message?: string } } };
      toast({
        variant: "error",
        title: e2?.response?.data?.message ?? "Could not create class",
      });
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div className="max-w-2xl mx-auto space-y-6">
      <button
        type="button"
        onClick={() => navigate("/admin/classes")}
        className="inline-flex items-center gap-1.5 text-[12px] text-surface-500 hover:text-surface-800"
      >
        <ArrowLeft className="w-3.5 h-3.5" />
        All classes
      </button>

      <PageHeader
        eyebrow="Academics"
        title="Create new class"
        description="Add a class to the school. You can assign subjects and teachers afterwards."
      />

      <form onSubmit={submit} className="space-y-6">
        <Card>
          <div className="flex items-center gap-3 mb-5">
            <div className="w-9 h-9 rounded-xl bg-brand-50 grid place-items-center">
              <GraduationCap className="w-4 h-4 text-brand-600" />
            </div>
            <h3 className="text-[15px] font-semibold text-surface-900">
              Class details
            </h3>
          </div>

          <div className="space-y-4">
            <FormField label="Class name" required hint="e.g. JSS 1A, Primary 3B, SS 2 Science">
              <Input
                value={name}
                onChange={(e: ChangeEvent<HTMLInputElement>) => setName(e.target.value)}
                placeholder="JSS 1A"
              />
            </FormField>

            <FormField label="Section" required>
              <Select
                value={section}
                onChange={(e) => setSection(e.target.value)}
                placeholder="Choose a section"
                options={SECTIONS}
              />
            </FormField>
          </div>
        </Card>

        <div className="flex justify-end gap-3">
          <Button
            type="button"
            variant="secondary"
            onClick={() => navigate("/admin/classes")}
          >
            Cancel
          </Button>
          <Button type="submit" icon={GraduationCap} loading={submitting}>
            Create class
          </Button>
        </div>
      </form>
    </div>
  );
}