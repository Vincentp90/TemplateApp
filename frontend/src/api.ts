import axios from "axios";

axios.defaults.withCredentials = true;

export const api = axios.create({ baseURL: import.meta.env.VITE_API_URL });

api.interceptors.request.use(config => {
  config.withCredentials = true;//TODO check is this needed
  return config;
});
