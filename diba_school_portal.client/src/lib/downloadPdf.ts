import api from "./api";

/**
 * POSTs a payload to one of the /pdfreport endpoints and triggers
 * a file download in the browser.
 */
export async function downloadPdf(
  endpoint: string,
  payload: unknown,
  fallbackName = "download.pdf"
): Promise<void> {
  const res = await api.post(endpoint, payload, { responseType: "blob" });

  // Filename comes from Content-Disposition: attachment; filename="..."
  const disposition = (res.headers?.["content-disposition"] as string) ?? "";
  const match = disposition.match(/filename="?([^";]+)"?/i);
  const filename = match?.[1] ?? fallbackName;

  const blob = new Blob([res.data], { type: "application/pdf" });
  const url = URL.createObjectURL(blob);
  const a = document.createElement("a");
  a.href = url;
  a.download = filename;
  document.body.appendChild(a);
  a.click();
  a.remove();
  URL.revokeObjectURL(url);
}