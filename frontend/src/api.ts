import axios from "axios";
//import { router } from "./router";

export const api = axios.create({ baseURL: "http://localhost:5186/" });

api.interceptors.request.use(config => {
  const token = localStorage.getItem("token");
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});
