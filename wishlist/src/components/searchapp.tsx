'use client';

import { useState, useEffect, useRef } from 'react';
import debounce from 'lodash.debounce';

//TODO move to serparate file?
type AppListing = { appid: number; name: string };

export default function SearchApp() {
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<AppListing[]>([]);
  const [selected, setSelected] = useState<AppListing[]>([]);

  // TODO learn what this does, why useref?
  // does this work correctly?
  const fetchResults = useRef(
    debounce(async (q: string) => {
      const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/applisting/search/${encodeURIComponent(q)}`);
      const data = await res.json();
      setResults(data.slice(0, 10));
    }, 300)
  );

  useEffect(() => {
    if (query.length > 3) fetchResults.current(query);
    else setResults([]);
  }, [query]);

  useEffect(() => {
    fetch(`${process.env.NEXT_PUBLIC_API_URL}/wishlist`, {
      headers: {
        'x-user-id': '1'
      },
    })
      .then(res => res.json())
      .then(data => setSelected(data.map(
        (item: AppListing) => ({ appid: item.appid, name: item.name }))))
      .catch(err => console.error('Fetch error:', err));
  }, []);

  const onClickApplisting = (appItem: AppListing) => {
    setSelected([...selected, appItem]);
    setQuery('');
    fetch(`${process.env.NEXT_PUBLIC_API_URL}/wishlist/${appItem.appid}`, {
      method: 'POST',
      headers: {
        'x-user-id': '1'
      },
    }).catch(err => console.error('Failed to add to wishlist:', err));
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
        {results.length > 0 && (
          <ul className="border rounded p-2 bg-white shadow">
            {results.map((item: AppListing, idx) => (
              <li
                key={idx}
                value={item.appid}
                onClick={() => onClickApplisting(item)}
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
        <ul className="border rounded p-2 bg-white shadow">
          {selected.map((s, i) => (
            <li key={i} className="p-1 border-b last:border-b-0">
              {s.name}
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}