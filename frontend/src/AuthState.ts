import { create } from "zustand";

type AuthState = {
  isAuthenticated: boolean;
  user: string | null;
  setAuthenticated: (value: boolean) => void;
  setUser: (user: string | null) => void;
  reset: () => void;
};

export const useAuthStore = create<AuthState>((set) => ({
  isAuthenticated: false,
  user: null,
  setAuthenticated: (value) => set({ isAuthenticated: value }),
  setUser: (user) => set({ user }),
  reset: () => set({ isAuthenticated: false, user: null }),
}));