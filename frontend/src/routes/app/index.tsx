import { createFileRoute } from '@tanstack/react-router'
import Search from '../../components/search'
import { Suspense } from 'react'
import { Loading } from '../../components/tiny/loading'

export const Route = createFileRoute('/app/')({
  component: Home,
})

function Home() {
  return (
    <Suspense fallback={<Loading />}>
      <Search />
    </Suspense>    
  )
}