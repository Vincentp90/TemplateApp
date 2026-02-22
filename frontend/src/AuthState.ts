import { create } from "zustand";

export const UserRole = {
  Admin: "Admin",
  User: "User",
} as const;

export type UserRole = typeof UserRole[keyof typeof UserRole];

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
