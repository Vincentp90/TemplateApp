import { createFileRoute } from '@tanstack/react-router'
import WLItemsList from '../../components/wlItemsList'
import { Suspense } from 'react'
import { Loading } from '../../components/tiny/loading';

export const Route = createFileRoute('/app/overview')({
  component: Overview,
})

function Overview() {
  return (
    <Suspense fallback={<Loading message="Loading overview..." />}>
      <WLItemsList />
    </Suspense>
  )
}
