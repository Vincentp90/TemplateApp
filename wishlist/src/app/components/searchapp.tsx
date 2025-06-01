'use client';

import { useState, useEffect, useRef  } from 'react';
import debounce from 'lodash.debounce';

export default function SearchApp() {
  const [query, setQuery] = useState('');
  const [results, setResults] = useState<string[]>([]);
  const [selected, setSelected] = useState<string | null>(null);

  // TODO learn what this does, why useref?
  // does this work correctly?
  const fetchResults = useRef (
    debounce(async (q: string) => {
      const res = await fetch(`http://localhost:5186/applisting/search/${encodeURIComponent(q)}`);
      const data = await res.json();
      setResults(data.slice(0, 10));
    }, 300)
  );

  useEffect(() => {
    if (query.length > 3) fetchResults.current(query);
    else setResults([]);
  }, [query]);

  return (
    <div>
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
            <li key={idx} onClick={() => setSelected(item)}>
              {item}
            </li>
          ))}
        </ul>
      )}
      {selected && <div>Selected: {selected}</div>}
    </div>
  );
}