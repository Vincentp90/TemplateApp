import { createFileRoute } from '@tanstack/react-router'
import LoginForm from '../../components/loginForm'

export const Route = createFileRoute('/auth/login')({
    component: RouteComponent,
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

