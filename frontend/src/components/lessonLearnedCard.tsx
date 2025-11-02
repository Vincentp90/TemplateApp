export type Card = {
    id: number;
    title: string;
    info: string;
    // TODO: add card theme and use different color for each of them. React, .NET, infra, etc
}

interface LessonLearnedCardProps {
  card: Card;
}

function EscapeHTMLAndRestoreCodeTags(str: string) {
  const escapedStr = str
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/"/g, "&quot;")
    .replace(/'/g, "&#39;");

  return escapedStr.replaceAll('[[code]]', '<pre><code>').replaceAll('[[endcode]]', '</code></pre>');
}

export default function LessonLearnedCard({ card }: LessonLearnedCardProps) {
    const cardInfo = EscapeHTMLAndRestoreCodeTags(card.info);

    return (
        <div className="p-4 bg-white shadow rounded-2xl">
            <h2 className="text-lg font-semibold">{card.title}</h2>
            <div className="text-sm text-gray-600" dangerouslySetInnerHTML={{ __html: cardInfo }} />
        </div>
    )
}