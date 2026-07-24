'use client';

import { useSuspenseQuery, useQuery, useQueryClient } from '@tanstack/react-query';
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
}

interface ODataResponse<T> {
  value: T[];
}

interface WishlistItemResponse {
  appId: number | null;
  name: string | null;
  dateAdded: string | null;
  alertRuleId: string | null;
}

interface GamePriceResponse {
  appId: number;
  amount: number | null;
  currency: string;
  lastCheckedAt: string | null;
  isUnavailable: boolean;
}

export default function WLItemsList() {
  const queryClient = useQueryClient();

  // Query 1: Get wishlist items (core fields + alert info)
  const { data: wishlistItems } = useSuspenseQuery<WishlistItemResponse[]>({
    queryKey: ['wishlistOverview'],
    queryFn: async () => {
      const res = await api.get<ODataResponse<WishlistItemResponse> | WishlistItemResponse[]>("/wishlist");
      const items = Array.isArray(res.data) ? res.data : res.data.value;
      return items;
    },
  });

  // Query 2: Get prices for all wishlist items
  const appIds = wishlistItems
    .map(item => item.appId)
    .filter((id): id is number => id != null);

  const { data: prices = [] } = useQuery<GamePriceResponse[]>({
    queryKey: ['prices', appIds],
    queryFn: async () => {
      if (appIds.length === 0) return [];
      const query = appIds.map(id => `appIds=${id}`).join('&');
      const res = await api.get(`/api/prices?${query}`);
      return res.data as GamePriceResponse[];
    },
    enabled: appIds.length > 0,
  });

  // Merge: build maps of appId -> price data and appId -> alert info
  const priceMap = new Map<number, GamePriceResponse>();
  for (const price of prices) {
    priceMap.set(price.appId, price);
  }

  const mergedItems: MergedWishlistItem[] = wishlistItems.map((item) => {
    const priceData = item.appId != null ? priceMap.get(item.appId) : undefined;
    return {
      appId: item.appId,
      name: item.name,
      dateAdded: item.dateAdded,
      price: priceData?.amount ?? null,
      priceCurrency: priceData?.currency ?? 'EUR',
      lastCheckedAt: priceData?.lastCheckedAt ?? null,
      isUnavailable: priceData?.isUnavailable ?? false,
      alertRuleId: item.alertRuleId,
    };
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
    // Invalidate and refetch both queries
    queryClient.invalidateQueries({ queryKey: ['wishlistOverview'] });
    queryClient.invalidateQueries({ queryKey: ['prices'] });
  };

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
              {mergedItems.map((item, i) => {
                const currentPrice = item.price ?? null;
                const currency = item.priceCurrency ?? 'EUR';
                const hasSnapshots = item.lastCheckedAt != null;

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
                        <button
                          onClick={() => setAlertModalAppId(item.appId ?? 0)}
                          className="text-xs text-blue-600 hover:text-blue-800 dark:text-blue-400 dark:hover:text-blue-300 underline"
                        >
                          {item.alertRuleId ? 'Edit alert' : 'Set alert'}
                        </button>
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
