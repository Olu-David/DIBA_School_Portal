import { useState, type FormEvent, type ChangeEvent } from "react";
import { useNavigate } from "react-router-dom";
import { GraduationCap, ArrowLeft, Sparkles } from "lucide-react";
import PageHeader from "../../components/PageHeader";
import { Button, Card, FormField, Input, Select } from "../../components/ui";
import { useToast } from "../../hooks/useToast";
import api from "../../lib/api";
import type { ApiResponse } from "../../types/common";

const SECTIONS = [
  { value: "PreNursery", label: "Pre-Nursery" },
  { value: "Nursery", label: "Nursery" },
  { value: "Primary", label: "Primary" },
  { value: "JuniorSecondary", label: "Junior Secondary" },
  { value: "SeniorSecondary", label: "Senior Secondary" },
];

const STREAMS = [
  { value: "Science", label: "Science" },
  { value: "Arts", label: "Arts" },
  { value: "Commercial", label: "Commercial" },
  { value: "General", label: "General" },
];

export default function NewClassPage() {
  const navigate = useNavigate();
  const { toast } = useToast();
  const [name, setName] = useState("");
  const [section, setSection] = useState("");
  const [stream, setStream] = useState("");
  const [submitting, setSubmitting] = useState(false);

  const isSenior = section === "SeniorSecondary";

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    if (!name.trim() || !section) {
      toast({ variant: "error", title: "Name and section are required" });
      return;
    }
    if (isSenior && !stream) {
      toast({
        variant: "error",
        title: "Senior Secondary classes need a stream",
      });
      return;
    }

    setSubmitting(true);
    try {
      const { data } = await api.post<ApiResponse<string>>(
        "/schoolclass/create",
        {
          name: name.trim(),
          academicSection: section,
          stream: isSenior ? stream : null,
        }
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
            <FormField
              label="Class name"
              required
              hint={
                isSenior
                  ? "e.g. SS 1 Science, SS 2 Arts, SS 3 Commercial"
                  : "e.g. Nursery 1, Primary 3B, JSS 1A"
              }
            >
              <Input
                value={name}
                onChange={(e: ChangeEvent<HTMLInputElement>) =>
                  setName(e.target.value)
                }
                placeholder={
                  isSenior ? "SS 1 Science" : "Nursery 1"
                }
              />
            </FormField>

            <FormField label="Section" required>
              <Select
                value={section}
                onChange={(e) => {
                  setSection(e.target.value);
                  if (e.target.value !== "SeniorSecondary") setStream("");
                }}
                placeholder="Choose a section"
                options={SECTIONS}
              />
            </FormField>

            {isSenior && (
              <FormField
                label="Stream"
                required
                hint="Senior Secondary classes belong to a single stream"
              >
                <Select
                  value={stream}
                  onChange={(e) => setStream(e.target.value)}
                  placeholder="Choose a stream"
                  options={STREAMS}
                />
              </FormField>
            )}
          </div>
        </Card>

        {/* Helpful tip when SS is picked */}
        {isSenior && (
          <div className="flex items-start gap-3 p-4 rounded-xl bg-brand-50 border border-brand-100">
            <Sparkles className="w-4 h-4 text-brand-600 mt-0.5 shrink-0" />
            <p className="text-[12px] text-brand-800 leading-relaxed">
              Tip: create <strong>separate classes for each stream</strong> —
              "SS 1 Science", "SS 1 Arts", "SS 1 Commercial". Pupils are
              enrolled into the class that matches their stream, and the
              system fills in the rest automatically.
            </p>
          </div>
        )}

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