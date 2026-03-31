import { Link } from "@tanstack/react-router";
import { useAuthStore } from '../AuthState.ts'
import { useEffect } from "react";

import { Sun01Icon, Moon01Icon } from "hugeicons-react";

const toggleDark = () => {
  const isDark = document.documentElement.classList.toggle('dark')
  localStorage.setItem('theme', isDark ? 'dark' : 'light')
}

export default function Header() {
  const username = useAuthStore((state) => state.user!.username);

  useEffect(() => {
    const saved = localStorage.getItem('theme')
    if (saved === 'dark') {
      document.documentElement.classList.add('dark')
    }
  }, [])

  return (
    <header className="sticky top-0 bg-gray-300 text-black dark:bg-gray-900 dark:text-white px-6 h-14 shadow-md flex justify-between items-center z-50">
      <nav className="space-x-4">
        <Link to="/app" className="hover:text-gray-300">Home</Link>
        <Link to="/app/overview" className="hover:text-gray-300">Overview</Link>
        <Link to="/app/lessonsLearned" className="hover:text-gray-300">Lessons learned</Link>
        <Link to="/app/about" className="hover:text-gray-300">About</Link>
        <Link to="/auth/logout" className="hover:text-gray-300">Logout</Link>
      </nav>
      <div className="flex items-center gap-4">
        <ThemeToggle toggleDark={toggleDark} />
        <Link to="/app/profile" className="hover:text-gray-300 text-xl font-semibold">{username}</Link>
      </div>
    </header>
  );
}

interface ThemeToggleProps {
  toggleDark: () => void;
}

function ThemeToggle({ toggleDark }: ThemeToggleProps) {
  return (
    <button
      onClick={toggleDark}
      className="flex items-center justify-center rounded-full bg-gray-200 dark:bg-gray-800"
      aria-label="Toggle theme"
    >
      <Sun01Icon className="h-5 w-5 text-orange-500 block dark:hidden" />
      <Moon01Icon className="h-5 w-5 text-blue-400 hidden dark:block" />
    </button>
  );
}