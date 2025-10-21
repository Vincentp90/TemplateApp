'use client';

import { useSuspenseQuery } from '@tanstack/react-query';
import { api } from "../api";

//TODO move to separate file
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

  return (
    <>
      <div className="flex flex-col gap-2">
        <h2 className="text-xl font-semibold">Wishlist</h2>
        <div className="inline-block rounded-lg bg-white shadow overflow-hidden border border-gray-200">
          {/* Header */}
          <div className="grid grid-cols-[auto_auto] bg-gray-100 text-gray-700 font-semibold px-6 py-3 border-b border-gray-200">
            <span className="whitespace-nowrap">Name</span>
            <span className="whitespace-nowrap text-right">Date Added</span>
          </div>

          {/* Rows */}
          {wishlistItems.map((s, i) => (
            <div
              key={i}
              className={`grid grid-cols-[auto_auto] px-6 py-3 items-center ${
                i % 2 === 0 ? "bg-white" : "bg-gray-50"
              } hover:bg-gray-100 transition-colors duration-200`}
            >
              <span className="min-w-0 truncate">{s.name}</span>
              <span className="min-w-0 truncate text-right">{s.dateadded}</span>
            </div>
          ))}
        </div>
      </div>
    </>
  );
}