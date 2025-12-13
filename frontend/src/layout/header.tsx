import { Link } from "@tanstack/react-router";
import { useAuthStore } from '../AuthState.ts'

export default function Header() {
  const username = useAuthStore((state) => state.user);

  return (
    <header className="sticky top-0 bg-gray-900 text-white px-6 py-4 shadow-md flex justify-between items-center z-50">      
      <nav className="space-x-4">
        <Link to="/app" className="hover:text-gray-300">Home</Link>
        <Link to="/app/overview" className="hover:text-gray-300">Overview</Link>
        <Link to="/app/lessonsLearned" className="hover:text-gray-300">Lessons learned</Link>
        <Link to="/app/about" className="hover:text-gray-300">About</Link>
        <Link to="/auth/logout" className="hover:text-gray-300">Logout</Link>
      </nav>
      <Link to="/app/profile" className="hover:text-gray-300 text-xl font-semibold">{username}</Link>
    </header>
  );
}