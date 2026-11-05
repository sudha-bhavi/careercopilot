import { useCallback, useEffect, useState } from 'react';
import {
  clearToken,
  createApplication,
  deleteApplication,
  getDashboard,
  getToken,
  listApplications,
  setUnauthorizedHandler,
  updateApplication,
} from './api';
import { ApplicationForm } from './components/ApplicationForm';
import { ApplicationList } from './components/ApplicationList';
import { DashboardCards } from './components/DashboardCards';
import { LoginForm } from './components/LoginForm';
import { ResumeMatch } from './components/ResumeMatch';
import { STATUSES, type ApplicationInput, type Dashboard, type JobApplication, type Status } from './types';

export default function App() {
  const [signedIn, setSignedIn] = useState(() => getToken() !== null);

  // api.ts calls this when the server answers 401 (expired token), which shows the login screen.
  useEffect(() => setUnauthorizedHandler(() => setSignedIn(false)), []);

  if (!signedIn) return <LoginForm onLoggedIn={() => setSignedIn(true)} />;

  return (
    <Home
      onSignOut={() => {
        clearToken();
        setSignedIn(false);
      }}
    />
  );
}

function Home({ onSignOut }: { onSignOut: () => void }) {
  const [tab, setTab] = useState<'applications' | 'resume'>('applications');
  const [items, setItems] = useState<JobApplication[]>([]);
  const [dashboard, setDashboard] = useState<Dashboard | null>(null);
  const [filter, setFilter] = useState<Status | ''>('');
  const [editing, setEditing] = useState<JobApplication | 'new' | null>(null);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    try {
      const [list, summary] = await Promise.all([listApplications(filter || undefined), getDashboard()]);
      setItems(list);
      setDashboard(summary);
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not load data.');
    }
  }, [filter]);

  useEffect(() => {
    void load();
  }, [load]);

  async function save(input: ApplicationInput) {
    if (editing && editing !== 'new') await updateApplication(editing.id, input);
    else await createApplication(input);
    setEditing(null);
    await load();
  }

  async function changeStatus(item: JobApplication, status: Status) {
    try {
      await updateApplication(item.id, { ...item, status });
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not update status.');
    }
  }

  async function remove(item: JobApplication) {
    if (!window.confirm(`Delete ${item.company} – ${item.role}?`)) return;
    try {
      await deleteApplication(item.id);
      await load();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not delete.');
    }
  }

  return (
    <div className="shell">
      <header className="topbar">
        <h1>JobPilot</h1>
        <nav className="row">
          <button className={tab === 'applications' ? '' : 'secondary'} onClick={() => setTab('applications')}>
            Applications
          </button>
          <button className={tab === 'resume' ? '' : 'secondary'} onClick={() => setTab('resume')}>
            Resume match
          </button>
          <button className="secondary" onClick={onSignOut}>Sign out</button>
        </nav>
      </header>

      {error && <p className="error" role="alert">{error}</p>}

      {tab === 'resume' ? (
        <ResumeMatch />
      ) : (
        <main className="stack">
          <DashboardCards data={dashboard} />

          {editing ? (
            <ApplicationForm
              initial={editing === 'new' ? undefined : editing}
              onSubmit={save}
              onCancel={() => setEditing(null)}
            />
          ) : (
            <div className="row between">
              <label className="inline">
                Show
                <select value={filter} onChange={(e) => setFilter(e.target.value as Status | '')}>
                  <option value="">All</option>
                  {STATUSES.map((s) => (
                    <option key={s}>{s}</option>
                  ))}
                </select>
              </label>
              <button onClick={() => setEditing('new')}>Add application</button>
            </div>
          )}

          <ApplicationList
            items={items}
            onEdit={setEditing}
            onDelete={remove}
            onStatusChange={changeStatus}
          />
        </main>
      )}
    </div>
  );
}
