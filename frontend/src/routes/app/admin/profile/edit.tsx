import { createFileRoute } from '@tanstack/react-router'
import { Suspense } from 'react'
import { Loading } from '../../../../components/tiny/loading'
import ProfileEdit from '../../../../components/profileEdit'

export const Route = createFileRoute('/app/admin/profile/edit')({
  component: RouteComponent,
  validateSearch: (search: { userId?: string }) => ({
    userId: search.userId,
  }),
})

function RouteComponent() {
  const userId = Route.useSearch().userId

  return (
      <Suspense fallback={<Loading />}>
        <ProfileEdit userId={userId} />
      </Suspense>    
    )
}
