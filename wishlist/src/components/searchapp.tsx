'use client';

import { useState, useEffect, useRef  } from 'react';
import debounce from 'lodash.debounce';

export default function SearchApp() {
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<string[]>([]);
  const [selected, setSelected] = useState<string[]>([]);

  // TODO learn what this does, why useref?
  // does this work correctly?
  const fetchResults = useRef (
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
    fetch('/api/selected') // TODO call wishlistitem api
      .then(res => res.json())
      .then(data => setSelected(data));
  }, []);

  const onClickApplisting = (appid: string) => {
    setSelected([...selected, appid]);
    setQuery('');
    //TODO add to wishlist
  }

  return (
    <div className="flex flex-col gap-4">
      <input
        type="text"
        value={query}
        onChange={(e) => {
          setQuery(e.target.value);
        }}
        placeholder="Type to search..."
      />
      {results.length > 0 && (
        <ul>
          {results.map((item: string, idx) => (
            <li key={idx} onClick={() => onClickApplisting(item)}>
              {item}
            </li>
          ))}
        </ul>
      )}
      <h2>Wishlist</h2>
      <ul>
        {selected.map((s, i) => (
          <li key={i}>{s}</li>
        ))}
      </ul>
    </div>
  );
}