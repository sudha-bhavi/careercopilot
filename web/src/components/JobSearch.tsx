import { useState, type FormEvent } from 'react';
import { createApplication, searchJobs } from '../api';
import type { JobMatch } from '../types';

// The resume is remembered in this browser only, so you don't paste it for every search.
const RESUME_KEY = 'careercopilot.jobsearch.resume';

function loadResume(): string {
  try {
    return localStorage.getItem(RESUME_KEY) ?? '';
  } catch {
    return '';
  }
}

function rememberResume(value: string) {
  try {
    localStorage.setItem(RESUME_KEY, value);
  } catch {
    // storage unavailable (private window); the search still works
  }
}

const tier = (score: number) => (score >= 70 ? 'strong' : score >= 40 ? 'partial' : 'weak');
const tierLabel = { strong: 'Strong fit', partial: 'Partial fit', weak: 'Low fit' } as const;

const shorten = (text: string, max: number) => (text.length > max ? `${text.slice(0, max).trimEnd()}…` : text);

export function JobSearch({ onSaved }: { onSaved: () => void }) {
  const [city, setCity] = useState('');
  const [query, setQuery] = useState('');
  const [resume, setResume] = useState(loadResume);
  const [results, setResults] = useState<JobMatch[] | null>(null);
  const [searchedCity, setSearchedCity] = useState('');
  const [saved, setSaved] = useState<Set<string>>(new Set());
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function submit(e: FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError(null);
    try {
      rememberResume(resume);
      const found = await searchJobs(city.trim(), query.trim(), resume);
      setResults(found);
      setSearchedCity(city.trim());
      setSaved(new Set());
    } catch (err) {
      setResults(null);
      setError(err instanceof Error ? err.message : 'Search failed.');
    } finally {
      setBusy(false);
    }
  }

  async function saveToTracker(match: JobMatch) {
    setError(null);
    try {
      const notes =
        `Found via job search in ${searchedCity}. Resume match ${match.score}%.` +
        (match.missing.length > 0 ? ` Missing: ${match.missing.join(', ')}.` : '');
      await createApplication({
        company: match.job.company.slice(0, 200),
        role: match.job.title.slice(0, 200),
        link: match.job.url,
        status: 'Saved',
        appliedDate: null,
        followUpDate: null,
        notes,
      });
      setSaved((prev) => new Set(prev).add(match.job.id));
      onSaved();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not save to your tracker.');
    }
  }

  return (
    <section className="stack">
      <form onSubmit={submit} className="card stack">
        <h2>Find jobs</h2>
        <p className="muted">
          Search open jobs in a city and see which ones fit your resume best. Your resume is sent only to
          rank the results; it is not stored on the server.
        </p>
        <div className="grid">
          <label>
            City
            <input
              value={city}
              onChange={(e) => setCity(e.target.value)}
              placeholder="e.g. Cincinnati"
              required
              maxLength={100}
            />
          </label>
          <label>
            Job title or keywords (optional)
            <input
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              placeholder="e.g. .NET developer"
              maxLength={100}
            />
          </label>
        </div>
        <label>
          Your resume
          <textarea
            value={resume}
            onChange={(e) => setResume(e.target.value)}
            rows={8}
            required
            maxLength={20000}
            placeholder="Paste your resume text. It is remembered in this browser only."
          />
        </label>
        {error && <p className="error" role="alert">{error}</p>}
        <div className="row">
          <button type="submit" disabled={busy}>{busy ? 'Searching…' : 'Search jobs'}</button>
          <span className="muted small">
            Jobs by <a href="https://www.adzuna.com" target="_blank" rel="noopener noreferrer">Adzuna</a>
          </span>
        </div>
      </form>

      {results && results.length === 0 && (
        <div className="card empty-state">
          <h2>No jobs found</h2>
          <p className="muted">Try a broader title, or a larger nearby city.</p>
        </div>
      )}

      {results && results.length > 0 && (
        <>
          <p className="muted">
            {results.length} open jobs near {searchedCity}, best fit first. Scores compare skills in the
            posting with your resume, so treat them as a guide.
          </p>
          <div className="cards">
            {results.map((m) => {
              const t = tier(m.score);
              return (
                <article className="app-card" key={m.job.id}>
                  <div className="app-head">
                    <div className="app-title">
                      <strong>{m.job.title}</strong>
                      <span className="muted">
                        {m.job.company}
                        {m.job.location && ` · ${m.job.location}`}
                      </span>
                    </div>
                    <span className={`badge match-${t}`}>{m.score}% · {tierLabel[t]}</span>
                  </div>

                  {m.job.salary && <p className="small"><strong>{m.job.salary}</strong></p>}
                  {m.job.description && <p className="notes">{shorten(m.job.description, 220)}</p>}

                  {m.matched.length > 0 && (
                    <div className="chips">
                      {m.matched.map((s) => <span key={s} className="chip ok">{s}</span>)}
                    </div>
                  )}
                  {m.missing.length > 0 && (
                    <div className="chips">
                      {m.missing.map((s) => <span key={s} className="chip bad">{s}</span>)}
                    </div>
                  )}
                  {m.matched.length + m.missing.length === 0 && (
                    <p className="muted small">No known skills were listed in this posting&apos;s preview.</p>
                  )}

                  <div className="card-actions">
                    <a href={m.job.url} target="_blank" rel="noopener noreferrer">View posting ↗</a>
                    <button
                      className="secondary small-btn"
                      onClick={() => saveToTracker(m)}
                      disabled={saved.has(m.job.id)}
                    >
                      {saved.has(m.job.id) ? 'Saved ✓' : 'Save to tracker'}
                    </button>
                  </div>
                </article>
              );
            })}
          </div>
        </>
      )}
    </section>
  );
}
