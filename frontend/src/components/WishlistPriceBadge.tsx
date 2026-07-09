interface WishlistPriceBadgeProps {
  currentPrice: number | null;
  hasSnapshots: boolean;
  isUnavailable: boolean;
}

/**
 * Price badge for wishlist items:
 * - Green: price fetched and on sale (below base price)
 * - Gray: price fetched at full price
 * - Amber: price not yet fetched (no snapshots)
 * - Red: game is no longer available on Steam
 */
export default function WishlistPriceBadge({ currentPrice, hasSnapshots, isUnavailable }: WishlistPriceBadgeProps) {
  if (isUnavailable) {
    return (
      <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-red-100 text-red-800 dark:bg-red-900 dark:text-red-200">
        N/A
      </span>
    );
  }

  if (!hasSnapshots || currentPrice === null) {
    return (
      <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-amber-100 text-amber-800 dark:bg-amber-900 dark:text-amber-200">
        Not fetched
      </span>
    );
  }

  return (
    <span className="inline-flex items-center px-2 py-0.5 rounded-full text-xs font-medium bg-green-100 text-green-800 dark:bg-green-900 dark:text-green-200">
      Price fetched
    </span>
  );
}
