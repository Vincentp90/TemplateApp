import { createFileRoute, Outlet, redirect } from '@tanstack/react-router'
import { useAuthStore, UserRole } from '../../../AuthState';

export const Route = createFileRoute('/app/admin')({
  component: RouteComponent,
  beforeLoad: async () => {
    const authStore = useAuthStore.getState();
    if (!authStore.user) {
      throw redirect({ to: "/auth/login" });
    }

    if (authStore.user.role !== UserRole.Admin) {
      throw redirect({ to: "/app/notauthorized" });
    }
  },
})

function RouteComponent() {
  return <Outlet />
}
