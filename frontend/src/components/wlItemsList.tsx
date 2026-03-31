'use client';

import { useSuspenseQuery } from '@tanstack/react-query';
import { api } from "../api";

type AppListingDetailed = { appid: number; name: string; dateadded: string };

export default function WLItemsList() {
  const { data: wishlistItems = [] } = useSuspenseQuery<AppListingDetailed[]>({
    queryKey: ['wishlistOverview'],
    queryFn: async () => {
      const res = await api.get("/wishlist?fields=name,dateadded");
      const data = res.data;
      return data.map((item: AppListingDetailed) => ({
        name: item.name,
        dateadded: item.dateadded,
      }));
    },
  });

  const toLocalTime = (date: string) => new Date(date).toLocaleString(undefined, {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  });

  return (
    <>
      <div className="flex flex-col gap-2">
        <h2 className="text-xl font-semibold">Wishlist</h2>
        <div className="inline-block rounded-lg shadow overflow-hidden border border-gray-200 dark:border-gray-800">
          {/* Header */}
          <div className="grid grid-cols-[auto_auto] bg-gray-100 dark:bg-gray-900 font-semibold px-6 py-3 border-b border-gray-200 dark:border-gray-800">
            <span className="whitespace-nowrap">Name</span>
            <span className="whitespace-nowrap text-right">Date Added</span>
          </div>

          {/* Rows */}
          {wishlistItems.map((s, i) => (
            <div
              key={i}
              className={`grid grid-cols-[auto_auto] px-6 py-3 items-center ${
                i % 2 === 0 ? "bg-white dark:bg-gray-700" : "bg-gray-50 dark:bg-gray-600"
              } hover:bg-gray-400 transition-colors duration-200`}
            >
              <span className="min-w-0 truncate">{s.name}</span>
              <span className="min-w-0 truncate text-right">{toLocalTime(s.dateadded)}</span>
            </div>
          ))}
        </div>
      </div>
    </>
  );
}