import { createFileRoute } from '@tanstack/react-router'
import { Suspense } from 'react'
import { Loading } from '../../../components/tiny/loading'
import ProfileEdit from '../../../components/profileEdit'

export const Route = createFileRoute('/app/profile/edit')({
  component: Home,
})

function Home() {
  return (
    <Suspense fallback={<Loading />}>
      <ProfileEdit />
    </Suspense>    
  )
}