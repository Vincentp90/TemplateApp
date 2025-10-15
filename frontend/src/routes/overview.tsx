import { createFileRoute } from '@tanstack/react-router'
import WLItemsList from '../components/wlItemsList'

export const Route = createFileRoute('/overview')({
  component: Overview,
})

function Overview() {
  return (
    <WLItemsList />
  )
}
