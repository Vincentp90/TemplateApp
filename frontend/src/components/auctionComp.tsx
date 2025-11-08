import { useMutation, useQueryClient, useSuspenseQuery } from "@tanstack/react-query";
import { useState } from "react";
import { api } from "../api";
import { useInterval } from "./tiny/useInterval";

type Auction = {
    ID: number;
    startdate: Date;
    enddate: Date;
    userhasbid: boolean;
    startingPrice: number;
    currentPrice: number;
    appname: string;
};

export function AuctionComp() {
    const queryClient = useQueryClient();
    const [bid, setBid] = useState("");
    const [secondsLeft, setSecondsLeft] = useState<number>(60 * 30);
    const [isSubmitting, setIsSubmitting] = useState(false);

    //const activePrice = currentPrice || startingPrice;

    const { data: currentAuction } = useSuspenseQuery<Auction>({
        queryKey: ['currentauction'],
        queryFn: async () => {
            const res = await api.get("/auction");
            const data = res.data;
            setSecondsLeft((data.enddate - Date.now()) / 1000);
            console.log(data);
            return data;
        },
    });

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
        onError: () => {
          // TODO
        },
        onSettled: () => {
          queryClient.invalidateQueries({ queryKey: ['currentauction'] });
          setIsSubmitting(false);
          setBid("");
        },
      });

    const submitBid = (bid: number) => {
        const auction = { ...currentAuction, currentprice: bid };
        addMutation.mutate(auction);
    };

    const minutes = Math.floor(secondsLeft / 60);
    const seconds = secondsLeft % 60;

    return (
        <div className={`max-w-md mx-auto p-4 rounded-2xl shadow-md border ${currentAuction.userhasbid ? "border-green-500" : "border-gray-300"}`}>
            <div className="mb-4">
                <h2 className="text-xl font-semibold">{currentAuction?.currentPrice}</h2>
                <p className={`text-sm ${currentAuction.userhasbid ? "text-green-600" : "text-gray-500"}`}>
                    {currentAuction.userhasbid
                        ? "You hold the highest bid"
                        : "You are not the highest bidder"}
                </p>
            </div>

            <div className="space-y-1 mb-4">
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
                <button
                    onClick={() => submitBid(parseFloat(bid))}
                    disabled={isSubmitting || !bid}
                    className="px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 disabled:opacity-50"
                >
                    Submit
                </button>
            </div>

            <div className="flex justify-between">
                {[1.1, 1.4, 2.0].map((mult) => (
                    <button
                        key={mult}
                        onClick={() =>
                            submitBid(parseFloat((currentAuction?.currentPrice * mult)?.toFixed(2)))
                        }
                        disabled={isSubmitting}
                        className="flex-1 mx-1 px-3 py-2 bg-gray-100 rounded hover:bg-gray-200 disabled:opacity-50"
                    >
                        {mult * 100}% (${(currentAuction?.currentPrice * mult)?.toFixed(2)})
                    </button>
                ))}
            </div>
        </div>
    );
}
