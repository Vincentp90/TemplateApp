import { Outlet, createFileRoute } from '@tanstack/react-router'
import Header from '../../layout/header.tsx'
import Sidebar from '../../layout/sidebar.tsx'
import Footer from '../../layout/footer.tsx'

export const Route = createFileRoute('/app')({
  component: AppLayout
})

function AppLayout() {
  return (
    <div className="flex flex-col min-h-screen">
      <Header />
      <div className="flex flex-1">
        <Sidebar />
        <main className="flex-1 p-4">
          <Outlet />
        </main>
      </div>
      <Footer />
    </div>
  )
}