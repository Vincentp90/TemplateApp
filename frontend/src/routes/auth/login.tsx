import { redirect, createFileRoute } from '@tanstack/react-router'
import { useAuthStore } from '../../AuthState.ts'
import { api } from '../../api.ts'
import LoginForm from '../../components/loginForm'

export const Route = createFileRoute('/auth/login')({
    component: RouteComponent,    
    beforeLoad: async () => {
        // If someone bookmarked the login page, send them immediatly to the app if they are still logged in
        try {
          const res = await api.get("/auth/me");
          const authStore = useAuthStore.getState();
          authStore.setUser({
            username: res.data.username,
            role: res.data.role,
          });      
        } catch {
          throw redirect({ to: "/auth/login" });
        }
        throw redirect({ to: "/app" });
      },
})

function RouteComponent() {
    return (
        <div className="flex items-center justify-center min-h-screen bg-gray-100">
            <div className="bg-white p-8 rounded-2xl shadow-lg w-full max-w-sm">
                <LoginForm />
            </div>
        </div>
    )
}

