import { useState } from 'react';
import { api } from '../api';

interface AlertRuleModalProps {
  appId: number;
  userId: string;
  onClose: () => void;
  onSuccess: () => void;
}

export default function AlertRuleModal({ appId, userId, onClose, onSuccess }: AlertRuleModalProps) {
  const [threshold, setThreshold] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setLoading(true);

    try {
      const amount = parseFloat(threshold);
      if (isNaN(amount) || amount <= 0) {
        setError('Please enter a valid positive price.');
        return;
      }

      await api.post(`/api/wishlist/${encodeURIComponent(userId)}/games/${appId}/alert`, null, {
        params: { thresholdAmount: amount, currency: 'EUR' },
      });

      onSuccess();
      onClose();
    } catch (err: unknown) {
      if (err instanceof Error) {
        setError(err.message || 'Failed to create alert rule.');
      } else {
        setError('Failed to create alert rule.');
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="bg-white dark:bg-gray-800 rounded-lg shadow-xl p-6 w-full max-w-sm">
        <h3 className="text-lg font-semibold mb-4">Set Price Alert</h3>

        <form onSubmit={handleSubmit}>
          <label className="block text-sm font-medium mb-1">
            Alert me when price drops below
          </label>
          <div className="flex gap-2 mb-4">
            <input
              type="number"
              step="0.01"
              min="0"
              value={threshold}
              onChange={(e) => setThreshold(e.target.value)}
              placeholder="0.00"
              className="flex-1 rounded-md border border-gray-300 dark:border-gray-600 px-3 py-2 bg-white dark:bg-gray-700 text-gray-900 dark:text-gray-100 focus:outline-none focus:ring-2 focus:ring-blue-500"
              autoFocus
            />
            <span className="flex items-center text-gray-500 dark:text-gray-400">EUR</span>
          </div>

          {error && <p className="text-red-500 text-sm mb-3">{error}</p>}

          <div className="flex justify-end gap-2">
            <button
              type="button"
              onClick={onClose}
              className="px-4 py-2 rounded-md border border-gray-300 dark:border-gray-600 text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700"
            >
              Cancel
            </button>
            <button
              type="submit"
              disabled={loading}
              className="px-4 py-2 rounded-md bg-blue-600 text-white hover:bg-blue-700 disabled:opacity-50"
            >
              {loading ? 'Saving...' : 'Set Alert'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
