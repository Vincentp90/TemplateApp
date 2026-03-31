'use client';

import { useSuspenseQuery } from '@tanstack/react-query';
import { api } from '../../api';
import { Link } from '@tanstack/react-router';
import { useState } from 'react';

type User = { uuid: string; username: string; };
type UsersData = { items: User[]; hasNextPage: boolean; };

const PAGE_SIZE = 100;

export default function UsersList() {
  const [page, setPage] = useState(1);
  
  const { data } = useSuspenseQuery<UsersData>({
    queryKey: ['users', page],
    queryFn: async () => {
      const res = await api.get('/users', {
        params: { page, limit: PAGE_SIZE },
      });
      return res.data;
    }
  });

  return (
    <>
      <div className="inline-grid grid-cols-[auto_auto_min-content] rounded-lg shadow border border-gray-200 dark:border-gray-400 overflow-hidden">
        {/* Header */}
        <div className="contents">
          <span className="bg-gray-100 dark:bg-gray-900 text-gray-700 dark:text-gray-300 font-semibold px-4 py-3 border-b whitespace-nowrap">
            Public ID
          </span>
          <span className="bg-gray-100 dark:bg-gray-900 text-gray-700 dark:text-gray-300 font-semibold px-4 py-3 border-b whitespace-nowrap">
            UserName
          </span>
          <span className="bg-gray-100 dark:bg-gray-900 text-gray-700 dark:text-gray-300 font-semibold px-4 py-3 border-b text-right whitespace-nowrap">
            Edit
          </span>
        </div>

        {/* Rows */}
        {data.items.map((s, i) => (
          <div key={s.uuid} className="contents">
            <span className={`px-4 py-3 whitespace-nowrap ${i % 2 ? "bg-gray-50 dark:bg-gray-800" : "bg-white dark:bg-gray-700"}`}>
              {s.uuid}
            </span>
            <span className={`px-4 py-3 whitespace-nowrap ${i % 2 ? "bg-gray-50 dark:bg-gray-800" : "bg-white dark:bg-gray-700"}`}>
              {s.username}
            </span>
            <Link
              to="/app/admin/profile" search={{ userId: s.uuid }}
              className={`px-4 py-3 text-right text-blue-600 hover:underline whitespace-nowrap ${
                i % 2 ? "bg-gray-50 dark:bg-gray-800" : "bg-white dark:bg-gray-700"
              }`}
            >
              Edit
            </Link>
          </div>
        ))}        
      </div>

      {/* Pagination */}
        <div className="flex items-center gap-2 mt-4">
          <button
            className="px-3 py-1 border rounded disabled:opacity-50 disabled:cursor-not-allowed"
            disabled={page === 1}
            onClick={() => setPage((p) => p - 1)}
          >
            Prev
          </button>

          <span className="px-3 py-1">Page {page}</span>

          <button
            className="px-3 py-1 border rounded disabled:opacity-50 disabled:cursor-not-allowed"
            disabled={!data.hasNextPage}
            onClick={() => setPage((p) => p + 1)}
          >
            Next
          </button>
        </div>
    </>
  );
}