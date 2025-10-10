import './App.css'
import Search from './search/search'
import Header from './layout/header.tsx'
import Sidebar from './layout/sidebar.tsx'
import Footer from './layout/footer.tsx'

function App() {
  return (
    <>
      <div className="flex flex-col min-h-screen">
        <Header />
          <div className="flex flex-1">
            <Sidebar />
            <main className="flex-1 p-4">
              <Search />
            </main>
          </div>
        <Footer />
      </div>      
    </>
  )
}

export default App
