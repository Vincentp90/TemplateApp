
export default function Header() {
  return (
    <header className="bg-gray-900 text-white px-6 py-4 shadow-md flex justify-between items-center">
      <div className="text-xl font-semibold">Top Bar</div>
      <nav className="space-x-4">
        <a href="/main" className="hover:text-gray-300">Home</a>
        <a href="/main/about" className="hover:text-gray-300">About</a>
        <a href="/main/nextexample" className="hover:text-gray-300">Contact</a>
      </nav>
    </header>
  );
}