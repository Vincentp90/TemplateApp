'use client';

import { useSuspenseQuery } from '@tanstack/react-query';
import { api } from "../api";
import { useWishlistPrices } from '../hooks/useWishlistPrices';
import WishlistPriceBadge from './WishlistPriceBadge';
import AlertRuleModal from './AlertRuleModal';
import { useState } from 'react';

type AppListingDetailed = { appId: number; name: string; dateAdded: string };

export default function WLItemsList() {
  const { data: wishlistItems = [] } = useSuspenseQuery<AppListingDetailed[]>({
    queryKey: ['wishlistOverview'],
    queryFn: async () => {
      const res = await api.get("/wishlist?fields=name,dateadded");
      const data = res.data.items;
      return data.map((item: AppListingDetailed) => ({
        name: item.name,
        dateAdded: item.dateAdded,
      }));
    },
  });

  // Get SteamTracker price data (userId from auth state — default to empty for now)
  const [userId] = useState(() => {
    try {
      return localStorage.getItem('userId') || '';
    } catch {
      return '';
    }
  });

  const { data: priceItems = [] } = useWishlistPrices(userId);

  const toLocalTime = (date: string) => new Date(date).toLocaleString(undefined, {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
  });

  // Merge wishlist items with price data
  const priceMap = new Map(priceItems.map(p => [p.appId, p]));

  const [alertModalAppId, setAlertModalAppId] = useState<number | null>(null);

  const handleAlertModalClose = () => setAlertModalAppId(null);
  const handleAlertSuccess = () => {
    setAlertModalAppId(null);
    // Invalidate and refetch prices
    window.location.reload();
  };

  return (
    <>
      <div className="flex flex-col gap-2">
        <h2 className="text-xl font-semibold">Wishlist</h2>
        <div className="inline-block rounded-lg shadow overflow-hidden border border-gray-200 dark:border-gray-800">
          {/* Header */}
          <div className="grid grid-cols-[1fr_auto_auto_auto] bg-gray-100 dark:bg-gray-900 font-semibold px-6 py-3 border-b border-gray-200 dark:border-gray-800">
            <span className="whitespace-nowrap">Name</span>
            <span className="whitespace-nowrap text-center">Price</span>
            <span className="whitespace-nowrap text-center">Status</span>
            <span className="whitespace-nowrap text-right">Date Added</span>
          </div>

          {/* Rows */}
          {wishlistItems.map((item, i) => {
            const priceData = priceMap.get(item.appId);
            const currentPrice = priceData?.currentPrice ?? null;
            const hasSnapshots = priceData?.hasSnapshots ?? false;
            const currency = priceData?.currency ?? 'EUR';

            return (
              <div
                key={i}
                className={`grid grid-cols-[1fr_auto_auto_auto] px-6 py-3 items-center ${
                  i % 2 === 0 ? "bg-white dark:bg-gray-700" : "bg-gray-50 dark:bg-gray-600"
                } hover:bg-gray-400 transition-colors duration-200`}
              >
                <span className="min-w-0 truncate">{item.name}</span>
                <span className="text-center tabular-nums">
                  {currentPrice != null ? `${currentPrice.toFixed(2)} ${currency}` : '—'}
                </span>
                <div className="flex items-center justify-center gap-2">
                  <WishlistPriceBadge currentPrice={currentPrice} hasSnapshots={hasSnapshots} />
                  {userId && (
                    <button
                      onClick={() => setAlertModalAppId(item.appId)}
                      className="text-xs text-blue-600 hover:text-blue-800 dark:text-blue-400 dark:hover:text-blue-300 underline"
                    >
                      {priceData?.alertRuleId ? 'Edit alert' : 'Set alert'}
                    </button>
                  )}
                </div>
                <span className="min-w-0 truncate text-right">{toLocalTime(item.dateAdded)}</span>
              </div>
            );
          })}
        </div>
      </div>

      {/* Alert Rule Modal */}
      {alertModalAppId != null && (
        <AlertRuleModal
          appId={alertModalAppId}
          userId={userId}
          onClose={handleAlertModalClose}
          onSuccess={handleAlertSuccess}
        />
      )}
    </>
  );
}