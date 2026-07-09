'use client';

import { useSuspenseQuery } from '@tanstack/react-query';
import { api } from "../api";
import WishlistPriceBadge from './WishlistPriceBadge';
import AlertRuleModal from './AlertRuleModal';
import { useState } from 'react';

export interface MergedWishlistItem {
  appId: number | null;
  name: string | null;
  dateAdded: string | null;
  price: number | null;
  priceCurrency: string;
  lastCheckedAt: string | null;
  isUnavailable: boolean;
  alertRuleId: string | null;
  alertThreshold: number | null;
  alertCurrency: string;
}

export default function WLItemsList() {
  const { data: wishlistItems = [] } = useSuspenseQuery<MergedWishlistItem[]>({
    queryKey: ['wishlistOverview'],
    queryFn: async () => {
      const res = await api.get("/wishlist?fields=appid,name,dateadded,price,pricecurrency,lastcheckedat,alertruleid,alertthreshold,alertcurrency");
      const data = res.data.items;
      return data.map((item: MergedWishlistItem) => ({
        appId: item.appId,
        name: item.name,
        dateAdded: item.dateAdded,
        price: item.price,
        priceCurrency: item.priceCurrency ?? 'EUR',
        lastCheckedAt: item.lastCheckedAt,
        isUnavailable: item.isUnavailable ?? false,
        alertRuleId: item.alertRuleId,
        alertThreshold: item.alertThreshold,
        alertCurrency: item.alertCurrency ?? 'EUR',
      }));
    },
  });

  const toLocalTime = (date: string | null) => {
    if (!date) return '—';
    return new Date(date).toLocaleString(undefined, {
      year: "numeric",
      month: "2-digit",
      day: "2-digit",
      hour: "2-digit",
      minute: "2-digit",
    });
  };

  const [alertModalAppId, setAlertModalAppId] = useState<number | null>(null);

  const handleAlertModalClose = () => setAlertModalAppId(null);

  const handleAlertSuccess = () => {
    setAlertModalAppId(null);
    // Invalidate and refetch the merged wishlist
    window.location.reload();
  };

  const hasUserId = true; // Auth is required for this route

  return (
    <>
      <div className="flex flex-col gap-2 items-center w-full">
        <h2 className="text-xl font-semibold">Wishlist</h2>
        <div className="rounded-lg shadow overflow-hidden border border-gray-200 dark:border-gray-800">
          <table className="w-full">
            {/* Header */}
            <thead>
              <tr className="bg-gray-100 dark:bg-gray-900 font-semibold border-b border-gray-200 dark:border-gray-800">
                <th className="text-left px-6 py-3 whitespace-nowrap">Name</th>
                <th className="text-center px-6 py-3 whitespace-nowrap">Price</th>
                <th className="text-center px-6 py-3 whitespace-nowrap">Status</th>
                <th className="text-right px-6 py-3 whitespace-nowrap">Date Added</th>
              </tr>
            </thead>
            {/* Rows */}
            <tbody>
              {wishlistItems.map((item, i) => {
                const currentPrice = item.price ?? null;
                const currency = item.priceCurrency ?? 'EUR';
                const hasSnapshots = item.lastCheckedAt != null;
                const alertRuleId = item.alertRuleId;

                const renderPrice = () => {
                  if (item.isUnavailable) return 'N/A';
                  if (currentPrice === null) return '—';
                  if (currentPrice === 0) return 'Free';
                  return `${currentPrice.toFixed(2)} ${currency}`;
                };

                return (
                  <tr
                    key={i}
                    className={`${
                      i % 2 === 0 ? "bg-white dark:bg-gray-700" : "bg-gray-50 dark:bg-gray-600"
                    } hover:bg-gray-400 transition-colors duration-200`}
                  >
                    <td className="px-6 py-3 min-w-0 truncate">{item.name ?? '—'}</td>
                    <td className="px-6 py-3 text-center tabular-nums">{renderPrice()}</td>
                    <td className="px-6 py-3">
                      <div className="flex items-center justify-center gap-2">
                        <WishlistPriceBadge currentPrice={currentPrice} hasSnapshots={hasSnapshots} isUnavailable={item.isUnavailable} />
                        {hasUserId && (
                          <button
                            onClick={() => setAlertModalAppId(item.appId ?? 0)}
                            className="text-xs text-blue-600 hover:text-blue-800 dark:text-blue-400 dark:hover:text-blue-300 underline"
                          >
                            {alertRuleId ? 'Edit alert' : 'Set alert'}
                          </button>
                        )}
                      </div>
                    </td>
                    <td className="px-6 py-3 text-right whitespace-nowrap">{toLocalTime(item.dateAdded)}</td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </div>

      {/* Alert Rule Modal */}
      {alertModalAppId != null && (
        <AlertRuleModal
          appId={alertModalAppId}
          onClose={handleAlertModalClose}
          onSuccess={handleAlertSuccess}
        />
      )}
    </>
  );
}