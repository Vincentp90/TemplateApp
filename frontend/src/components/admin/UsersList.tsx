'use client';

import { useSuspenseQuery } from '@tanstack/react-query';
import { api } from '../../api';
import { Link } from '@tanstack/react-router';

type User = { uuid: string; username: string; };

export default function UsersList() {
  const { data: userItems = [] } = useSuspenseQuery<User[]>({
    queryKey: ['users'],
    queryFn: async () => {
      const res = await api.get("/users");
      return res.data;
    },
  });

  return (
    <>
      <div className="inline-grid grid-cols-[auto_auto_min-content] rounded-lg bg-white shadow border border-gray-200 overflow-hidden">
        {/* Header */}
        <div className="contents">
          <span className="bg-gray-100 text-gray-700 font-semibold px-4 py-3 border-b whitespace-nowrap">
            Name
          </span>
          <span className="bg-gray-100 text-gray-700 font-semibold px-4 py-3 border-b whitespace-nowrap">
            Date Added
          </span>
          <span className="bg-gray-100 text-gray-700 font-semibold px-4 py-3 border-b text-right whitespace-nowrap">
            Edit
          </span>
        </div>

        {/* Rows */}
        {userItems.map((s, i) => (
          <div key={i} className="contents">
            <span className={`px-4 py-3 whitespace-nowrap ${i % 2 ? "bg-gray-50" : "bg-white"}`}>
              {s.uuid}
            </span>
            <span className={`px-4 py-3 whitespace-nowrap ${i % 2 ? "bg-gray-50" : "bg-white"}`}>
              {s.username}
            </span>
            <Link
              to="/app/admin/profile" search={{ userId: s.uuid }}
              className={`px-4 py-3 text-right text-blue-600 hover:underline whitespace-nowrap ${
                i % 2 ? "bg-gray-50" : "bg-white"
              }`}
            >
              Edit
            </Link>
          </div>
        ))}
      </div>
    </>
  );
}