import { useState, type FormEvent } from 'react';
import { STATUSES, type ApplicationInput, type JobApplication, type Status } from '../types';

interface Props {
  initial?: JobApplication;
  onSubmit: (input: ApplicationInput) => Promise<void>;
  onCancel: () => void;
}

// Empty inputs become null so the API stores "no value" instead of an empty string.
const orNull = (value: string) => (value.trim() === '' ? null : value.trim());

export function ApplicationForm({ initial, onSubmit, onCancel }: Props) {
  const [company, setCompany] = useState(initial?.company ?? '');
  const [role, setRole] = useState(initial?.role ?? '');
  const [link, setLink] = useState(initial?.link ?? '');
  const [status, setStatus] = useState<Status>(initial?.status ?? 'Saved');
  const [appliedDate, setAppliedDate] = useState(initial?.appliedDate ?? '');
  const [followUpDate, setFollowUpDate] = useState(initial?.followUpDate ?? '');
  const [notes, setNotes] = useState(initial?.notes ?? '');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function submit(e: FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError(null);
    try {
      await onSubmit({
        company: company.trim(),
        role: role.trim(),
        link: orNull(link),
        status,
        appliedDate: orNull(appliedDate),
        followUpDate: orNull(followUpDate),
        notes: orNull(notes),
      });
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not save.');
      setBusy(false);
    }
  }

  return (
    <form onSubmit={submit} className="card stack">
      <h2>{initial ? 'Edit application' : 'New application'}</h2>
      <div className="grid">
        <label>
          Company
          <input value={company} onChange={(e) => setCompany(e.target.value)} required maxLength={200} />
        </label>
        <label>
          Role
          <input value={role} onChange={(e) => setRole(e.target.value)} required maxLength={200} />
        </label>
        <label>
          Job link
          <input type="url" value={link} onChange={(e) => setLink(e.target.value)} placeholder="https://" />
        </label>
        <label>
          Status
          <select value={status} onChange={(e) => setStatus(e.target.value as Status)}>
            {STATUSES.map((s) => (
              <option key={s}>{s}</option>
            ))}
          </select>
        </label>
        <label>
          Applied on
          <input type="date" value={appliedDate} onChange={(e) => setAppliedDate(e.target.value)} />
        </label>
        <label>
          Follow up on
          <input type="date" value={followUpDate} onChange={(e) => setFollowUpDate(e.target.value)} />
        </label>
      </div>
      <label>
        Notes
        <textarea value={notes} onChange={(e) => setNotes(e.target.value)} rows={3} maxLength={4000} />
      </label>
      {error && <p className="error" role="alert">{error}</p>}
      <div className="row">
        <button type="submit" disabled={busy}>{busy ? 'Saving…' : 'Save'}</button>
        <button type="button" className="secondary" onClick={onCancel}>Cancel</button>
      </div>
    </form>
  );
}
