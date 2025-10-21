import { QueryCache, QueryClient } from "@tanstack/react-query";
import { router } from "./router";
import type { AxiosError } from "axios";


export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: false,
    },    
  },
  queryCache: new QueryCache({
    onError: (error) => {
      const axiosError = error as AxiosError;
      const status = axiosError.response?.status;

      if (status === 401) {
        localStorage.removeItem("token");
        router.navigate({ to: "/auth/login" });
      }
    },
  }),
});
