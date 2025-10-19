import { createFileRoute } from '@tanstack/react-router'
import WLItemsList from '../../components/wlItemsList'
import { Suspense } from 'react'
import { Loading02Icon } from "hugeicons-react";

export const Route = createFileRoute('/app/overview')({
  component: Overview,
})

function Overview() {
  return (
    <Suspense fallback={<div>Loading...<Loading02Icon size={48} /></div>}>
      <WLItemsList />
    </Suspense>
  )
}
