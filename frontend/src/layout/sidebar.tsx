import { Link } from "@tanstack/react-router";

// TODO fix scroll issue (sidebar scrolls with content)
export default function Sidebar() {
  return (
    <aside className="w-64 bg-gray-800 text-white p-4">
      <nav className="space-y-4">
        <Link to="/app/auction" className="block hover:text-gray-300">Auction</Link>
        <Link to="/app/liveauction" className="block hover:text-gray-300">Auction with live updates</Link>
      </nav>
    </aside>
  );
}