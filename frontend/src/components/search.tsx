'use client';

import { useState, useRef } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import debounce from 'lodash.debounce';

import { Loading02Icon } from "hugeicons-react";

//TODO move to separate file?
type AppListing = { appid: number; name: string };

const APIURL = "http://localhost:5186";//TODO put in better spot

const fetchSearchResults = async (query: string): Promise<AppListing[]> => {
  const res = await fetch(`${APIURL}/applisting/search/${encodeURIComponent(query)}`);
  const data = await res.json();
  return data.slice(0, 10);
};

export default function Search() {
  const [searchInputValue, setSearchInputValue] = useState('');
  const [searchQuery, setSearchQuery] = useState('');
  const queryClient = useQueryClient();

  const debounceQueryInput = useRef(
    debounce(async (q: string) => {
      setSearchQuery(q);
    }, 300)
  );

  const handleSearchInputChange = (input: string) =>{
    setSearchInputValue(input);
    if (searchInputValue.length > 2) debounceQueryInput.current(searchInputValue);
    else setSearchQuery('');
  };

  const { data: searchResults = [] } = useQuery<AppListing[]>({
    queryKey: ['search', searchQuery],
    queryFn: async () => fetchSearchResults(searchQuery),
    enabled: searchQuery.length > 2,
  });

  // Number 1 in ['wishlist', 1] to be replaced later with userId
  const { data: wishlistItems = [], isPending: wishlistItemsPending } = useQuery<AppListing[]>({
    queryKey: ['wishlist', 1],
    queryFn: async () => {
      const res = await fetch(`${APIURL}/wishlist?fields=appid,name`, {
        headers: { 'x-user-id': '1' },
      });
      const data = await res.json();
      return data.map((item: AppListing) => ({
        appid: item.appid,
        name: item.name,
      }));
    },
  });

  const addMutation = useMutation({
    mutationFn: async (appItem: AppListing) => {
      await fetch(`${APIURL}/wishlist/${appItem.appid}`, {
        method: 'POST',
        headers: { 'x-user-id': '1' },
      });
      return appItem;
    },
    onMutate: async (appItem) => {
      await queryClient.cancelQueries({ queryKey: ['wishlist', 1] });
      const previous = queryClient.getQueryData<AppListing[]>(['wishlist', 1]) || [];
      queryClient.setQueryData(['wishlist', 1], [...previous, appItem]);
      return { previous };
    },
    onError: (_err, _appItem, context) => {
      if (context?.previous)
        queryClient.setQueryData(['wishlist', 1], context.previous);
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: ['wishlist', 1] });
    },
  });

  const removeMutation = useMutation({
    mutationFn: async (appItem: AppListing) => {
      await fetch(`${APIURL}/wishlist/${appItem.appid}`, {
        method: 'DELETE',
        headers: { 'x-user-id': '1' },
      });
      return appItem;
    },
    onMutate: async (appItem) => {
      await queryClient.cancelQueries({ queryKey: ['wishlist', 1] });
      const previous = queryClient.getQueryData<AppListing[]>(['wishlist', 1]) || [];
      queryClient.setQueryData(
        ['wishlist', 1],
        previous.filter(i => i.appid !== appItem.appid)
      );
      return { previous };
    },
    onError: (_err, _appItem, context) => {
      if (context?.previous)
        queryClient.setQueryData(['wishlist', 1], context.previous);
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: ['wishlist', 1] });
    },
  });

  const addToWishlist = (appItem: AppListing) => {
    addMutation.mutate(appItem);
  };

  const removeFromWishlist = (appItem: AppListing) => {
    removeMutation.mutate(appItem);
  };

  return (
    <div className="grid grid-cols-2 gap-6 max-w-4xl">
      <div className="flex flex-col gap-4">
        <input
          type="text"
          value={searchInputValue}
          onChange={(e) => handleSearchInputChange(e.target.value)}
          placeholder="Type to search..."
          className="p-2 border rounded shadow-sm"
        />
        {searchResults.length > 0 && (
          <ul className="border rounded p-2 bg-white shadow">
            {searchResults.map((item: AppListing, idx) => (
              <li
                key={idx}
                value={item.appid}
                onClick={() => addToWishlist(item)}
                className="cursor-pointer hover:bg-gray-100 p-1"
              >
                {item.name}
              </li>
            ))}
          </ul>
        )
        }
      </div>

      <div className="flex flex-col gap-2">
        <h2 className="text-xl font-semibold">Wishlist</h2>
        <ul className="border rounded p-2 bg-white shadow divide-y">
          {wishlistItemsPending ? (<Loading02Icon size={48} />) :
            wishlistItems.map((s, i) => (
              <li key={i} className="flex items-center justify-between py-2 px-1">
                <span>{s.name}</span>
                <button
                  onClick={() => removeFromWishlist(s)}
                  className="text-red-600 hover:text-white hover:bg-red-600 border border-red-600 px-2 py-1 rounded text-sm transition"
                >
                  Delete
                </button>
              </li>
            ))
          }
        </ul>
      </div>
    </div>
  );
}