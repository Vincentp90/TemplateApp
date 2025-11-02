import { createFileRoute } from '@tanstack/react-router'
import data from "../../data/lessonsLearned.json"
import type { Card } from '../../components/lessonLearnedCard';
import LessonLearnedCard from '../../components/lessonLearnedCard';

export const Route = createFileRoute('/app/lessonsLearned')({
  component: InfoCards,
})


function shuffle(array: Card[]) {
  return [...array].sort(() => Math.random() - 0.5);
}

function InfoCards() {
  const cards = shuffle(data);

  return (
    <div className="flex flex-col items-center p-4">
      <div className="w-[80%] max-w-[800px] flex flex-col gap-4">
        {cards.map((card) => (
          <LessonLearnedCard key={card.id} card={card} />
        ))}
      </div>
    </div>
  )
}
