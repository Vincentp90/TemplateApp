import { create } from "zustand";

type UserRole = "Admin" | "User";

type User = {
  username: string;
  role: UserRole;
};

type AuthState = {
  user: User | null;
  setUser: (user: User | null) => void;
  reset: () => void;
  isAuthenticated: () => boolean; // computed
};

export const useAuthStore = create<AuthState>((set, get) => ({
  user: null,
  setUser: (user) => set({ user }),
  reset: () => set({ user: null }),
  isAuthenticated: () => !!get().user,
}));
