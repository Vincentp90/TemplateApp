import { redirect } from 'next/navigation'

export default function Home() {
  redirect('/main'); //TODO redirect to login if not authenticated
}
