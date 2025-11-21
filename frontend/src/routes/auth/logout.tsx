import { createFileRoute, redirect } from '@tanstack/react-router'
import { queryClient } from '../../queryClient'
import { api } from '../../api';

export const Route = createFileRoute('/auth/logout')({
    component: RouteComponent,
    beforeLoad: async () => {
        await api.post("/auth/logout");
        queryClient.clear();
        throw redirect({ to: '/auth/login' })
    },
})

function RouteComponent() {
    return <div>Hello "/auth/logout"!</div>
}
