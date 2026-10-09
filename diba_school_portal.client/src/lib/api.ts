import axios from "axios";

const api = axios.create({
  baseURL: "/api",
  headers: { "Content-Type": "application/json" },
});

// Request interceptor — attach JWT if we have one
api.interceptors.request.use((config) => {
  const token = localStorage.getItem("diba_token");
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Response interceptor — pass errors through, don't redirect
api.interceptors.response.use(
  (response) => response,
  (error) => {
    // Log every failure to the console for debugging
    console.error(
      "API ERROR:",
      error.config?.method?.toUpperCase(),
      error.config?.url,
      "→",
      error.response?.status ?? "NO RESPONSE",
      error.response?.data ?? error.message
    );

    // 401 → clear stale token, but don't redirect (route guards handle that)
    if (error.response?.status === 401) {
      localStorage.removeItem("diba_token");
      localStorage.removeItem("diba_user");
    }

    return Promise.reject(error);
  }
);

export default api;