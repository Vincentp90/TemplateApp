import { createFileRoute } from '@tanstack/react-router'
import StatsCard from '../../components/statsCard'
import { Suspense } from 'react'
import { Loading } from '../../components/tiny/loading';

export const Route = createFileRoute('/app/stats')({
  component: Stats,
})

function Stats() {
  return (
    <Suspense fallback={<Loading message="Loading stats..." />}>
      <StatsCard />
    </Suspense>
  )
}
