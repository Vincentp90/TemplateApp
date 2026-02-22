import { Link } from "@tanstack/react-router";
import { useAuthStore, UserRole } from "../AuthState";

// TODO fix scroll issue (sidebar scrolls with content)
export default function Sidebar() {
  const userIsAdmin = useAuthStore((state) => state.user?.role) === UserRole.Admin;

  return (
    <aside className="w-64 bg-gray-800 text-white p-4">
      <nav className="space-y-4">
        <Link to="/app/auction" className="block hover:text-gray-300">Auction</Link>
        <Link to="/app/liveauction" className="block hover:text-gray-300">Auction with live updates</Link>
        {userIsAdmin && (
          <Link to="/app/admin" className="block hover:text-gray-300">Admin temp link</Link>  
        )}
      </nav>
    </aside>
  );
}