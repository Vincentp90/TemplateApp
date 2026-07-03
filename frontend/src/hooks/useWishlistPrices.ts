import { useQuery } from '@tanstack/react-query';
import { api } from '../api';

export interface WishlistPriceItem {
  appId: number;
  name: string;
  currentPrice: number | null;
  currency: string;
  lastCheckedAt: string | null;
  hasSnapshots: boolean;
  alertRuleId: string | null;
}

export function useWishlistPrices(userId: string) {
  return useQuery<WishlistPriceItem[]>({
    queryKey: ['wishlistPrices', userId],
    queryFn: async () => {
      const res = await api.get(`/api/wishlist?userId=${encodeURIComponent(userId)}`);
      return res.data;
    },
    enabled: !!userId,
  });
}
