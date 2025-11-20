import { useMutation, useQueryClient, useSuspenseQuery } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import { api } from "../api";
import { useInterval } from "./tiny/useInterval";
import WlButton from "./tiny/wlButton";
import type { AxiosError } from "axios";
import { HubConnectionBuilder } from '@microsoft/signalr';

type Auction = {
    ID: number;
    startDate: Date;
    endDate: Date;
    userHasBid: boolean;
    startingPrice: number;
    currentPrice: number;
    appID: number;
    appName: string;
    rowVersion: number;
};

export function AuctionLive() {
    const queryClient = useQueryClient();
    const [bid, setBid] = useState("");
    const [isSubmitting, setIsSubmitting] = useState(false);

    const { data: currentAuction } = useSuspenseQuery<Auction>({
        queryKey: ['currentauction'],
        queryFn: async () => {
            const res = await api.get("/auction");
            const data = res.data;
            //setSecondsLeft((new Date(data.endDate).getTime() - Date.now()) / 1000);
            return data;
        },
    });

    const endtime = new Date(currentAuction.endDate).getTime();
    const [secondsLeft, setSecondsLeft] = useState<number>((endtime - Date.now()) / 1000);

    useInterval(() => {
        setSecondsLeft(secondsLeft > 0 ? secondsLeft - 1 : 0);
    }, 1000);

    const addMutation = useMutation({
        mutationFn: async (auction: Auction) => {
            setIsSubmitting(true);
            await api.post(`/auction`, auction);
            return auction;
        },
        onMutate: async (auction) => {
            await queryClient.cancelQueries({ queryKey: ['currentauction'] });
            queryClient.setQueryData(['currentauction'], auction);
            return auction;
        },
        onError: async (err: unknown, submittedAuction) => {            
            const axiosError = err as AxiosError;
            if(axiosError.status !== 409) 
                return;

            // Handle concurrency error. Refech current auction and if price is lower than our bid, resubmit with updated RowVersion stamp
            // if price is higher, show message to user that other bid is higher
            await queryClient.refetchQueries({ queryKey: ['currentauction'] });
            const latestAuction = queryClient.getQueryData<Auction>(['currentauction']);
                            
            if (latestAuction!.currentPrice < Number(bid)) {
                const updatedAuction = {
                    ...submittedAuction,
                    currentprice: bid,
                    rowVersion: latestAuction!.rowVersion,
                };
                queryClient.setQueryData(['currentauction'], updatedAuction);
                addMutation.mutate(updatedAuction);
            } else {
                alert('Another bidder placed a higher offer before you.');
                queryClient.setQueryData(['currentauction'], latestAuction);
            }            
        },
        onSettled: () => {
            queryClient.invalidateQueries({ queryKey: ['currentauction'] });
            setIsSubmitting(false);
            setBid("");
        },
    });

    useEffect(() => {
        const url = api.getUri() + "/auctionHub"
        console.log(url);
        const connection = new HubConnectionBuilder()
            .withUrl(url)
            .withAutomaticReconnect()
            .build();

        connection.start();

        connection.on("AuctionUpdated", async () => {
            console.log("AuctionUpdated");
            await queryClient.refetchQueries({ queryKey: ['currentauction'] });
        });

        return () => {
            connection.stop();
        };
    }, [queryClient]);

    const submitBid = (bid: number) => {
        const auction = { ...currentAuction, currentprice: bid };
        setBid(bid.toString());
        addMutation.mutate(auction);
    };

    const minutes = Math.floor(secondsLeft / 60);
    const seconds = (secondsLeft % 60).toFixed(0);

    const currentPriceMult = (mult: number) => (Math.max(currentAuction?.currentPrice, currentAuction?.startingPrice) * mult)?.toFixed(2);

    const simulateBid = async () => api.get('auction/simulatebid');

    return (
        <div className={`max-w-md mx-auto p-4 rounded-2xl shadow-md border ${currentAuction.userHasBid ? "border-green-500" : "border-gray-300"}`}>
            <div className="mb-4">
                <h2 className="text-xl font-semibold">{currentAuction?.currentPrice}</h2>
                <p className={`text-sm ${currentAuction.userHasBid ? "text-green-600" : "text-gray-500"}`}>
                    {currentAuction.userHasBid
                        ? "You hold the highest bid"
                        : "You are not the highest bidder"}
                </p>
            </div>

            <div className="space-y-1 mb-4">
                <h3>App for auction: {currentAuction.appName}</h3>
                <p>Starting price: ${currentAuction.startingPrice.toFixed(2)}</p>
                <p>Current price: ${currentAuction?.currentPrice?.toFixed(2)}</p>
                <p>
                    Time left: {minutes}:{seconds.toString().padStart(2, "0")}
                </p>
            </div>

            <div className="flex gap-2 mb-4">
                <input
                    type="number"
                    placeholder="Enter your bid"
                    value={bid}
                    onChange={(e) => setBid(e.target.value)}
                    disabled={isSubmitting}
                    className="flex-1 border rounded px-3 py-2 focus:outline-none focus:ring-2 focus:ring-blue-500"
                />
                <WlButton
                    onClick={() => submitBid(parseFloat(bid))}
                    disabled={isSubmitting || !bid}
                    isPrimary={true}
                >
                    Submit
                </WlButton>
            </div>

            <div className="flex justify-between">
                {[1.1, 1.4, 2.0].map((mult) => (
                    <WlButton className="m-2"
                        key={mult}
                        onClick={() =>
                            submitBid(parseFloat(currentPriceMult(mult)))
                        }
                        disabled={isSubmitting}
                        isPrimary={true}
                    >
                        +{(mult * 100 - 100).toFixed(0)}% (${currentPriceMult(mult)})
                    </WlButton>
                ))}
            </div>

            <WlButton onClick={() => simulateBid()} isPrimary={false} className="m-2">
                Simulate higher bid from other user
            </WlButton>
        </div>
    );
}
