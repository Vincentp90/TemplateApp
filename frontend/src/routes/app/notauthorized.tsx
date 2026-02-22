import { createFileRoute } from '@tanstack/react-router'

export const Route = createFileRoute('/app/notauthorized')({
  component: RouteComponent,
})

function RouteComponent() {
  return (
    <div className="min-h-screen flex items-center justify-center bg-black">
      <h1 className="text-red-600 text-6xl font-bold uppercase">
        YOU ARE NOT AUTHORIZED
      </h1>
    </div>
  );
}