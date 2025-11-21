import { Outlet, createFileRoute, redirect } from '@tanstack/react-router'
import Header from '../../layout/header.tsx'
import Sidebar from '../../layout/sidebar.tsx'
import Footer from '../../layout/footer.tsx'
import { api } from '../../api.ts'

export const Route = createFileRoute('/app')({
  component: AppLayout,
  beforeLoad: async () => {
    try {
      await api.get("/auth/me");//TODO store isAuthenticated in Zustand and use that after first check
    } catch {
      throw redirect({ to: "/auth/login" });
    }
  },
})

function AppLayout() {
  return (
    <div className="flex flex-col min-h-screen">
      <Header />
      <div className="flex flex-1 overflow-hidden">
        <Sidebar />
        <main className="flex-1 overflow-y-auto p-4">
          <Outlet />
        </main>
      </div>
      <Footer />
    </div>
  )
}