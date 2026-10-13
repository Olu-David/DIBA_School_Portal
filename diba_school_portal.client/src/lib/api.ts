import axios from "axios";

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? "/api",
  headers: { "Content-Type": "application/json" },
});

// ---- Request: attach JWT ----
api.interceptors.request.use((config) => {
  const token = localStorage.getItem("diba_token");
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// ---- Response: NEVER redirect. Let the app handle it. ----
api.interceptors.response.use(
  (response) => response,
  (error) => {
    const status = error.response?.status;
    const url: string = error.config?.url ?? "";
    const isAuthEndpoint = url.includes("/auth/");

    // On 401 for a real endpoint (not a login attempt), clear the token.
    // Do NOT redirect — the app's route guards decide what to do next.
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

    return Promise.reject(error);
  }
);

export default api;