import { createRootRoute, Outlet } from '@tanstack/react-router'
import { TanStackRouterDevtools } from '@tanstack/react-router-devtools'
import Header from '../layout/header.tsx'
import Sidebar from '../layout/sidebar.tsx'
import Footer from '../layout/footer.tsx'

const RootLayout = () => (
    <>
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
        <hr />        
        <TanStackRouterDevtools />
    </>
)

export const Route = createRootRoute({ component: RootLayout })