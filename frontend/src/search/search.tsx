'use client';

import { useState, useEffect, useRef } from 'react';
import debounce from 'lodash.debounce';

//TODO move to serparate file?
type AppListing = { appid: number; name: string };

const APIURL = "http://localhost:5186";//TODO put in better spot

export default function Search() {
  const [query, setQuery] = useState('');
  const [searchResults, setSearchResults] = useState<AppListing[]>([]);
  const [wishlistItems, setWishlistItems] = useState<AppListing[]>([]);

  // TODO learn what this does, why useref?
  // does this work correctly?
  const fetchResults = useRef(
    debounce(async (q: string) => {
      const res = await fetch(`${APIURL}/applisting/search/${encodeURIComponent(q)}`);
      const data = await res.json();
      setSearchResults(data.slice(0, 10));
    }, 300)
  );

  useEffect(() => {
    if (query.length > 2) fetchResults.current(query);
    else setSearchResults([]);
  }, [query]);

  useEffect(() => {
    fetch(`${APIURL}/wishlist`, {
      headers: {
        'x-user-id': '1'
      },
    })
      .then(res => res.json())
      .then(data => setWishlistItems(data.map(
        (item: AppListing) => ({ appid: item.appid, name: item.name }))))
      .catch(err => console.error('Fetch error:', err));
  }, []);

  const addToWishlist = (appItem: AppListing) => {
    setWishlistItems([...wishlistItems, appItem]);
    setQuery('');
    fetch(`${APIURL}/wishlist/${appItem.appid}`, {
      method: 'POST',
      headers: {
        'x-user-id': '1'
      },
    }).catch(err => console.error('Failed to add to wishlist:', err));
  }

  const removeFromWishlist = (appItem: AppListing) => {
    setWishlistItems(list => list.filter(i => i.appid !== appItem.appid));
    
    fetch(`${APIURL}/wishlist/${appItem.appid}`, {
      method: 'DELETE',
      headers: {
        'x-user-id': '1'
      },
    }).catch(err => console.error('Failed to remove from wishlist:', err));
  }

  return (
    <div className="grid grid-cols-2 gap-6">
      {/* Left Column: Search */}
      <div className="flex flex-col gap-4">
        <input
          type="text"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
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
        )}
      </div>

      {/* Right Column: Wishlist */}
      <div className="flex flex-col gap-2">
        <h2 className="text-xl font-semibold">Wishlist</h2>
        <ul className="border rounded p-2 bg-white shadow divide-y">
          {wishlistItems.map((s, i) => (
            <li key={i} className="flex items-center justify-between py-2 px-1">
              <span>{s.name}</span>
              <button
                onClick={() => removeFromWishlist(s)}
                className="text-red-600 hover:text-white hover:bg-red-600 border border-red-600 px-2 py-1 rounded text-sm transition"
              >
                Delete
              </button>
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}