// Search.test.tsx
import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, it, vi, expect } from 'vitest';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import Search from './search';
import { api } from '../api';


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
  return render(<QueryClientProvider client={client}>{ui}</QueryClientProvider>);
};

describe('Search component', () => {
  it('renders input and wishlist header', async () => {
    renderWithClient(<Search />);
    expect(screen.getByPlaceholderText(/type to search/i)).toBeInTheDocument();
    expect(screen.getAllByText(/wishlist/i)[0]).toBeInTheDocument();

    await waitFor(() => {
      expect(api.get).toHaveBeenCalledWith('/wishlist?fields=appid,name');
    });
  });

  it('calls API when user types more than 2 chars', async () => {
    mockedApiGet.mockResolvedValueOnce({
      data: [
        { appid: 1, name: 'Half-Life' },
        { appid: 2, name: 'Portal' },
      ],
    });

    renderWithClient(<Search />);   

    await waitFor(() => {        
      expect(api.get).toHaveBeenNthCalledWith(2, '/wishlist?fields=appid,name');
    });

    const input = screen.getByPlaceholderText(/type to search/i);    
    
    await userEvent.type(input, 'hal');
    await waitFor(() => {
      expect(api.get).toHaveBeenCalledWith('/applisting/search/hal');
    });

    await userEvent.type(input, 'f');
    await waitFor(() => {
      expect(api.get).toHaveBeenCalledWith('/applisting/search/half');
    });
  });

  it('renders fetched results', async () => {
    mockedApiGet.mockResolvedValueOnce({
      data: [{ appid: 1, name: 'Half-Life' }],
    });

    renderWithClient(<Search />);

    const input = screen.getByPlaceholderText(/type to search/i);//TODO no longer works

    await userEvent.type(input, 'half');
    await waitFor(() => {
      expect(screen.getByText('Half-Life')).toBeInTheDocument();
    });
  });
});
