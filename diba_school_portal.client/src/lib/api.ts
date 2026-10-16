import axios from "axios";

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? "/api",
  // ⚠️ REMOVED: headers: { "Content-Type": "application/json" }
  //    Setting Content-Type on the instance forces it on every request,
  //    which breaks FormData uploads (multipart needs a boundary).
});

// ---- Request: attach JWT + set Content-Type only when needed ----
api.interceptors.request.use((config) => {
  const token = localStorage.getItem("diba_token");
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  // If we're sending FormData, DELETE any Content-Type header so the
  // browser can set:
  //   Content-Type: multipart/form-data; boundary=----WebKitFormBoundaryXXXX
  // If you set it manually to "multipart/form-data", the boundary is lost
  // and the server returns 415 again.
  if (config.data instanceof FormData) {
    delete config.headers["Content-Type"];
  }
  // Otherwise: let axios auto-serialize JSON objects — it will set
  //   Content-Type: application/json
  // on its own. No need to set it here.

  return config;
});

// ---- Response: NEVER redirect. Let the app handle it. ----
api.interceptors.response.use(
  (response) => response,
  (error) => {
    const status = error.response?.status;
    const url: string = error.config?.url ?? "";
    const isAuthEndpoint = url.includes("/auth/");

    if (status === 401 && !isAuthEndpoint) {
      localStorage.removeItem("diba_token");
      localStorage.removeItem("diba_user");
    }

    // Log every failure to the console so you can debug
    console.error(
      "API ERROR:",
      error.config?.method?.toUpperCase(),
      url,
      "→",
      status ?? "NO RESPONSE",
      error.response?.data ?? error.message
    );

    // Extra hint for 415s — shows what Content-Type actually went out
    if (status === 415) {
      console.error(
        "   ↳ Content-Type sent:",
        error.config?.headers?.["Content-Type"] ?? "(not set)",
        "| Data type:",
        Object.prototype.toString.call(error.config?.data)
      );
    }

    return Promise.reject(error);
  }
);

export default api;