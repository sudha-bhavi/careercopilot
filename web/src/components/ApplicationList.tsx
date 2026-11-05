import type { JobApplication } from '../types';
import { ApplicationCard, type CardActions } from './ApplicationCard';

interface Props extends CardActions {
  items: JobApplication[];
}

export function ApplicationList({ items, ...actions }: Props) {
  if (items.length === 0) return <EmptyState />;

  return (
    <div className="cards">
      {items.map((a) => (
        <ApplicationCard key={a.id} item={a} {...actions} />
      ))}
    </div>
  );
}

export function EmptyState() {
  return (
    <div className="card empty-state">
      <h2>No applications yet</h2>
      <p className="muted">Add your first one with the button above and start tracking your search.</p>
    </div>
  );
}
