import { useState } from 'react';
import { STATUSES, type JobApplication, type Status } from '../types';
import { ApplicationCard, type CardActions } from './ApplicationCard';
import { EmptyState } from './ApplicationList';

interface Props extends CardActions {
  items: JobApplication[];
}

// One column per status. Dragging a card onto another column changes its status.
export function ApplicationBoard({ items, ...actions }: Props) {
  const [over, setOver] = useState<Status | null>(null);

  if (items.length === 0) return <EmptyState />;

  return (
    <div className="board">
      {STATUSES.map((status) => {
        const column = items.filter((i) => i.status === status);
        return (
          <section
            key={status}
            className={`column s-${status} ${over === status ? 'over' : ''}`}
            onDragOver={(e) => {
              e.preventDefault();
              setOver(status);
            }}
            onDragLeave={() => setOver(null)}
            onDrop={(e) => {
              e.preventDefault();
              setOver(null);
              const id = e.dataTransfer.getData('text/plain');
              const dropped = items.find((i) => i.id === id);
              if (dropped && dropped.status !== status) actions.onStatusChange(dropped, status);
            }}
          >
            <header className="col-head">
              <span>{status}</span>
              <span className="count">{column.length}</span>
            </header>
            {column.map((a) => (
              <ApplicationCard key={a.id} item={a} draggable {...actions} />
            ))}
            {column.length === 0 && <p className="muted small empty">Drop a card here</p>}
          </section>
        );
      })}
    </div>
  );
}
