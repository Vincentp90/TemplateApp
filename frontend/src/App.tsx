import './App.css'
import Search from './components/search'

//TODO improve: this seems kinda pointless to have index.tsx > App.tsx > Search.tsx with both index and app being pretty much empty
function App() {
  return (
    <>
      <Search />  
    </>
  )
}

export default App
