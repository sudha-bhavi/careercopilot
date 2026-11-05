import { STATUSES, type Dashboard } from '../types';

export function DashboardCards({ data }: { data: Dashboard | null }) {
  if (!data) return null;
  return (
    <section className="tiles" aria-label="Summary">
      <div className="tile total">
        <span className="tile-number">{data.total}</span>
        <span className="muted">Total</span>
      </div>
      {STATUSES.map((s) => (
        <div className={`tile s-${s}`} key={s}>
          <span className="tile-number">{data.byStatus[s] ?? 0}</span>
          <span className="muted">{s}</span>
        </div>
      ))}
      <div className={`tile ${data.followUpsDue > 0 ? 'alert' : ''}`}>
        <span className="tile-number">{data.followUpsDue}</span>
        <span className="muted">Follow-ups due</span>
      </div>
    </section>
  );
}
