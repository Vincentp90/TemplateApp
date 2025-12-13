import { createFileRoute } from '@tanstack/react-router'
import { Suspense } from 'react'
import { Loading } from '../../../components/tiny/loading'

export const Route = createFileRoute('/app/profile/edit')({
  component: Home,
})

function Home() {
  return (
    <Suspense fallback={<Loading />}>
      <h1>TODO EDIT</h1>
    </Suspense>    
  )
}