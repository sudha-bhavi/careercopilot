import { useState, type FormEvent } from 'react';
import { matchResume } from '../api';
import type { MatchResult } from '../types';

export function ResumeMatch() {
  const [resume, setResume] = useState('');
  const [job, setJob] = useState('');
  const [result, setResult] = useState<MatchResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function submit(e: FormEvent) {
    e.preventDefault();
    setBusy(true);
    setError(null);
    try {
      setResult(await matchResume(resume, job));
    } catch (err) {
      setResult(null);
      setError(err instanceof Error ? err.message : 'Match failed.');
    } finally {
      setBusy(false);
    }
  }

  return (
    <section className="stack">
      <form onSubmit={submit} className="card stack">
        <h2>Resume match</h2>
        <p className="muted">
          Paste your resume and a job description to see which skills match and which are missing.
          Nothing you paste is stored.
        </p>
        <div className="grid">
          <label>
            Your resume
            <textarea value={resume} onChange={(e) => setResume(e.target.value)} rows={12} required maxLength={20000} />
          </label>
          <label>
            Job description
            <textarea value={job} onChange={(e) => setJob(e.target.value)} rows={12} required maxLength={20000} />
          </label>
        </div>
        {error && <p className="error" role="alert">{error}</p>}
        <button type="submit" disabled={busy}>{busy ? 'Matching…' : 'Check match'}</button>
      </form>

      {result && (
        <div className="card stack" aria-live="polite">
          <p className="score">
            {result.score}% <span className="muted">match</span>
          </p>
          {result.matched.length + result.missing.length === 0 && (
            <p className="muted">No known skills were found in this job description.</p>
          )}
          {result.matched.length > 0 && (
            <div>
              <h3>Matched</h3>
              <div className="chips">
                {result.matched.map((s) => <span key={s} className="chip ok">{s}</span>)}
              </div>
            </div>
          )}
          {result.missing.length > 0 && (
            <div>
              <h3>Missing from your resume</h3>
              <div className="chips">
                {result.missing.map((s) => <span key={s} className="chip bad">{s}</span>)}
              </div>
            </div>
          )}
        </div>
      )}
    </section>
  );
}
