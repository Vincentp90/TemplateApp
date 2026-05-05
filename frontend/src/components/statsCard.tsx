'use client';

import { useSuspenseQuery } from '@tanstack/react-query';
import { api } from "../api";

type WishlistStats = { 
  avgTimeAdded: string; 
  avgTimeBetweenAdded: string; 
  oldestItem: string; 
  mostCommonCharacter: string 
};

// TODO : Move to utils
const formatTimeSpan = (value: string | undefined) => {
  if (!value) return "0s";

  // Regex to match: [days.]hours:minutes:seconds
  const regex = /^(?:(\d+)\.)?(\d{2}):(\d{2}):(\d{2})/;
  const match = value.match(regex);

  if (!match) return value;

  const days = parseInt(match[1]) || 0;
  const hours = parseInt(match[2]);
  const minutes = parseInt(match[3]);

  const parts = [];
  if (days > 0) parts.push(`${days}d`);
  if (hours > 0) parts.push(`${hours}h`);
  if (minutes > 0 || parts.length === 0) parts.push(`${minutes}m`);

  return parts.join(' ');
};

export default function StatsCard() {
  const { data: wishlistStats } = useSuspenseQuery<WishlistStats>({
    queryKey: ['wishlistStats'],
    queryFn: async () => {
      const res = await api.get("/wishlist/stats");
      console.log(res.data);
      return res.data;
    },
  });

  return (
    <div className="p-6 max-w-md mx-auto bg-white rounded-xl shadow-md space-y-4 border border-gray-200">
      <h2 className="text-xl font-bold text-gray-800">Wishlist Stats</h2>
      
      <div className="grid grid-cols-1 gap-4">
        <div className="flex flex-col">
          <span className="text-sm text-gray-500 uppercase tracking-wide">Most Common Character</span>
          <span className="text-lg font-medium text-indigo-600">{wishlistStats.mostCommonCharacter}</span>
        </div>

        <div className="flex flex-col">
          <span className="text-sm text-gray-500 uppercase tracking-wide">Oldest Item</span>
          <span className="text-md text-gray-700 italic">"{wishlistStats.oldestItem}"</span>
        </div>

        <hr className="border-gray-100" />

        <div className="flex justify-between items-center">
          <span className="text-sm text-gray-600">Avg. Time Added</span>
          <span className="text-sm font-semibold">
            {formatTimeSpan(wishlistStats.avgTimeAdded)}
          </span>
        </div>

        <div className="flex justify-between items-center">
          <span className="text-sm text-gray-600">Avg. Gap Between Adds</span>
          <span className="text-sm font-semibold">
            {formatTimeSpan(wishlistStats.avgTimeBetweenAdded)}
          </span>
        </div>
      </div>
    </div>
  );
}