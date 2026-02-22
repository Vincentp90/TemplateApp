import { createFileRoute } from '@tanstack/react-router'
import { Suspense } from 'react'
import { Loading } from '../../../../components/tiny/loading'
import Profile from '../../../../components/profile'

export const Route = createFileRoute('/app/admin/profile/')({
  component: Home,
  validateSearch: (search: { userId?: string }) => ({
    userId: search.userId,
  }),
})

function Home() {
  const userId = Route.useSearch().userId

  return (
    <Suspense fallback={<Loading />}>
      <Profile userId={userId} />
    </Suspense>    
  )
}