import { createFileRoute } from '@tanstack/react-router'
import { Suspense } from 'react'
import { Loading } from '../../../components/tiny/loading'
import Profile from '../../../components/profile'

export const Route = createFileRoute('/app/profile/')({
  component: Home,
})

function Home() {
  return (
    <Suspense fallback={<Loading />}>
      <Profile />
    </Suspense>    
  )
}