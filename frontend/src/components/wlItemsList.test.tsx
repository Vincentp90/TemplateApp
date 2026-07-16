// wlItemsList.test.tsx
import { render, screen, waitFor } from '@testing-library/react';
import { describe, it, vi, expect, beforeEach } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import WLItemsList from './wlItemsList';
import { api } from '../api';
import { Suspense } from 'react';

const mockedApiGet = vi.fn();

beforeEach(() => {
  vi.resetAllMocks();
  mockedApiGet.mockReset();
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  (api as any).get = mockedApiGet;
});

const renderWithClient = (ui: React.ReactNode) => {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={client}>
      <Suspense fallback={<div>Loading...</div>}>
        {ui}
      </Suspense>
    </QueryClientProvider>
  );
};

describe('WLItemsList component', () => {
  it('makes two separate API calls: wishlist and prices', async () => {
    mockedApiGet.mockImplementation((url: string) => {
      if (url === '/wishlist') {
        return Promise.resolve({
          data: {
            items: [
              { appId: 1, name: 'Half-Life', dateAdded: '2024-01-01T00:00:00Z' },
              { appId: 42, name: 'Portal', dateAdded: '2024-02-01T00:00:00Z' },
            ],
          },
        });
      }
      if (url.includes('/api/prices')) {
        return Promise.resolve({
          data: [
            { appId: 1, amount: 9.99, currency: 'EUR', lastCheckedAt: '2024-06-01T00:00:00Z', isUnavailable: false },
            { appId: 42, amount: null, currency: 'EUR', lastCheckedAt: null, isUnavailable: false },
          ],
        });
      }
      return Promise.reject(new Error('Unexpected URL: ' + url));
    });

    renderWithClient(<WLItemsList />);

    await waitFor(() => {
      expect(mockedApiGet).toHaveBeenCalledTimes(2);
    });

    expect(mockedApiGet).toHaveBeenNthCalledWith(1, '/wishlist');
    expect(mockedApiGet).toHaveBeenNthCalledWith(2, '/api/prices?appIds=1&appIds=42');

    expect(screen.getByText('Half-Life')).toBeInTheDocument();
    expect(screen.getByText('Portal')).toBeInTheDocument();
  });

  it('renders price from the prices endpoint', async () => {
    mockedApiGet.mockImplementation((url: string) => {
      if (url === '/wishlist') {
        return Promise.resolve({
          data: {
            items: [{ appId: 1, name: 'Half-Life', dateAdded: '2024-01-01T00:00:00Z' }],
          },
        });
      }
      if (url.includes('/api/prices')) {
        return Promise.resolve({
          data: [
            { appId: 1, amount: 19.99, currency: 'EUR', lastCheckedAt: '2024-06-01T00:00:00Z', isUnavailable: false },
          ],
        });
      }
      return Promise.reject(new Error('Unexpected URL: ' + url));
    });

    renderWithClient(<WLItemsList />);

    await waitFor(() => {
      expect(screen.getByText('19.99 EUR')).toBeInTheDocument();
    });
  });

  it('renders "—" when price is null', async () => {
    mockedApiGet.mockImplementation((url: string) => {
      if (url === '/wishlist') {
        return Promise.resolve({
          data: {
            items: [{ appId: 1, name: 'Half-Life', dateAdded: '2024-01-01T00:00:00Z' }],
          },
        });
      }
      if (url.includes('/api/prices')) {
        return Promise.resolve({
          data: [
            { appId: 1, amount: null, currency: 'EUR', lastCheckedAt: null, isUnavailable: false },
          ],
        });
      }
      return Promise.reject(new Error('Unexpected URL: ' + url));
    });

    renderWithClient(<WLItemsList />);

    await waitFor(() => {
      expect(screen.getByText('—')).toBeInTheDocument();
    });
  });

  it('renders "N/A" when price is unavailable', async () => {
    mockedApiGet.mockImplementation((url: string) => {
      if (url === '/wishlist') {
        return Promise.resolve({
          data: {
            items: [{ appId: 1, name: 'Half-Life', dateAdded: '2024-01-01T00:00:00Z' }],
          },
        });
      }
      if (url.includes('/api/prices')) {
        return Promise.resolve({
          data: [
            { appId: 1, amount: null, currency: 'EUR', lastCheckedAt: null, isUnavailable: true },
          ],
        });
      }
      return Promise.reject(new Error('Unexpected URL: ' + url));
    });

    renderWithClient(<WLItemsList />);

    await waitFor(() => {
      // N/A appears in both the price cell and the badge
      const nAElements = screen.getAllByText('N/A');
      expect(nAElements.length).toBeGreaterThanOrEqual(1);
    });
  });

  it('renders "Free" when price is 0', async () => {
    mockedApiGet.mockImplementation((url: string) => {
      if (url === '/wishlist') {
        return Promise.resolve({
          data: {
            items: [{ appId: 1, name: 'Half-Life', dateAdded: '2024-01-01T00:00:00Z' }],
          },
        });
      }
      if (url.includes('/api/prices')) {
        return Promise.resolve({
          data: [
            { appId: 1, amount: 0, currency: 'EUR', lastCheckedAt: '2024-06-01T00:00:00Z', isUnavailable: false },
          ],
        });
      }
      return Promise.reject(new Error('Unexpected URL: ' + url));
    });

    renderWithClient(<WLItemsList />);

    await waitFor(() => {
      expect(screen.getByText('Free')).toBeInTheDocument();
    });
  });

  it('handles empty wishlist gracefully', async () => {
    mockedApiGet.mockImplementation((url: string) => {
      if (url === '/wishlist') {
        return Promise.resolve({ data: { items: [] } });
      }
      // Prices query should not be called when there are no items
      return Promise.reject(new Error('Unexpected URL: ' + url));
    });

    renderWithClient(<WLItemsList />);

    await waitFor(() => {
      expect(screen.getByText('Wishlist')).toBeInTheDocument();
    });

    expect(screen.queryByText('Half-Life')).not.toBeInTheDocument();
  });

  it('handles items with no matching price data', async () => {
    mockedApiGet.mockImplementation((url: string) => {
      if (url === '/wishlist') {
        return Promise.resolve({
          data: {
            items: [
              { appId: 1, name: 'Half-Life', dateAdded: '2024-01-01T00:00:00Z' },
              { appId: 999, name: 'Unknown Game', dateAdded: '2024-01-01T00:00:00Z' },
            ],
          },
        });
      }
      if (url.includes('/api/prices')) {
        // Prices only has data for appId 1, not 999
        return Promise.resolve({
          data: [
            { appId: 1, amount: 9.99, currency: 'EUR', lastCheckedAt: '2024-06-01T00:00:00Z', isUnavailable: false },
          ],
        });
      }
      return Promise.reject(new Error('Unexpected URL: ' + url));
    });

    renderWithClient(<WLItemsList />);

    await waitFor(() => {
      expect(screen.getByText('Unknown Game')).toBeInTheDocument();
      expect(screen.getByText('—')).toBeInTheDocument(); // Unknown Game has no price
    });
  });
});
