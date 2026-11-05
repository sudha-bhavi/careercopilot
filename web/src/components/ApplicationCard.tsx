import type { DragEvent } from 'react';
import { STATUSES, type JobApplication, type Status } from '../types';

export interface CardActions {
  onEdit: (item: JobApplication) => void;
  onDelete: (item: JobApplication) => void;
  onStatusChange: (item: JobApplication, status: Status) => void;
}

// Local calendar date as YYYY-MM-DD, comparable as text with the API's date strings.
const today = () => new Date().toLocaleDateString('en-CA');

export const isDue = (a: JobApplication) =>
  a.followUpDate !== null && a.followUpDate <= today() && (a.status === 'Applied' || a.status === 'Interview');

// A stable colour per company, so each card gets its own avatar without storing anything.
function hue(text: string) {
  let h = 0;
  for (const c of text) h = (h * 31 + c.charCodeAt(0)) % 360;
  return h;
}

interface Props extends CardActions {
  item: JobApplication;
  draggable?: boolean;
}

export function ApplicationCard({ item, draggable = false, onEdit, onDelete, onStatusChange }: Props) {
  const due = isDue(item);

  function dragStart(e: DragEvent) {
    e.dataTransfer.setData('text/plain', item.id);
    e.dataTransfer.effectAllowed = 'move';
  }

  return (
    <article className={`app-card ${due ? 'due' : ''}`} draggable={draggable} onDragStart={dragStart}>
      <div className="app-head">
        <span className="avatar" style={{ background: `hsl(${hue(item.company)} 65% 48%)` }} aria-hidden="true">
          {item.company.charAt(0).toUpperCase()}
        </span>
        <div className="app-title">
          <strong>{item.company}</strong>
          <span className="muted">{item.role}</span>
        </div>
        <span className={`badge s-${item.status}`}>{item.status}</span>
      </div>

      {item.link && (
        <a href={item.link} target="_blank" rel="noopener noreferrer">View posting ↗</a>
      )}
      {item.notes && <p className="notes">{item.notes}</p>}

      {(item.appliedDate || item.followUpDate) && (
        <p className="muted small">
          {item.appliedDate && <>Applied {item.appliedDate}</>}
          {item.followUpDate && (
            <>
              {item.appliedDate && ' · '}
              Follow up {item.followUpDate}
              {due && <span className="due-tag"> · due</span>}
            </>
          )}
        </p>
      )}

      <div className="card-actions">
        <select
          value={item.status}
          aria-label={`Status for ${item.company}`}
          onChange={(e) => onStatusChange(item, e.target.value as Status)}
        >
          {STATUSES.map((s) => (
            <option key={s}>{s}</option>
          ))}
        </select>
        <button className="secondary small-btn" onClick={() => onEdit(item)}>Edit</button>
        <button className="danger small-btn" onClick={() => onDelete(item)}>Delete</button>
      </div>
    </article>
  );
}
