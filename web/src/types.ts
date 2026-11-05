export const STATUSES = ['Saved', 'Applied', 'Interview', 'Offer', 'Rejected'] as const;
export type Status = (typeof STATUSES)[number];

export interface JobApplication {
  id: string;
  company: string;
  role: string;
  link: string | null;
  status: Status;
  appliedDate: string | null;
  followUpDate: string | null;
  notes: string | null;
  createdAt: string;
}

// What the form sends. No id or createdAt: the server owns those.
export interface ApplicationInput {
  company: string;
  role: string;
  link: string | null;
  status: Status;
  appliedDate: string | null;
  followUpDate: string | null;
  notes: string | null;
}

export interface Dashboard {
  total: number;
  byStatus: Record<Status, number>;
  followUpsDue: number;
}

export interface JobListing {
  id: string;
  title: string;
  company: string;
  location: string;
  description: string;
  url: string;
  salary: string | null;
  postedAt: string | null;
}

export interface JobMatch {
  job: JobListing;
  score: number;
  matched: string[];
  missing: string[];
}

export interface MatchResult {
  score: number;
  matched: string[];
  missing: string[];
}
