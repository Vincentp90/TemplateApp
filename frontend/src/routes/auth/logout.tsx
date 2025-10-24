import { createFileRoute, redirect } from '@tanstack/react-router'
import { queryClient } from '../../queryClient'

export const Route = createFileRoute('/auth/logout')({
    component: RouteComponent,
    beforeLoad: () => {
        localStorage.removeItem('token'); 
        queryClient.clear();
        throw redirect({ to: '/auth/login' })
    },
})

function RouteComponent() {
    return <div>Hello "/auth/logout"!</div>
}
