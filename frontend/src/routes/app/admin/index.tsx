import { createFileRoute } from '@tanstack/react-router'
import { Suspense } from 'react'
import { Loading } from '../../../components/tiny/loading'
import UsersList from '../../../components/admin/UsersList'

export const Route = createFileRoute('/app/admin/')({
  component: RouteComponent,
})

function RouteComponent() {
  return (
      <Suspense fallback={<Loading />}>
        <UsersList />
      </Suspense>    
    )
}
