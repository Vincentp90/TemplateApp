import { createFileRoute, Outlet } from '@tanstack/react-router'

export const Route = createFileRoute('/app/admin')({
  component: RouteComponent,
})// TODO redirect back to normal user /app index if not admin

function RouteComponent() {
  return <Outlet />
}
