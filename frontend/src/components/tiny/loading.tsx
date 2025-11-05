import { Loading02Icon } from "hugeicons-react";

interface LoadingProps {
  message?: string;
  size?: number;
}

export function Loading({ message = "Loading...", size = 64 }: LoadingProps) {
  return (
    <div className="flex flex-col items-center justify-center gap-2 p-8">
      <Loading02Icon size={size} className="animate-spin" />
      <span>{message}</span>
    </div>
  );
}