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
import { ApplicationBoard } from './components/ApplicationBoard';
import { ApplicationForm } from './components/ApplicationForm';
import { ApplicationList } from './components/ApplicationList';
import { Brand } from './components/Brand';
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
  const [view, setView] = useState<'board' | 'list'>('board');
  const [items, setItems] = useState<JobApplication[]>([]);
  const [dashboard, setDashboard] = useState<Dashboard | null>(null);
  const [filter, setFilter] = useState<Status | ''>('');
  const [editing, setEditing] = useState<JobApplication | 'new' | null>(null);
  const [error, setError] = useState<string | null>(null);

  // The board already shows every status as a column, so the status filter only applies to the list.
  const activeFilter = view === 'list' ? filter : '';

  const load = useCallback(async () => {
    try {
      const [list, summary] = await Promise.all([listApplications(activeFilter || undefined), getDashboard()]);
      setItems(list);
      setDashboard(summary);
      setError(null);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not load data.');
    }
  }, [activeFilter]);

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

  const actions = { onEdit: setEditing, onDelete: remove, onStatusChange: changeStatus };

  return (
    <div className="shell">
      <header className="topbar">
        <Brand />
        <div className="row">
          <nav className="tabs" aria-label="Sections">
            <button className={`tab ${tab === 'applications' ? 'active' : ''}`} onClick={() => setTab('applications')}>
              Applications
            </button>
            <button className={`tab ${tab === 'resume' ? 'active' : ''}`} onClick={() => setTab('resume')}>
              Resume match
            </button>
          </nav>
          <button className="secondary" onClick={onSignOut}>Sign out</button>
        </div>
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
              <div className="row">
                <nav className="tabs" aria-label="View">
                  <button className={`tab ${view === 'board' ? 'active' : ''}`} onClick={() => setView('board')}>
                    Board
                  </button>
                  <button className={`tab ${view === 'list' ? 'active' : ''}`} onClick={() => setView('list')}>
                    List
                  </button>
                </nav>
                {view === 'list' && (
                  <label className="inline">
                    Show
                    <select value={filter} onChange={(e) => setFilter(e.target.value as Status | '')}>
                      <option value="">All</option>
                      {STATUSES.map((s) => (
                        <option key={s}>{s}</option>
                      ))}
                    </select>
                  </label>
                )}
              </div>
              <button onClick={() => setEditing('new')}>+ Add application</button>
            </div>
          )}

          {view === 'board' ? (
            <ApplicationBoard items={items} {...actions} />
          ) : (
            <ApplicationList items={items} {...actions} />
          )}
        </main>
      )}
    </div>
  );
}
