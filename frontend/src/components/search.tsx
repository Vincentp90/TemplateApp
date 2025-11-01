'use client';

import { useState, useRef } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import debounce from 'lodash.debounce';
import { api } from "../api";

import { Loading02Icon } from "hugeicons-react";
import WlButton from './tiny/wlButton';

type AppListing = { appid: number; name: string };
const wlQueryKey = ['wishlist'];

const fetchSearchResults = async (query: string): Promise<AppListing[]> => {
  const res = await api.get(`/applisting/search/${encodeURIComponent(query)}`);
  const data = res.data;  
  return data.slice(0, 10);
};

export default function Search() {
  const [searchQuery, setSearchQuery] = useState('');
  const queryClient = useQueryClient();

  const debounceQueryInput = useRef(
    debounce(async (q: string) => {
      setSearchQuery(q);
    }, 300)
  );

  const handleSearchInputChange = (input: string) =>{
    if (input.length > 2) debounceQueryInput.current(input);
    else setSearchQuery('');
  };

  const { data: searchResults = [] } = useQuery<AppListing[]>({
    queryKey: ['search', searchQuery],
    queryFn: async () => fetchSearchResults(searchQuery),
    enabled: searchQuery.length > 2,
  });

  const { data: wishlistItems = [], isPending: wishlistItemsPending } = useQuery<AppListing[]>({
    queryKey: wlQueryKey,
    queryFn: async () => {
      const res = await api.get(`/wishlist?fields=appid,name`);
      const data = res.data;
      return data.map((item: AppListing) => ({
        appid: item.appid,
        name: item.name,
      }));
    },
  });

  // TODO further learn what this does and how it works
  const addMutation = useMutation({
    mutationFn: async (appItem: AppListing) => {
      await api.post(`/wishlist/${appItem.appid}`);
      return appItem;
    },
    onMutate: async (appItem) => {
      await queryClient.cancelQueries({ queryKey: wlQueryKey });
      const previous = queryClient.getQueryData<AppListing[]>(wlQueryKey) || [];
      queryClient.setQueryData(wlQueryKey, [...previous, appItem]);
      return { previous };
    },
    onError: (_err, _appItem, context) => {
      if (context?.previous)
        queryClient.setQueryData(wlQueryKey, context.previous);
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: wlQueryKey });
    },
  });

  const removeMutation = useMutation({
    mutationFn: async (appItem: AppListing) => {
      await api.delete(`/wishlist/${appItem.appid}`);
      return appItem;
    },
    onMutate: async (appItem) => {
      await queryClient.cancelQueries({ queryKey: wlQueryKey });
      const previous = queryClient.getQueryData<AppListing[]>(wlQueryKey) || [];
      queryClient.setQueryData(
        wlQueryKey,
        previous.filter(i => i.appid !== appItem.appid)
      );
      return { previous };
    },
    onError: (_err, _appItem, context) => {
      if (context?.previous)
        queryClient.setQueryData(wlQueryKey, context.previous);
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: wlQueryKey });
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
        <h2>Search steam games to add to wishlist</h2>
        <input
          type="text"
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
                <WlButton onClick={() => removeFromWishlist(s)} isPrimary={true}>
                  Delete
                </WlButton>
              </li>
            ))
          }
        </ul>
      </div>
    </div>
  );
}