import { createFileRoute } from "@tanstack/react-router"
import { AuctionLive } from "../../components/auctionLive"
import { Suspense } from "react"
import { Loading } from "../../components/tiny/loading"

export const Route = createFileRoute('/app/liveauction')({
    component: Auction,
})

function Auction() {
    return (
        <div className="flex items-center justify-center min-h-screen bg-gray-100">
            <div className="bg-white p-8 rounded-2xl shadow-lg w-full max-w-lg">
                <Suspense fallback={<Loading message="Loading auction..." />}>
                    <AuctionLive />
                </Suspense>
            </div>
        </div>
    )
}