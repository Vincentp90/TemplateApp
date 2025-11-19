import { useEffect, useRef } from 'react'

// Based on https://overreacted.io/making-setinterval-declarative-with-react-hooks/?ref=reactpractice.dev and https://usehooks-ts.com/react-hook/use-interval

export function useInterval(callback: () => void, delay: number | null) {
  const savedCallback = useRef(callback)

  // Remember the latest callback if it changes
  useEffect(() => {
    savedCallback.current = callback
  }, [callback])

  // Set up the interval
  useEffect(() => {
    if (delay === null) {
      return;
    }

    const id = setInterval(() => {
      savedCallback.current()
    }, delay)

    return () => {
      clearInterval(id)
    }
  }, [delay])
}