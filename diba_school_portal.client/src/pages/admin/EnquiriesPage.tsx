import { useEffect, useState } from "react";
import { Inbox, Mail, Phone, Filter } from "lucide-react";
import PageHeader from "../../components/PageHeader";
import {
  Button, Card, EmptyState, Pagination, SearchInput, Select, Badge, Skeleton,
} from "../../components/ui";
import { useDebounce } from "../../hooks/useDebounce";
import { formatDate } from "../../lib/format";
import api from "../../lib/api";

interface Enquiry {
  id: string;
  type: string;
  childFirstName: string | null;
  childLastName: string | null;
  section: string | null;
  parentName: string;
  parentEmail: string;
  parentPhone: string;
  subject: string | null;
  message: string;
  status: string;
  createdAt: string;
  contactedAt: string | null;
}

export default function EnquiriesPage() {
  const [rows, setRows] = useState<Enquiry[]>([]);
  const [loading, setLoading] = useState(true);
  const [page, setPage] = useState(1);
  const [total, setTotal] = useState(0);
  const [pageSize] = useState(20);
  const [search, setSearch] = useState("");
  const debounced = useDebounce(search, 400);
  const [type, setType] = useState("");
  const [status, setStatus] = useState("");

  useEffect(() => {
    setLoading(true);
    (async () => {
      try {
        const params = new URLSearchParams({
          pageNumber: String(page),
          pageSize: String(pageSize),
        });
        if (debounced.trim()) params.set("search", debounced.trim());
        if (type) params.set("type", type);
        if (status) params.set("status", status);

        const { data } = await api.get(`/enquiries?${params}`);
        if (data.success) {
          setRows(data.data.items);
          setTotal(data.data.totalCount);
        }
      } finally {
        setLoading(false);
      }
    })();
  }, [debounced, type, status, page, pageSize]);

  return (
    <div className="space-y-6">
      <PageHeader
        eyebrow="Admissions"
        title="Enquiries inbox"
        description="Every admission enquiry and contact message from the public site."
        actions={<Button variant="secondary" icon={Filter}>Export</Button>}
      />

      <Card padding="sm">
        <div className="flex flex-col lg:flex-row gap-3">
          <div className="flex-1">
            <SearchInput
              placeholder="Search by name, email, or phone..."
              value={search}
              onChange={(e) => { setSearch(e.target.value); setPage(1); }}
            />
          </div>
          <div className="w-full lg:w-48">
            <Select
              value={type}
              onChange={(e) => { setType(e.target.value); setPage(1); }}
              options={[
                { value: "", label: "All types" },
                { value: "Admission", label: "Admissions" },
                { value: "Contact", label: "Contact" },
              ]}
            />
          </div>
          <div className="w-full lg:w-48">
            <Select
              value={status}
              onChange={(e) => { setStatus(e.target.value); setPage(1); }}
              options={[
                { value: "", label: "Any status" },
                { value: "New", label: "New" },
                { value: "Contacted", label: "Contacted" },
                { value: "Enrolled", label: "Enrolled" },
                { value: "Lost", label: "Lost" },
              ]}
            />
          </div>
        </div>
      </Card>

      <Card padding="none">
        {loading ? (
          <div className="p-6 space-y-3">
            {Array.from({ length: 5 }).map((_, i) => <Skeleton key={i} className="h-20" />)}
          </div>
        ) : rows.length === 0 ? (
          <EmptyState
            icon={Inbox}
            title="No enquiries yet"
            description="New admissions and contact enquiries will appear here."
          />
        ) : (
          <>
            <div className="divide-y divide-surface-100">
              {rows.map((e) => (
                <div key={e.id} className="p-5 hover:bg-surface-50/50 transition-colors">
                  <div className="flex items-start justify-between gap-4 mb-3">
                    <div className="flex items-start gap-3 min-w-0">
                      <div className="w-10 h-10 rounded-xl bg-brand-50 grid place-items-center shrink-0">
                        <Inbox className="w-4 h-4 text-brand-600" />
                      </div>
                      <div className="min-w-0">
                        <div className="flex items-center gap-2 flex-wrap">
                          <p className="text-[14px] font-semibold text-surface-900">{e.parentName}</p>
                          <Badge variant={e.type === "Admission" ? "emerald" : "sky"}>{e.type}</Badge>
                          <Badge
                            variant={
                              e.status === "New" ? "amber" :
                              e.status === "Contacted" ? "sky" :
                              e.status === "Enrolled" ? "emerald" : "neutral"
                            }
                          >
                            {e.status}
                          </Badge>
                        </div>
                        {e.childFirstName && (
                          <p className="text-[12px] text-surface-500 mt-0.5">
                            Applying for <strong>{e.childFirstName} {e.childLastName}</strong>
                            {e.section && ` - ${e.section}`}
                          </p>
                        )}
                        {e.subject && (
                          <p className="text-[12px] text-surface-500 mt-0.5">Subject: {e.subject}</p>
                        )}
                      </div>
                    </div>
                    <span className="text-[11px] text-surface-400 whitespace-nowrap shrink-0">
                      {formatDate(e.createdAt)}
                    </span>
                  </div>

                  <p className="text-[13px] text-surface-700 leading-relaxed line-clamp-2 mb-3">
                    {e.message}
                  </p>

                  <div className="flex items-center gap-4 flex-wrap text-[12px] text-surface-500">
                    <a href={`mailto:${e.parentEmail}`} className="inline-flex items-center gap-1.5 hover:text-brand-700">
                      <Mail className="w-3 h-3" />{e.parentEmail}
                    </a>
                    {e.parentPhone && (
                      <a href={`tel:${e.parentPhone}`} className="inline-flex items-center gap-1.5 hover:text-brand-700">
                        <Phone className="w-3 h-3" />{e.parentPhone}
                      </a>
                    )}
                  </div>
                </div>
              ))}
            </div>
            <div className="px-6">
              <Pagination pageNumber={page} pageSize={pageSize} totalCount={total} onPageChange={setPage} />
            </div>
          </>
        )}
      </Card>
    </div>
  );
}