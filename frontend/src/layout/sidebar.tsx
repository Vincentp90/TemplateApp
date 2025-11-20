// TODO fix scroll issue (sidebar scrolls with content)
export default function Sidebar() {
  return (
    <aside className="w-64 bg-gray-800 text-white p-4">
      <nav className="space-y-4">
        <a href="/app/auction" className="block hover:text-gray-300">Auction</a>
        <a href="/app/liveauction" className="block hover:text-gray-300">Auction with live updates</a>
        <a href="#" className="block hover:text-gray-300">Dolor</a>
        <a href="#" className="block hover:text-gray-300">Sit Amet</a>
      </nav>
    </aside>
  );
}