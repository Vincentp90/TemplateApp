import { Link } from "@tanstack/react-router";

export default function Header() {
  return (
    <header className="bg-gray-900 text-white px-6 py-4 shadow-md flex justify-between items-center">
      <div className="text-xl font-semibold">Top Bar</div>
      <nav className="space-x-4">
        <Link to="/app" className="hover:text-gray-300">Home</Link>
        <Link to="/app/overview" className="hover:text-gray-300">Overview</Link>
        <Link to="/app/about" className="hover:text-gray-300">About</Link>
        <Link to="/auth/logout" className="hover:text-gray-300">Logout</Link>
      </nav>
    </header>
  );
}