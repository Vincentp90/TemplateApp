// Search.test.tsx
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, vi, expect } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import Search from './search';
import { api } from '../api';
import { Suspense } from 'react';


vi.mock('../api', () => ({
  api: {
    get: vi.fn(),
    post: vi.fn(),
    delete: vi.fn(),
  },
}));
const mockedApiGet = vi.mocked(api.get);

vi.mock('lodash.debounce', () => ({
  // eslint-disable-next-line @typescript-eslint/no-unsafe-function-type
  default: (fn: Function) => fn, // call immediately
}));

const renderWithClient = (ui: React.ReactNode) => {
  const client = new QueryClient();
  return render(
    <QueryClientProvider client={client}>
      <Suspense fallback={<div>Loading...</div>}>
        {ui}
      </Suspense>
    </QueryClientProvider>);
};

describe('Search component', () => {
  it('renders input and wishlist header', async () => {
    // Even though the following returns empty data, we still need it otherwise the suspense will wait forever for the Search useSuspsenseQuery to finish
    mockedApiGet.mockResolvedValueOnce({ data: { value: [] } });
    renderWithClient(<Search />);

    // We need to use find here because it will wait until the input is rendered (if we await)
    expect(await screen.findByPlaceholderText(/type to search/i)).toBeInTheDocument();
    expect(screen.getAllByText(/wishlist/i)[0]).toBeInTheDocument();
  });

  it('only fetches appId and name from wishlist (no unnecessary fields)', async () => {
    // The search page only needs appId and name from the wishlist.
    // This test ensures we don't accidentally fetch dateAdded or alertRuleId,
    // which would waste bandwidth and expose unnecessary data.
    mockedApiGet.mockResolvedValueOnce({ data: { value: [] } });
    renderWithClient(<Search />);
    renderWithClient(<Search />);

    await waitFor(() => {
      expect(api.get).toHaveBeenCalled();
    });

    const wishlistCall = mockedApiGet.mock.calls.find(
      call => call[0]?.includes('wishlist')
    );
    expect(wishlistCall).toBeDefined();

    const url = wishlistCall![0] as string;

    // The URL must use OData $select to only request appId and name
    expect(url).toMatch(/\$select=appId,name/);

    // It must NOT include fields we don't need
    expect(url).not.toMatch(/dateAdded/i);
    expect(url).not.toMatch(/alertRuleId/i);
  });

  it('calls API when user types more than 2 chars', async () => {
    mockedApiGet.mockResolvedValueOnce({
      data: {
        value: [
          { appId: 1, name: 'Half-Life' },
          { appId: 2, name: 'Portal' },
        ],
      },
    });
    renderWithClient(<Search />);

    await waitFor(() => {
      expect(api.get).toHaveBeenNthCalledWith(2, '/wishlist?$select=appId,name');
    });

    const input = await screen.findByPlaceholderText(/type to search/i);

    await userEvent.type(input, 'hal');
    await waitFor(() => {
      expect(api.get).toHaveBeenCalledWith('/applistings/search/hal');
    });

    await userEvent.type(input, 'f');
    await waitFor(() => {
      expect(api.get).toHaveBeenCalledWith('/applistings/search/half');
    });
  });

  it('renders fetched results', async () => {
    mockedApiGet.mockResolvedValueOnce({ data: { value: [] } }); // wishlist
    mockedApiGet.mockResolvedValue({
      data: [{ appId: 1, name: 'Half-Life' }]
    }); // search (returns plain array, not OData)

    renderWithClient(<Search />);

    const input = await screen.findByPlaceholderText(/type to search/i);

    await userEvent.type(input, 'half');
    await waitFor(() => {
      expect(screen.getByText('Half-Life')).toBeInTheDocument();
    });
  });
});
