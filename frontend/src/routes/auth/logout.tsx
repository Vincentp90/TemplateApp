import { createFileRoute, redirect } from '@tanstack/react-router'
import { queryClient } from '../../queryClient'
import { api } from '../../api';
import { useAuthStore } from '../../AuthState';

export const Route = createFileRoute('/auth/logout')({
    component: RouteComponent,
    beforeLoad: async () => {
        useAuthStore.getState().reset();
        await api.post("/auth/logout");
        queryClient.clear();
        throw redirect({ to: '/auth/login' })
    },
})

function RouteComponent() {
    return <div>Hello "/auth/logout"!</div>
}
