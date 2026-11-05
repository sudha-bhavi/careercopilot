import { STATUSES, type JobApplication, type Status } from '../types';

interface Props {
  items: JobApplication[];
  onEdit: (item: JobApplication) => void;
  onDelete: (item: JobApplication) => void;
  onStatusChange: (item: JobApplication, status: Status) => void;
}

// Local calendar date as YYYY-MM-DD, comparable as text with the API's date strings.
const today = () => new Date().toLocaleDateString('en-CA');

const isDue = (a: JobApplication) =>
  a.followUpDate !== null && a.followUpDate <= today() && (a.status === 'Applied' || a.status === 'Interview');

export function ApplicationList({ items, onEdit, onDelete, onStatusChange }: Props) {
  if (items.length === 0) {
    return <p className="muted card">No applications yet. Add your first one.</p>;
  }

  return (
    <ul className="list">
      {items.map((a) => (
        <li key={a.id} className={`card item ${isDue(a) ? 'due' : ''}`}>
          <div className="item-main">
            <strong>{a.company}</strong>
            <span>{a.role}</span>
            {a.link && (
              <a href={a.link} target="_blank" rel="noopener noreferrer">View posting</a>
            )}
            {a.notes && <p className="muted notes">{a.notes}</p>}
            <p className="muted small">
              {a.appliedDate && <>Applied {a.appliedDate}</>}
              {a.followUpDate && <> · Follow up {a.followUpDate}{isDue(a) ? ' (due)' : ''}</>}
            </p>
          </div>
          <div className="item-actions">
            <select
              value={a.status}
              aria-label={`Status for ${a.company}`}
              onChange={(e) => onStatusChange(a, e.target.value as Status)}
            >
              {STATUSES.map((s) => (
                <option key={s}>{s}</option>
              ))}
            </select>
            <button className="secondary" onClick={() => onEdit(a)}>Edit</button>
            <button className="danger" onClick={() => onDelete(a)}>Delete</button>
          </div>
        </li>
      ))}
    </ul>
  );
}
