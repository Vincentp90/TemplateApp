import { redirect, createFileRoute } from '@tanstack/react-router'

// TODO If authenticated, redirect to profile page
// If not redirect to /auth/login
export const Route = createFileRoute('/auth/')({
  component: RouteComponent,
  beforeLoad: () => {
    throw redirect({ to: '/auth/login' })
  },
})

function RouteComponent() {
  return <div>Hello "/auth/"!</div>
}
