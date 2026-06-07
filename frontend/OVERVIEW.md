# Frontend Project Overview

## Technology Stack
- **Framework**: React 19.1.1
- **Language**: TypeScript (~5.9.3)
- **Build Tool**: Vite 7.1.7
- **Routing**: TanStack Router 1.132.47 (file-based routing)
- **State Management**: Zustand 5.0.8 (auth state)
- **Data Fetching**: TanStack React Query 5.90.2
- **HTTP Client**: Axios 1.12.2
- **Styling**: Tailwind CSS 4.1.16 (via `@tailwindcss/vite`)
- **Form Handling**: React Hook Form 7.64.0 + Zod 4.1.12 (validation)
- **Real-time**: Microsoft SignalR 10.0.0 (auction live updates)
- **UI Icons**: Hugeicons React 0.3.0
- **Testing**: Vitest 4.0.6, React Testing Library 16.3.0, Playwright 1.57.0
- **Linting**: ESLint 9.36.0 + TypeScript ESLint
- **Compiler Plugin**: Babel React Compiler 19.1.0 (experimental compiler)

## Development guidelines
- Use useEffect sparingly, use alternatives if possible (useQuery for querying, pure function for calculated fields, etc)
- Run unit tests with this command: `npm run test`

## Project Structure
```
frontend/
├── src/
│   ├── main.tsx                    # App entry point (React 19, StrictMode, QueryClient, Router)
│   ├── api.ts                      # Axios instance with credentials
│   ├── AuthState.ts                # Zustand store for auth state (user + role)
│   ├── queryClient.ts              # React Query client (401 → redirect to login)
│   ├── router.ts                   # TanStack Router config
│   ├── index.css                   # Global styles
│   │
│   ├── routes/                     # File-based routes (TanStack Router)
│   │   ├── __root.tsx              # Root route (layout shell)
│   │   ├── index.tsx               # Redirect/route to auth or app
│   │   ├── auth/                   # Auth routes (login, logout)
│   │   │   ├── route.tsx
│   │   │   ├── login.tsx           # Login form (checks if already logged in)
│   │   │   └── logout.tsx
│   │   └── app/                    # Authenticated routes
│   │       ├── route.tsx           # App shell (sidebar, header, footer)
│   │       ├── index.tsx           # Home → Search component
│   │       ├── overview.tsx        # Wishlist items list
│   │       ├── auction.tsx         # Auction component
│   │       ├── liveauction.tsx     # Live auction (SignalR)
│   │       ├── stats.tsx           # Wishlist statistics
│   │       ├── lessonsLearned.tsx  # Lessons learned page
│   │       ├── about.tsx           # About page
│   │       ├── notauthorized.tsx   # 403 page
│   │       ├── admin/              # Admin-only routes
│   │       │   ├── route.tsx
│   │       │   ├── index.tsx       # Users list
│   │       │   └── profile/        # Admin profile management
│   │       └── profile/            # User profile (view + edit)
│   │
│   ├── components/
│   │   ├── tiny/                   # Small/shared UI primitives
│   │   │   ├── loading.tsx
│   │   │   ├── useInterval.tsx
│   │   │   └── wlButton.tsx
│   │   ├── search.tsx              # App search (searches Steam games)
│   │   ├── search.test.tsx         # Unit tests for search
│   │   ├── wlItemsList.tsx         # Wishlist items list
│   │   ├── auctionComp.tsx         # Auction component
│   │   ├── auctionLive.tsx         # Live auction (SignalR hub)
│   │   ├── statsCard.tsx           # Stats display
│   │   ├── lessonLearnedCard.tsx   # Lesson learned card
│   │   ├── loginForm.tsx           # Login form
│   │   ├── profile.tsx             # User profile view
│   │   ├── profileEdit.tsx         # User profile edit
│   │   └── admin/UsersList.tsx     # Admin user list
│   │
│   ├── data/
│   │   └── lessonsLearned.json     # Static lessons data
│   │
│   └── tests/
│       ├── README.md
│       └── playwright/wishlist.test.ts  # E2E tests
│
├── public/
│   └── vite.svg
├── package.json
├── tsconfig.json / tsconfig.app.json / tsconfig.node.json
├── vite.config.ts
├── eslint.config.js
├── playwright.config.ts
├── Dockerfile
├── Dockerfile.dev
├── nginx.conf
└── .env.development / .env.production
```

## Key Features
- **Authentication**: Cookie-based JWT (HttpOnly, Secure, SameSite=Strict). Login checks if user is already authenticated and redirects.
- **Real-time Auctions**: SignalR hub (`/auctionHub`) for live auction updates.
- **Admin Panel**: Admin-only routes for user management.
- **Wishlist Management**: Add/remove wishlist items, view wishlist stats.
- **Steam Integration**: Search Steam games via Steam API (backend handles API key).
- **Error Handling**: React Query cache intercepts 401 errors and redirects to login.
- **Suspense**: All page components wrapped in `Suspense` with loading fallbacks.

## Scripts
| Script | Description |
|--------|-------------|
| `npm run dev` | Start Vite dev server |
| `npm run build` | TypeScript check + Vite production build |
| `npm run lint` | ESLint |
| `npm run preview` | Preview production build |
| `npm run test` | Run Vitest unit tests |

## Docker
- `Dockerfile`: Production build with nginx serving static files
- `Dockerfile.dev`: Development with nginx + reverse proxy
