import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { HexColorPicker } from 'react-colorful'
import { Bar, BarChart, CartesianGrid, Cell, Line, LineChart, Pie, PieChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import './App.css'

const PAGE_SIZE = 10
let accessToken = null
let refreshRequest = null

const icons = {
  dashboard: 'M4 13h6V3H4v10Zm0 8h6v-6H4v6Zm10 0h6V11h-6v10Zm0-18v6h6V3h-6Z',
  projects: 'M4 5.5A2.5 2.5 0 0 1 6.5 3h3.1c.7 0 1.3.3 1.8.8l.8.9c.2.2.5.3.8.3h4.5A2.5 2.5 0 0 1 20 7.5v9a2.5 2.5 0 0 1-2.5 2.5h-11A2.5 2.5 0 0 1 4 16.5v-11Z',
  statistics: 'M4 19V9m6 10V5m6 14v-7m4 7H2',
  legacy: 'M5 5v14h14M8 15l3-3 3 2 4-5',
  timer: 'M9 2h6M12 8v5l3 2M12 22a9 9 0 1 0 0-18 9 9 0 0 0 0 18Z',
  plus: 'M12 5v14M5 12h14',
  edit: 'm4 20 4.2-1 9.9-9.9a2.8 2.8 0 0 0-4-4L4.2 15 4 20Zm8.7-13.5 4 4',
  logout: 'M14 8V5a2 2 0 0 0-2-2H5a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h7a2 2 0 0 0 2-2v-3m-4-4h11m0 0-3-3m3 3-3 3',
  close: 'M6 6l12 12M18 6 6 18',
  chevronLeft: 'm15 18-6-6 6-6',
  chevronRight: 'm9 18 6-6-6-6',
  play: 'm8 5 11 7-11 7V5Z',
  stop: 'M7 7h10v10H7z',
  delete: 'M4 7h16M9 7V4h6v3m3 0-1 14H7L6 7m4 4v6m4-6v6',
}

function Icon({ name, size = 20 }) {
  return (
    <svg width={size} height={size} viewBox="0 0 24 24" fill="none" aria-hidden="true">
      <path d={icons[name]} stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  )
}

function formatDuration(totalSeconds = 0) {
  const value = Math.max(0, Math.floor(totalSeconds))
  const hours = Math.floor(value / 3600)
  const minutes = Math.floor((value % 3600) / 60)
  const seconds = value % 60
  return [hours, minutes, seconds].map((part) => String(part).padStart(2, '0')).join(':')
}

function formatDate(date) {
  return new Intl.DateTimeFormat('en', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(date))
}

function entrySecondsInRange(entry, rangeStart, rangeEnd, runningTimer, elapsed) {
  const duration = Math.max(
    0,
    entry.isRunning && runningTimer?.timeEntryId === entry.id
      ? elapsed
      : entry.durationSeconds || 0,
  )
  if (duration === 0) return 0

  const segmentEnd = entry.isRunning
    ? new Date()
    : new Date(entry.stoppedAtUtc || entry.startedAtUtc)
  const recordedStart = new Date(entry.startedAtUtc)
  const durationStart = new Date(segmentEnd.getTime() - duration * 1000)
  const segmentStart = durationStart > recordedStart ? durationStart : recordedStart
  const overlapStart = Math.max(segmentStart.getTime(), new Date(rangeStart).getTime())
  const overlapEnd = Math.min(segmentEnd.getTime(), new Date(rangeEnd).getTime())
  return Math.max(0, Math.floor((overlapEnd - overlapStart) / 1000))
}

async function refreshAccessToken() {
  if (!refreshRequest) {
    refreshRequest = fetch('/api/auth/refresh', {
      method: 'POST',
      credentials: 'include',
    })
      .then(async (response) => {
        const payload = await response.json().catch(() => null)
        if (!response.ok || !payload?.data?.token) {
          const error = new Error(payload?.message || 'Your session has expired.')
          error.status = response.status
          throw error
        }
        accessToken = payload.data.token
        return payload.data
      })
      .finally(() => {
        refreshRequest = null
      })
  }
  return refreshRequest
}

async function apiRequest(path, options = {}) {
  const { skipAuthRefresh = false, ...fetchOptions } = options
  const response = await fetch(path, {
    ...fetchOptions,
    credentials: 'include',
    headers: {
      ...(fetchOptions.body ? { 'Content-Type': 'application/json' } : {}),
      ...(accessToken ? { Authorization: `Bearer ${accessToken}` } : {}),
      ...fetchOptions.headers,
    },
  })

  const payload = await response.json().catch(() => null)
  if (response.status === 401 && !skipAuthRefresh) {
    await refreshAccessToken()
    return apiRequest(path, { ...options, skipAuthRefresh: true })
  }
  if (!response.ok) {
    const error = new Error(payload?.message || `Request failed with status ${response.status}.`)
    error.status = response.status
    error.details = payload?.errors || []
    throw error
  }
  return payload
}

function Sidebar({ page, setPage, authenticated, onLogout }) {
  return (
    <aside className="sidebar">
      <div className="brand-mark"><span>TT</span></div>
      <nav>
        <button className={page === 'dashboard' ? 'active' : ''} disabled={!authenticated} onClick={() => setPage('dashboard')}>
          <Icon name="dashboard" /><span>Dashboard</span>
        </button>
        <button className={page === 'projects' ? 'active' : ''} disabled={!authenticated} onClick={() => setPage('projects')}>
          <Icon name="projects" /><span>Projects</span>
        </button>
        <button className={page === 'statistics' ? 'active' : ''} disabled={!authenticated} onClick={() => setPage('statistics')}>
          <Icon name="statistics" /><span>Statistics</span>
        </button>
        <button className={page === 'legacy' ? 'active' : ''} disabled={!authenticated} onClick={() => setPage('legacy')}>
          <Icon name="legacy" /><span>Legacy</span>
        </button>
      </nav>
      {authenticated && (
        <button className="sidebar-logout" onClick={onLogout}>
          <Icon name="logout" /><span>Log out</span>
        </button>
      )}
    </aside>
  )
}

function Login({ onLogin }) {
  const [form, setForm] = useState({ username: '', password: '' })
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  async function submit(event) {
    event.preventDefault()
    setLoading(true)
    setError('')
    try {
      const response = await apiRequest('/api/auth/login', {
        method: 'POST',
        body: JSON.stringify(form),
        skipAuthRefresh: true,
      })
      await onLogin(response.data)
    } catch (requestError) {
      setError(requestError.message)
    } finally {
      setLoading(false)
    }
  }

  return (
    <main className="login-page">
      <div className="login-glow" />
      <section className="login-card">
        <div className="eyebrow">Welcome back</div>
        <h1>Track time.<br /><span>Make it count.</span></h1>
        <p>Sign in to focus on the work that matters.</p>
        <form onSubmit={submit}>
          <label>Username<input value={form.username} onChange={(e) => setForm({ ...form, username: e.target.value })} autoComplete="username" /></label>
          <label>Password<input type="password" value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} autoComplete="current-password" /></label>
          {error && <div className="form-error">{error}</div>}
          <button className="primary-button login-button" disabled={loading}>
            {loading ? 'Signing in…' : 'Sign in'}
          </button>
        </form>
        <div className="login-footnote"><span />Your time stays yours<span /></div>
      </section>
    </main>
  )
}

function Modal({ title, subtitle, children, onClose }) {
  return (
    <div className="modal-backdrop" onMouseDown={(e) => e.target === e.currentTarget && onClose()}>
      <section className="modal-card" role="dialog" aria-modal="true">
        <div className="modal-heading">
          <div><h2>{title}</h2>{subtitle && <p>{subtitle}</p>}</div>
          <button className="icon-button" onClick={onClose} aria-label="Close"><Icon name="close" /></button>
        </div>
        {children}
      </section>
    </div>
  )
}

function ColorPickerPopover({ color, onChange }) {
  const [hexValue, setHexValue] = useState(color.toUpperCase())
  const validHex = /^#[0-9A-F]{6}$/.test(hexValue)

  function updatePicker(colorValue) {
    setHexValue(colorValue.toUpperCase())
    onChange(colorValue)
  }

  function updateHexValue(event) {
    const value = event.target.value.toUpperCase()
    setHexValue(value)
    if (/^#[0-9A-F]{6}$/.test(value)) onChange(value)
  }

  return (
    <div className="color-popover">
      <HexColorPicker color={color} onChange={updatePicker} />
      <label className="hex-color-field">
        Hex color
        <input
          value={hexValue}
          onChange={updateHexValue}
          onKeyDown={(event) => {
            if (event.key === 'Enter') event.preventDefault()
          }}
          maxLength={7}
          spellCheck="false"
          aria-invalid={!validHex}
          placeholder="#EE1C9A"
        />
        {!validHex && <small>Enter a color in #RRGGBB format.</small>}
      </label>
    </div>
  )
}

function ProjectFormModal({ project, onClose, onSaved }) {
  const editing = Boolean(project)
  const [form, setForm] = useState({
    name: project?.name || '',
    description: project?.description || '',
    color: project?.color || '#D946EF',
  })
  const [showPicker, setShowPicker] = useState(false)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  async function submit(event) {
    event.preventDefault()
    setLoading(true)
    setError('')
    try {
      const response = await apiRequest(editing ? `/api/projects/${project.id}` : '/api/projects', {
        method: editing ? 'PUT' : 'POST',
        body: JSON.stringify({ ...form, description: form.description || null }),
      })
      onSaved(response.data)
      onClose()
    } catch (requestError) {
      setError([requestError.message, ...requestError.details].filter(Boolean).join(' '))
    } finally {
      setLoading(false)
    }
  }

  return (
    <Modal title={editing ? 'Edit project' : 'Create a project'} subtitle={editing ? 'Update the details for this project.' : 'Give your next block of focused work a home.'} onClose={onClose}>
      <form className="project-form" onSubmit={submit}>
        <label>Project name<input autoFocus value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} placeholder="e.g. Website redesign" /></label>
        <label>Description <span>Optional</span><textarea rows="4" value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} placeholder="What are you working toward?" /></label>
        <div className="color-field">
          <span>Project color</span>
          <div className="color-control">
            <button type="button" className="color-swatch" style={{ background: form.color }} onClick={() => setShowPicker(!showPicker)} aria-label="Choose project color" />
            <button type="button" className="color-value" onClick={() => setShowPicker(!showPicker)}>{form.color.toUpperCase()}</button>
          </div>
          {showPicker && (
            <ColorPickerPopover color={form.color} onChange={(color) => setForm({ ...form, color })} />
          )}
        </div>
        {error && <div className="form-error">{error}</div>}
        <div className="modal-actions">
          <button type="button" className="secondary-button" onClick={onClose}>Cancel</button>
          <button className="primary-button" disabled={loading}>{loading ? 'Saving…' : editing ? 'Save changes' : 'Create project'}</button>
        </div>
      </form>
    </Modal>
  )
}

function TimerModal({ projects, onClose, onStarted }) {
  const [projectId, setProjectId] = useState(projects[0]?.id || '')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  async function start() {
    if (!projectId) return
    setLoading(true)
    setError('')
    try {
      const response = await apiRequest(`/api/projects/${projectId}/timer/start`, { method: 'POST' })
      onStarted(response.data)
      onClose()
    } catch (requestError) {
      setError(requestError.message)
    } finally {
      setLoading(false)
    }
  }

  return (
    <Modal title="Start a timer" subtitle="Choose the project you want to focus on." onClose={onClose}>
      <div className="timer-project-list">
        {projects.length === 0 ? <div className="empty-inline">Create a project before starting a timer.</div> : projects.map((project) => (
          <button key={project.id} className={Number(projectId) === project.id ? 'selected' : ''} onClick={() => setProjectId(project.id)}>
            <span className="project-dot" style={{ background: project.color }} />
            <span><strong>{project.name}</strong><small>{project.description || 'No description'}</small></span>
            <i />
          </button>
        ))}
      </div>
      {error && <div className="form-error">{error}</div>}
      <div className="modal-actions">
        <button className="secondary-button" onClick={onClose}>Cancel</button>
        <button className="primary-button" onClick={start} disabled={!projectId || loading}><Icon name="play" size={17} />{loading ? 'Starting…' : 'Start timer'}</button>
      </div>
    </Modal>
  )
}

function DeleteConfirmationModal({ title, subtitle, color, name, details, warning, confirmLabel, onClose, onConfirm }) {
  const [loading, setLoading] = useState(false)

  async function confirmDelete() {
    setLoading(true)
    await onConfirm()
    setLoading(false)
  }

  return (
    <Modal title={title} subtitle={subtitle} onClose={loading ? () => {} : onClose}>
      <div className="delete-confirmation">
        <span className="project-dot large" style={{ background: color }} />
        <div>
          <strong>{name}</strong>
          <small>{details}</small>
        </div>
      </div>
      <p className="delete-warning">{warning}</p>
      <div className="modal-actions">
        <button className="secondary-button" onClick={onClose} disabled={loading}>Cancel</button>
        <button className="danger-button" onClick={confirmDelete} disabled={loading}>
          <Icon name="delete" size={16} />{loading ? 'Deleting…' : confirmLabel}
        </button>
      </div>
    </Modal>
  )
}

function ManualTimeModal({ project, onClose, onSaved }) {
  const [time, setTime] = useState('00:00')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  async function submit(event) {
    event.preventDefault()
    setError('')

    const match = /^(\d+):([0-5]\d)$/.exec(time.trim())
    if (!match) {
      setError('Enter time as HH:MM. Minutes must be between 00 and 59.')
      return
    }

    const hours = Number(match[1])
    const minutes = Number(match[2])
    if (hours === 0 && minutes === 0) {
      setError('Manual time must be greater than 00:00.')
      return
    }

    setLoading(true)
    try {
      const response = await apiRequest(`/api/projects/${project.id}/manual-time`, {
        method: 'POST',
        body: JSON.stringify({ hours, minutes }),
      })
      onSaved(response.data)
      onClose()
    } catch (requestError) {
      setError([requestError.message, ...requestError.details].filter(Boolean).join(' '))
    } finally {
      setLoading(false)
    }
  }

  return (
    <Modal title="Add manual time" subtitle={`Add time directly to ${project.name}.`} onClose={onClose}>
      <form className="project-form" onSubmit={submit}>
        <label>
          Hours and minutes
          <input className="manual-time-input" autoFocus value={time} onChange={(event) => setTime(event.target.value)} placeholder="00:00" inputMode="numeric" aria-describedby="manual-time-help" />
        </label>
        <small id="manual-time-help" className="field-help">Use HH:MM — for example, 35:30 means 35 hours and 30 minutes.</small>
        <div className="manual-time-current">Current manual time: <strong>{formatDuration(project.manualTimeSeconds)}</strong></div>
        {error && <div className="form-error">{error}</div>}
        <div className="modal-actions">
          <button type="button" className="secondary-button" onClick={onClose} disabled={loading}>Cancel</button>
          <button className="primary-button" disabled={loading}><Icon name="plus" size={17} />{loading ? 'Adding…' : 'Add time'}</button>
        </div>
      </form>
    </Modal>
  )
}

function Dashboard({ entries, projects, runningTimer, elapsed, onResume, onStop, onDelete, actionEntryId }) {
  const [page, setPage] = useState(1)
  const todayDate = new Date().toDateString()
  const entryDuration = (entry) =>
    entry.isRunning && runningTimer?.timeEntryId === entry.id
      ? elapsed
      : entry.durationSeconds
  const trackedTotal = entries.reduce((sum, entry) => sum + entryDuration(entry), 0)
  const manualTotal = projects.reduce((sum, project) => sum + (project.manualTimeSeconds || 0), 0)
  const todayStart = startOfDay(new Date())
  const trackedToday = entries.reduce((sum, entry) =>
    sum + entrySecondsInRange(entry, todayStart, endOfDay(todayStart), runningTimer, elapsed), 0)
  const manualToday = projects.flatMap((project) => project.manualTimeAdjustments || [])
    .filter((adjustment) => new Date(adjustment.addedAtUtc).toDateString() === todayDate)
    .reduce((sum, adjustment) => sum + adjustment.durationSeconds, 0)
  const total = trackedTotal + manualTotal
  const today = trackedToday + manualToday
  const projectCount = new Set(entries.map((entry) => entry.projectId)).size
  const pageCount = Math.max(1, Math.ceil(entries.length / PAGE_SIZE))
  const safePage = Math.min(page, pageCount)
  const visibleEntries = useMemo(
    () => entries.slice((safePage - 1) * PAGE_SIZE, safePage * PAGE_SIZE),
    [entries, safePage],
  )

  return (
    <div className="page-content">
      <div className="page-title"><div><span className="eyebrow">Overview</span><h1>Dashboard</h1><p>A clear view of where your time has gone.</p></div></div>
      <div className="stats-grid">
        <div className="stat-card accent"><span>Today</span><strong>{formatDuration(today)}</strong><small>Focused time</small></div>
        <div className="stat-card"><span>All time</span><strong>{formatDuration(total)}</strong><small>Across every session</small></div>
        <div className="stat-card"><span>Projects tracked</span><strong>{projectCount}</strong><small>With time entries</small></div>
      </div>
      <section className="panel">
        <div className="panel-header"><div><h2>Recent time entries</h2><p>Your latest tracked sessions</p></div><span className="entry-count">{entries.length} entries</span></div>
        {entries.length === 0 ? (
          <div className="empty-state"><div className="empty-icon"><Icon name="timer" size={28} /></div><h3>No time entries yet</h3><p>Start a timer and your work will appear here.</p></div>
        ) : (
          <div className="entries-list">
            {visibleEntries.map((entry) => (
              <div className="entry-row" key={entry.id}>
                <span className="project-dot large" style={{ background: entry.projectColor }} />
                <div><strong>{entry.projectName}</strong><small>{formatDate(entry.startedAtUtc)}</small></div>
                <div className="entry-actions">
                  {entry.isRunning ? (
                    <button className="entry-timer-button stop" onClick={onStop} disabled={actionEntryId === entry.id} title="Stop this entry">
                      <Icon name="stop" size={15} />Stop
                    </button>
                  ) : (
                    <button className="entry-timer-button" onClick={() => onResume(entry.id)} disabled={Boolean(runningTimer) || actionEntryId === entry.id} title={runningTimer ? 'Stop the running timer first' : 'Continue this entry'}>
                      <Icon name="play" size={15} />Continue
                    </button>
                  )}
                  <button className="entry-delete-button" onClick={() => onDelete(entry)} disabled={entry.isRunning || actionEntryId === entry.id} title={entry.isRunning ? 'Stop this entry before deleting it' : 'Delete this entry'} aria-label={`Delete ${entry.projectName} time entry`}>
                    <Icon name="delete" size={15} />
                  </button>
                </div>
                <time>{formatDuration(entry.isRunning && runningTimer?.timeEntryId === entry.id ? elapsed : entry.durationSeconds)}</time>
              </div>
            ))}
          </div>
        )}
        {entries.length > PAGE_SIZE && (
          <div className="pagination dashboard-pagination">
            <span>Showing {(safePage - 1) * PAGE_SIZE + 1}–{Math.min(safePage * PAGE_SIZE, entries.length)} of {entries.length}</span>
            <div>
              <button disabled={safePage === 1} onClick={() => setPage(safePage - 1)} aria-label="Previous time entries page"><Icon name="chevronLeft" size={18} /></button>
              {Array.from({ length: pageCount }, (_, index) => index + 1).map((number) => (
                <button className={number === safePage ? 'active' : ''} key={number} onClick={() => setPage(number)}>{number}</button>
              ))}
              <button disabled={safePage === pageCount} onClick={() => setPage(safePage + 1)} aria-label="Next time entries page"><Icon name="chevronRight" size={18} /></button>
            </div>
          </div>
        )}
      </section>
    </div>
  )
}

function Projects({ projects, onCreate, onEdit, onDelete, onAddTime }) {
  const [page, setPage] = useState(1)
  const pageCount = Math.max(1, Math.ceil(projects.length / PAGE_SIZE))
  const safePage = Math.min(page, pageCount)
  const visible = useMemo(() => projects.slice((safePage - 1) * PAGE_SIZE, safePage * PAGE_SIZE), [projects, safePage])

  return (
    <div className="page-content">
      <div className="page-title">
        <div><span className="eyebrow">Workspace</span><h1>Projects</h1><p>Organize your time around the work that matters.</p></div>
        <button className="primary-button" onClick={onCreate}><Icon name="plus" size={18} />New project</button>
      </div>
      <section className="panel projects-panel">
        <div className="panel-header"><div><h2>All projects</h2><p>{projects.length} {projects.length === 1 ? 'project' : 'projects'} in your workspace</p></div></div>
        <div className="projects-table">
          <div className="project-table-head"><span>Project</span><span>Description</span><span>Total time</span><span>Manual</span><span>Edit</span><span>Delete</span></div>
          {visible.length === 0 ? (
            <div className="empty-state"><div className="empty-icon"><Icon name="projects" size={28} /></div><h3>No projects yet</h3><p>Create your first project to start tracking time.</p><button className="primary-button" onClick={onCreate}>Create project</button></div>
          ) : visible.map((project) => (
            <div className="project-table-row" key={project.id}>
              <div className="project-name"><span className="project-dot large" style={{ background: project.color }} /><strong>{project.name}</strong></div>
              <span className="description-cell">{project.description || '—'}</span>
              <time>{formatDuration(project.totalTimeSeconds)}</time>
              <button className="table-action static-action" onClick={() => onAddTime(project)} title={`Add manual time to ${project.name}`} aria-label={`Add manual time to ${project.name}`}><Icon name="plus" size={18} /></button>
              <button className="table-action" onClick={() => onEdit(project)} aria-label={`Edit ${project.name}`}><Icon name="edit" size={18} /></button>
              <button className="table-action delete-action" onClick={() => onDelete(project)} aria-label={`Delete ${project.name}`}><Icon name="delete" size={17} /></button>
            </div>
          ))}
        </div>
        {projects.length > PAGE_SIZE && (
          <div className="pagination">
            <span>Showing {(safePage - 1) * PAGE_SIZE + 1}–{Math.min(safePage * PAGE_SIZE, projects.length)} of {projects.length}</span>
            <div>
              <button disabled={safePage === 1} onClick={() => setPage(safePage - 1)}><Icon name="chevronLeft" size={18} /></button>
              {Array.from({ length: pageCount }, (_, index) => index + 1).map((number) => <button className={number === safePage ? 'active' : ''} key={number} onClick={() => setPage(number)}>{number}</button>)}
              <button disabled={safePage === pageCount} onClick={() => setPage(safePage + 1)}><Icon name="chevronRight" size={18} /></button>
            </div>
          </div>
        )}
      </section>
    </div>
  )
}

function LegacyProjectModal({ provider, project, onClose, onSaved }) {
  const editing = Boolean(project)
  const [form, setForm] = useState({ name: project?.name || '', description: project?.description || '', color: project?.color || '#D946EF' })
  const [showPicker, setShowPicker] = useState(false)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  async function submit(event) {
    event.preventDefault()
    setLoading(true)
    setError('')
    try {
      const response = await apiRequest(editing ? `/api/legacy-projects/${project.id}` : `/api/legacy-projects/${provider}`, {
        method: editing ? 'PUT' : 'POST',
        body: JSON.stringify({ ...form, description: form.description || null }),
      })
      onSaved(response.data)
      onClose()
    } catch (requestError) {
      setError([requestError.message, ...requestError.details].filter(Boolean).join(' '))
    } finally {
      setLoading(false)
    }
  }

  return (
    <Modal title={editing ? 'Edit legacy project' : `Add ${provider === 'TogglTrack' ? 'Toggle Track' : provider} legacy project`} subtitle={editing ? 'Update this archived project without changing its time.' : 'Import a project total without affecting your current statistics.'} onClose={onClose}>
      <form className="project-form" onSubmit={submit}>
        <label>Project name<input autoFocus value={form.name} onChange={(event) => setForm({ ...form, name: event.target.value })} placeholder="e.g. Archived website" /></label>
        <label>Description <span>Optional</span><textarea rows="4" value={form.description} onChange={(event) => setForm({ ...form, description: event.target.value })} placeholder="What was this project about?" /></label>
        <div className="color-field">
          <span>Project color</span>
          <div className="color-control">
            <button type="button" className="color-swatch" style={{ background: form.color }} onClick={() => setShowPicker(!showPicker)} aria-label="Choose project color" />
            <button type="button" className="color-value" onClick={() => setShowPicker(!showPicker)}>{form.color.toUpperCase()}</button>
          </div>
          {showPicker && <ColorPickerPopover color={form.color} onChange={(color) => setForm({ ...form, color })} />}
        </div>
        {error && <div className="form-error">{error}</div>}
        <div className="modal-actions"><button type="button" className="secondary-button" onClick={onClose}>Cancel</button><button className="primary-button" disabled={loading}>{loading ? 'Saving…' : editing ? 'Save changes' : 'Add legacy project'}</button></div>
      </form>
    </Modal>
  )
}

function LegacyTimeModal({ project, onClose, onSaved }) {
  const [time, setTime] = useState('00:00')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  async function submit(event) {
    event.preventDefault()
    const match = /^(\d+):([0-5]\d)$/.exec(time.trim())
    if (!match || (Number(match[1]) === 0 && Number(match[2]) === 0)) {
      setError('Enter a time greater than 00:00 using HH:MM.')
      return
    }
    setLoading(true)
    setError('')
    try {
      const response = await apiRequest(`/api/legacy-projects/${project.id}/time`, {
        method: 'POST',
        body: JSON.stringify({ hours: Number(match[1]), minutes: Number(match[2]) }),
      })
      onSaved(response.data)
      onClose()
    } catch (requestError) {
      setError([requestError.message, ...requestError.details].filter(Boolean).join(' '))
    } finally {
      setLoading(false)
    }
  }

  return (
    <Modal title="Add legacy time" subtitle={`Add an all-time total to ${project.name}.`} onClose={onClose}>
      <form className="project-form" onSubmit={submit}>
        <label>Hours and minutes<input className="manual-time-input" autoFocus value={time} onChange={(event) => setTime(event.target.value)} placeholder="00:00" inputMode="numeric" /></label>
        <small className="field-help">Use HH:MM — for example, 354:30 means 354 hours and 30 minutes.</small>
        <div className="manual-time-current">Current legacy time: <strong>{formatHoursMinutes(project.totalTimeSeconds)}</strong></div>
        {error && <div className="form-error">{error}</div>}
        <div className="modal-actions"><button type="button" className="secondary-button" onClick={onClose}>Cancel</button><button className="primary-button" disabled={loading}><Icon name="plus" size={17} />{loading ? 'Adding…' : 'Add time'}</button></div>
      </form>
    </Modal>
  )
}

function LegacySection({ title, provider, projects, onCreate, onEdit, onDelete, onAddTime }) {
  const [activeIndex, setActiveIndex] = useState(null)
  const chartData = projects.filter((project) => project.totalTimeSeconds > 0)
  const total = chartData.reduce((sum, project) => sum + project.totalTimeSeconds, 0)
  const percentageLabel = ({ cx, cy, midAngle, outerRadius, percent }) => {
    const radius = outerRadius + 25
    const angle = -midAngle * Math.PI / 180
    return <text x={cx + radius * Math.cos(angle)} y={cy + radius * Math.sin(angle)} fill="#cfc5d4" textAnchor={Math.cos(angle) >= 0 ? 'start' : 'end'} dominantBaseline="central" className="chart-percentage">{`${Math.round(percent * 100)}%`}</text>
  }

  return (
    <section className="legacy-provider-section">
      <div className="statistics-section-heading legacy-heading"><span className="eyebrow">Imported time</span><h2>{title}</h2><p>All-time totals from your previous workspace.</p></div>
      <div className="statistics-layout legacy-layout">
        <div className="statistics-chart legacy-chart panel">
          <div className="panel-header"><div><h2>{title} project time</h2><p>All-time distribution</p></div><strong className="legacy-total">{formatHoursMinutes(total)}</strong></div>
          {chartData.length ? <>
            <div className="doughnut-wrap">
              <ResponsiveContainer width="100%" height="100%"><PieChart>
                <Pie data={chartData} dataKey="totalTimeSeconds" nameKey="name" cx="50%" cy="50%" innerRadius="53%" outerRadius="72%" paddingAngle={2} stroke="none" labelLine={false} label={percentageLabel} isAnimationActive={false} onMouseLeave={() => setActiveIndex(null)}>
                  {chartData.map((project, index) => <Cell key={project.id} fill={project.color} onMouseEnter={() => setActiveIndex(index)} className={activeIndex === index ? 'active-chart-segment' : ''} />)}
                </Pie>
                <Tooltip content={({ active, payload }) => active && payload?.length ? <div className="chart-tooltip"><span className="project-dot" style={{ background: payload[0].payload.color }} /><div><strong>{payload[0].name}</strong><small>{formatHoursMinutes(payload[0].value)} hours</small></div></div> : null} />
              </PieChart></ResponsiveContainer>
              <div className="chart-center"><strong>{formatHoursMinutes(total)}</strong><span>total hours</span></div>
            </div>
          </> : <div className="empty-state statistics-empty"><div className="empty-icon"><Icon name="legacy" size={28} /></div><h3>No {title} time yet</h3><p>Add a legacy project, then enter its all-time total.</p></div>}
        </div>
        <aside className="legacy-projects-panel panel">
          <div className="panel-header"><div><h2>Legacy projects</h2><p>{projects.length} {projects.length === 1 ? 'project' : 'projects'}</p></div></div>
          <div className="legacy-panel-body">
            <button className="primary-button legacy-add-button" onClick={() => onCreate(provider)}><Icon name="plus" size={17} />Add legacy project</button>
            <div className="legacy-project-list">
              {projects.map((project) => <div key={project.id}><span className="project-dot large" style={{ background: project.color }} /><div><strong>{project.name}</strong><small>{formatHoursMinutes(project.totalTimeSeconds)}</small></div><div className="legacy-row-actions"><button onClick={() => onAddTime(project)} title={`Add time to ${project.name}`} aria-label={`Add time to ${project.name}`}><Icon name="plus" size={15} /></button><button onClick={() => onEdit(project)} title={`Edit ${project.name}`} aria-label={`Edit ${project.name}`}><Icon name="edit" size={15} /></button><button className="delete" onClick={() => onDelete(project)} title={`Delete ${project.name}`} aria-label={`Delete ${project.name}`}><Icon name="delete" size={14} /></button></div></div>)}
            </div>
          </div>
        </aside>
      </div>
    </section>
  )
}

function Legacy({ legacyProjects, onCreate, onEdit, onDelete, onAddTime }) {
  return (
    <div className="page-content legacy-page">
      <div className="page-title"><div><span className="eyebrow">Archive</span><h1>Legacy</h1><p>Keep historical totals separate from your active tracking workspace.</p></div></div>
      <LegacySection title="Clockify" provider="Clockify" projects={legacyProjects.Clockify || []} onCreate={onCreate} onEdit={onEdit} onDelete={onDelete} onAddTime={onAddTime} />
      <LegacySection title="Toggle Track" provider="TogglTrack" projects={legacyProjects.TogglTrack || []} onCreate={onCreate} onEdit={onEdit} onDelete={onDelete} onAddTime={onAddTime} />
    </div>
  )
}

const statisticRanges = [
  ['today', 'Day'],
  ['yesterday', 'Yesterday'],
  ['week', 'Week'],
  ['lastWeek', 'Last week'],
  ['month', 'Month'],
  ['lastMonth', 'Last month'],
  ['year', 'Year'],
  ['lastYear', 'Last year'],
  ['all', 'All time'],
  ['custom', 'Custom'],
]

function startOfDay(value) {
  const date = new Date(value)
  date.setHours(0, 0, 0, 0)
  return date
}

function endOfDay(value) {
  const date = new Date(value)
  date.setHours(23, 59, 59, 999)
  return date
}

function getStatisticRange(range, customStart, customEnd, dates) {
  const today = startOfDay(new Date())
  const endToday = endOfDay(today)
  if (range === 'today') return { start: today, end: endToday }
  if (range === 'yesterday') {
    const day = new Date(today)
    day.setDate(day.getDate() - 1)
    return { start: day, end: endOfDay(day) }
  }
  if (range === 'week' || range === 'lastWeek') {
    const start = new Date(today)
    start.setDate(start.getDate() - ((start.getDay() + 6) % 7) - (range === 'lastWeek' ? 7 : 0))
    const end = range === 'lastWeek' ? endOfDay(new Date(start.getFullYear(), start.getMonth(), start.getDate() + 6)) : endToday
    return { start, end }
  }
  if (range === 'month' || range === 'lastMonth') {
    const offset = range === 'lastMonth' ? -1 : 0
    const start = new Date(today.getFullYear(), today.getMonth() + offset, 1)
    const end = range === 'lastMonth' ? endOfDay(new Date(today.getFullYear(), today.getMonth(), 0)) : endToday
    return { start, end }
  }
  if (range === 'year' || range === 'lastYear') {
    const year = today.getFullYear() - (range === 'lastYear' ? 1 : 0)
    return { start: new Date(year, 0, 1), end: range === 'lastYear' ? endOfDay(new Date(year, 11, 31)) : endToday }
  }
  if (range === 'custom') {
    return {
      start: customStart ? startOfDay(`${customStart}T00:00:00`) : today,
      end: customEnd ? endOfDay(`${customEnd}T00:00:00`) : endToday,
    }
  }
  const earliest = dates.length ? new Date(Math.min(...dates.map((date) => new Date(date).getTime()))) : today
  return { start: startOfDay(earliest), end: endToday }
}

function formatHoursMinutes(seconds = 0) {
  const minutes = Math.floor(Math.max(0, seconds) / 60)
  return `${Math.floor(minutes / 60)}:${String(minutes % 60).padStart(2, '0')}`
}

function getBarPeriod(period, customWeek, customMonth) {
  const today = startOfDay(new Date())
  if (period === 'week' || period === 'lastWeek' || period === 'customWeek') {
    if (period === 'customWeek') {
      const match = /^(\d{4})-W(\d{2})$/.exec(customWeek)
      if (!match) return null
      const year = Number(match[1])
      const week = Number(match[2])
      const januaryFourth = new Date(year, 0, 4)
      const firstMonday = new Date(januaryFourth)
      firstMonday.setDate(januaryFourth.getDate() - ((januaryFourth.getDay() + 6) % 7))
      const start = new Date(firstMonday)
      start.setDate(firstMonday.getDate() + (week - 1) * 7)
      return { start, end: endOfDay(new Date(start.getFullYear(), start.getMonth(), start.getDate() + 6)) }
    }
    const start = new Date(today)
    start.setDate(start.getDate() - ((start.getDay() + 6) % 7) - (period === 'lastWeek' ? 7 : 0))
    return { start, end: endOfDay(new Date(start.getFullYear(), start.getMonth(), start.getDate() + 6)) }
  }
  if (period === 'month' || period === 'lastMonth' || period === 'customMonth') {
    if (period === 'customMonth') {
      const match = /^(\d{4})-(\d{2})$/.exec(customMonth)
      if (!match) return null
      const year = Number(match[1])
      const month = Number(match[2]) - 1
      return { start: new Date(year, month, 1), end: endOfDay(new Date(year, month + 1, 0)) }
    }
    const offset = period === 'lastMonth' ? -1 : 0
    return {
      start: new Date(today.getFullYear(), today.getMonth() + offset, 1),
      end: endOfDay(new Date(today.getFullYear(), today.getMonth() + offset + 1, 0)),
    }
  }
  return {
    start: new Date(today.getFullYear(), 0, 1),
    end: endOfDay(new Date(today.getFullYear(), 11, 31)),
  }
}

function createBarBuckets(period, grouping, range) {
  if (!range) return []
  const buckets = []
  const isWeekPeriod = ['week', 'lastWeek', 'customWeek'].includes(period)
  const isMonthPeriod = ['month', 'lastMonth', 'customMonth'].includes(period)
  if (isWeekPeriod || (isMonthPeriod && grouping === 'days')) {
    for (let date = new Date(range.start); date <= range.end; date.setDate(date.getDate() + 1)) {
      const start = new Date(date)
      buckets.push({
        label: isWeekPeriod ? new Intl.DateTimeFormat('en', { weekday: 'short' }).format(start) : String(start.getDate()),
        start,
        end: endOfDay(start),
      })
    }
    return buckets
  }
  if (isMonthPeriod) {
    for (let week = 0; week < 4; week += 1) {
      const start = new Date(range.start.getFullYear(), range.start.getMonth(), week * 7 + 1)
      const end = week === 3 ? range.end : endOfDay(new Date(range.start.getFullYear(), range.start.getMonth(), week * 7 + 7))
      buckets.push({ label: `Week ${week + 1}`, start, end })
    }
    return buckets
  }
  if (grouping === 'months') {
    for (let month = 0; month < 12; month += 1) {
      const start = new Date(range.start.getFullYear(), month, 1)
      buckets.push({
        label: new Intl.DateTimeFormat('en', { month: 'short' }).format(start),
        start,
        end: endOfDay(new Date(range.start.getFullYear(), month + 1, 0)),
      })
    }
    return buckets
  }
  let week = 1
  for (let start = new Date(range.start); start <= range.end; start.setDate(start.getDate() + 7)) {
    const bucketStart = new Date(start)
    const bucketEnd = endOfDay(new Date(Math.min(
      new Date(bucketStart.getFullYear(), bucketStart.getMonth(), bucketStart.getDate() + 6).getTime(),
      range.end.getTime(),
    )))
    buckets.push({ label: `W${week}`, start: bucketStart, end: bucketEnd })
    week += 1
  }
  return buckets
}

function Statistics({ projects, entries, runningTimer, elapsed }) {
  const [rangeKey, setRangeKey] = useState('all')
  const [customStart, setCustomStart] = useState('')
  const [customEnd, setCustomEnd] = useState('')
  const [activeIndex, setActiveIndex] = useState(null)
  const [barPeriod, setBarPeriod] = useState('week')
  const [barGrouping, setBarGrouping] = useState('days')
  const [activeBarIndex, setActiveBarIndex] = useState(null)
  const [customBarWeek, setCustomBarWeek] = useState('')
  const [customBarMonth, setCustomBarMonth] = useState('')
  const [linePeriod, setLinePeriod] = useState('week')
  const [lineGrouping, setLineGrouping] = useState('days')
  const [customLineWeek, setCustomLineWeek] = useState('')
  const [customLineMonth, setCustomLineMonth] = useState('')
  const allDates = [
    ...entries.map((entry) => entry.startedAtUtc),
    ...projects.flatMap((project) => (project.manualTimeAdjustments || []).map((adjustment) => adjustment.addedAtUtc)),
  ]
  const range = getStatisticRange(rangeKey, customStart, customEnd, allDates)
  const invalidCustomRange = rangeKey === 'custom' && (!customStart || !customEnd || range.start > range.end)
  const chartData = projects.map((project) => {
    const trackedSeconds = entries
      .filter((entry) => entry.projectId === project.id)
      .reduce((sum, entry) => sum + entrySecondsInRange(entry, range.start, range.end, runningTimer, elapsed), 0)
    const manualSeconds = (project.manualTimeAdjustments || [])
      .filter((adjustment) => new Date(adjustment.addedAtUtc) >= range.start && new Date(adjustment.addedAtUtc) <= range.end)
      .reduce((sum, adjustment) => sum + adjustment.durationSeconds, 0)
    return { id: project.id, name: project.name, color: project.color, seconds: trackedSeconds + manualSeconds }
  }).filter((project) => project.seconds > 0 && !invalidCustomRange)
  const totalSeconds = chartData.reduce((sum, project) => sum + project.seconds, 0)
  const mostWorked = chartData.reduce((largest, project) => !largest || project.seconds > largest.seconds ? project : largest, null)
  const days = Math.max(1, Math.floor((endOfDay(range.end) - startOfDay(range.start)) / 86400000) + 1)
  const averageDaily = totalSeconds / days
  const manualItems = projects.flatMap((project) => (project.manualTimeAdjustments || []).map((adjustment) => ({
      projectId: project.id,
      date: new Date(adjustment.addedAtUtc),
      seconds: adjustment.durationSeconds,
    })))
  const secondsForPeriod = (periodStart, periodEnd, projectId = null) => {
    const tracked = entries
      .filter((entry) => projectId === null || entry.projectId === projectId)
      .reduce((sum, entry) => sum + entrySecondsInRange(entry, periodStart, periodEnd, runningTimer, elapsed), 0)
    const manual = manualItems
      .filter((item) => (projectId === null || item.projectId === projectId) && item.date >= periodStart && item.date <= periodEnd)
      .reduce((sum, item) => sum + item.seconds, 0)
    return tracked + manual
  }
  const barRange = getBarPeriod(barPeriod, customBarWeek, customBarMonth)
  const buckets = createBarBuckets(barPeriod, barGrouping, barRange)
  const barData = buckets.map((bucket) => ({
    ...bucket,
    seconds: secondsForPeriod(bucket.start, bucket.end),
  }))
  const barProjectData = projects.map((project) => ({
    ...project,
    seconds: barRange ? secondsForPeriod(barRange.start, barRange.end, project.id) : 0,
  })).filter((project) => project.seconds > 0)
  const barTotalSeconds = barProjectData.reduce((sum, project) => sum + project.seconds, 0)
  const barMostWorked = barProjectData.reduce((largest, project) => !largest || project.seconds > largest.seconds ? project : largest, null)
  const averageEnd = barRange ? new Date(Math.min(barRange.end.getTime(), endOfDay(new Date()).getTime())) : new Date()
  const barDays = barRange ? Math.max(1, Math.floor((averageEnd - barRange.start) / 86400000) + 1) : 1
  const barAverageDaily = barTotalSeconds / barDays
  const lineRange = getBarPeriod(linePeriod, customLineWeek, customLineMonth)
  const lineBuckets = createBarBuckets(linePeriod, lineGrouping, lineRange)
  const lineData = lineBuckets.map((bucket) => ({
    ...bucket,
    seconds: secondsForPeriod(bucket.start, bucket.end),
  }))
  const lineProjectData = projects.map((project) => ({
    ...project,
    seconds: lineRange ? secondsForPeriod(lineRange.start, lineRange.end, project.id) : 0,
  })).filter((project) => project.seconds > 0)
  const lineTotalSeconds = lineProjectData.reduce((sum, project) => sum + project.seconds, 0)
  const lineMostWorked = lineProjectData.reduce((largest, project) => !largest || project.seconds > largest.seconds ? project : largest, null)
  const lineAverageEnd = lineRange ? new Date(Math.min(lineRange.end.getTime(), endOfDay(new Date()).getTime())) : new Date()
  const lineDays = lineRange ? Math.max(1, Math.floor((lineAverageEnd - lineRange.start) / 86400000) + 1) : 1
  const lineAverageDaily = lineTotalSeconds / lineDays

  const percentageLabel = ({ cx, cy, midAngle, outerRadius, percent }) => {
    const radius = outerRadius + 25
    const angle = -midAngle * Math.PI / 180
    return (
      <text x={cx + radius * Math.cos(angle)} y={cy + radius * Math.sin(angle)} fill="#cfc5d4" textAnchor={Math.cos(angle) >= 0 ? 'start' : 'end'} dominantBaseline="central" className="chart-percentage">
        {`${Math.round(percent * 100)}%`}
      </text>
    )
  }

  return (
    <div className="page-content statistics-page">
      <div className="page-title"><div><span className="eyebrow">Insights</span><h1>Statistics</h1><p>See how your focus is divided across projects.</p></div></div>
      <div className="statistics-summary">
        <div><span>Total time</span><strong>{formatHoursMinutes(totalSeconds)}</strong><small>hours and minutes</small></div>
        <div><span>Most worked on project</span><strong>{mostWorked?.name || 'No data'}</strong><small>{mostWorked ? formatHoursMinutes(mostWorked.seconds) : '0:00'} tracked</small></div>
        <div><span>Average daily</span><strong>{formatHoursMinutes(averageDaily)}</strong><small>per calendar day</small></div>
      </div>
      <section className="statistics-layout">
        <div className="statistics-chart panel">
          <div className="panel-header"><div><h2>Project time</h2><p>Share of total focused time</p></div></div>
          {chartData.length ? (
            <>
              <div className="doughnut-wrap">
                <ResponsiveContainer width="100%" height="100%">
                  <PieChart>
                    <Pie data={chartData} dataKey="seconds" nameKey="name" cx="50%" cy="50%" innerRadius="53%" outerRadius="72%" paddingAngle={2} stroke="none" labelLine={false} label={percentageLabel} isAnimationActive={false} onMouseLeave={() => setActiveIndex(null)}>
                      {chartData.map((project, index) => <Cell key={project.id} fill={project.color} onMouseEnter={() => setActiveIndex(index)} className={activeIndex === index ? 'active-chart-segment' : ''} />)}
                    </Pie>
                    <Tooltip content={({ active, payload }) => active && payload?.length ? <div className="chart-tooltip"><span className="project-dot" style={{ background: payload[0].payload.color }} /><div><strong>{payload[0].name}</strong><small>{formatHoursMinutes(payload[0].value)} hours</small></div></div> : null} />
                  </PieChart>
                </ResponsiveContainer>
                <div className="chart-center"><strong>{formatHoursMinutes(totalSeconds)}</strong><span>total hours</span></div>
              </div>
              <div className="chart-legend">
                {chartData.map((project) => <div key={project.id}><span className="project-dot" style={{ background: project.color }} /><strong>{project.name}</strong><time>{formatHoursMinutes(project.seconds)}</time></div>)}
              </div>
            </>
          ) : <div className="empty-state statistics-empty"><div className="empty-icon"><Icon name="statistics" size={28} /></div><h3>No time in this period</h3><p>Choose another range or track some project time.</p></div>}
        </div>
        <aside className="statistics-filters panel">
          <div className="panel-header"><div><h2>Time period</h2><p>Filter the chart</p></div></div>
          <div className="filter-options">
            {statisticRanges.map(([key, label]) => <button key={key} className={rangeKey === key ? 'active' : ''} onClick={() => setRangeKey(key)}>{label}</button>)}
          </div>
          {rangeKey === 'custom' && (
            <div className="custom-date-fields">
              <label>Start date<input type="date" value={customStart} max={customEnd || undefined} onChange={(event) => setCustomStart(event.target.value)} /></label>
              <label>End date<input type="date" value={customEnd} min={customStart || undefined} onChange={(event) => setCustomEnd(event.target.value)} /></label>
              {invalidCustomRange && <small>Select a valid start and end date.</small>}
            </div>
          )}
        </aside>
      </section>
      <div className="statistics-section-heading">
        <span className="eyebrow">Timeline</span>
        <h2>Time over time</h2>
        <p>Compare your focused hours across the selected calendar period.</p>
      </div>
      <div className="statistics-summary">
        <div><span>Total time</span><strong>{formatHoursMinutes(barTotalSeconds)}</strong><small>hours and minutes</small></div>
        <div><span>Most worked on project</span><strong>{barMostWorked?.name || 'No data'}</strong><small>{barMostWorked ? formatHoursMinutes(barMostWorked.seconds) : '0:00'} tracked</small></div>
        <div><span>Average daily</span><strong>{formatHoursMinutes(barAverageDaily)}</strong><small>per elapsed calendar day</small></div>
      </div>
      <section className="statistics-layout bar-statistics-layout">
        <div className="statistics-chart bar-chart-panel panel">
          <div className="panel-header"><div><h2>Tracked hours</h2><p>{barRange ? `${barGrouping === 'days' ? 'Daily' : barGrouping === 'weeks' ? 'Weekly' : 'Monthly'} view · ${new Intl.DateTimeFormat('en', { month: 'short', day: 'numeric', year: 'numeric' }).format(barRange.start)} – ${new Intl.DateTimeFormat('en', { month: 'short', day: 'numeric', year: 'numeric' }).format(barRange.end)}` : 'Choose a custom period to view your time'}</p></div></div>
          {barRange ? <div className={`bar-chart-wrap ${barData.length > 31 ? 'dense' : ''}`}>
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={barData} margin={{ top: 24, right: 24, left: 3, bottom: barData.length > 31 ? 22 : 4 }} onMouseLeave={() => setActiveBarIndex(null)}>
                <CartesianGrid vertical={false} stroke="rgba(255,255,255,.06)" />
                <XAxis dataKey="label" interval={barData.length > 31 ? 3 : 0} angle={barData.length > 31 ? -35 : 0} textAnchor={barData.length > 31 ? 'end' : 'middle'} tick={{ fill: '#827789', fontSize: 9 }} axisLine={false} tickLine={false} />
                <YAxis tickFormatter={(value) => `${Math.round(value / 3600)}h`} tick={{ fill: '#827789', fontSize: 9 }} axisLine={false} tickLine={false} width={38} />
                <Tooltip cursor={{ fill: 'rgba(237,79,194,.035)' }} content={({ active, payload, label }) => active && payload?.length ? <div className="bar-tooltip"><span>{label}</span><strong>{formatHoursMinutes(payload[0].value)} hours</strong></div> : null} />
                <Bar dataKey="seconds" name="Tracked time" fill="#d946ef" radius={[5, 5, 2, 2]} maxBarSize={36} isAnimationActive={false}>
                  {barData.map((bucket, index) => <Cell key={`${bucket.label}-${index}`} fill={activeBarIndex === index ? '#f77be9' : '#c43cd5'} className={activeBarIndex === index ? 'active-bar' : ''} onMouseEnter={() => setActiveBarIndex(index)} />)}
                </Bar>
              </BarChart>
            </ResponsiveContainer>
          </div> : <div className="empty-state statistics-empty"><div className="empty-icon"><Icon name="statistics" size={28} /></div><h3>Choose a period</h3><p>Select a week or month to build this chart.</p></div>}
        </div>
        <aside className="statistics-filters bar-filters panel">
          <div className="panel-header"><div><h2>Chart period</h2><p>Choose the timeline</p></div></div>
          <div className="bar-filter-group">
            <span>Period</span>
            <div className="bar-period-options">{[['week', 'Week'], ['lastWeek', 'Last week'], ['customWeek', 'Custom week'], ['month', 'Month'], ['lastMonth', 'Last month'], ['customMonth', 'Custom month'], ['year', 'Year']].map(([key, label]) => <button key={key} className={barPeriod === key ? 'active' : ''} onClick={() => { setBarPeriod(key); setBarGrouping(key === 'year' ? 'months' : 'days') }}>{label}</button>)}</div>
          </div>
          {barPeriod === 'customWeek' && <div className="bar-custom-period"><label>Choose week<input type="week" value={customBarWeek} onChange={(event) => setCustomBarWeek(event.target.value)} /></label></div>}
          {barPeriod === 'customMonth' && <div className="bar-custom-period"><label>Choose month<input type="month" value={customBarMonth} onChange={(event) => setCustomBarMonth(event.target.value)} /></label></div>}
          {['month', 'lastMonth', 'customMonth'].includes(barPeriod) && <div className="bar-filter-group"><span>Display by</span><div><button className={barGrouping === 'days' ? 'active' : ''} onClick={() => setBarGrouping('days')}>Days</button><button className={barGrouping === 'weeks' ? 'active' : ''} onClick={() => setBarGrouping('weeks')}>4 weeks</button></div></div>}
          {barPeriod === 'year' && <div className="bar-filter-group"><span>Display by</span><div><button className={barGrouping === 'months' ? 'active' : ''} onClick={() => setBarGrouping('months')}>Months</button><button className={barGrouping === 'weeks' ? 'active' : ''} onClick={() => setBarGrouping('weeks')}>Weeks</button></div></div>}
          <div className="bar-filter-note"><Icon name="statistics" size={17} /><span>Bars include tracked sessions and manual time.</span></div>
        </aside>
      </section>
      <div className="statistics-section-heading">
        <span className="eyebrow">Trend</span>
        <h2>Focus trend</h2>
        <p>Follow how your tracked hours change throughout a selected period.</p>
      </div>
      <div className="statistics-summary">
        <div><span>Total time</span><strong>{formatHoursMinutes(lineTotalSeconds)}</strong><small>hours and minutes</small></div>
        <div><span>Most worked on project</span><strong>{lineMostWorked?.name || 'No data'}</strong><small>{lineMostWorked ? formatHoursMinutes(lineMostWorked.seconds) : '0:00'} tracked</small></div>
        <div><span>Average daily</span><strong>{formatHoursMinutes(lineAverageDaily)}</strong><small>per elapsed calendar day</small></div>
      </div>
      <section className="statistics-layout line-statistics-layout">
        <div className="statistics-chart line-chart-panel panel">
          <div className="panel-header"><div><h2>Hours trend</h2><p>{lineRange ? `${lineGrouping === 'days' ? 'Daily' : lineGrouping === 'weeks' ? 'Weekly' : 'Monthly'} view · ${new Intl.DateTimeFormat('en', { month: 'short', day: 'numeric', year: 'numeric' }).format(lineRange.start)} – ${new Intl.DateTimeFormat('en', { month: 'short', day: 'numeric', year: 'numeric' }).format(lineRange.end)}` : 'Choose a custom period to view your time'}</p></div></div>
          {lineRange ? <div className={`line-chart-wrap ${lineData.length > 31 ? 'dense' : ''}`}>
            <ResponsiveContainer width="100%" height="100%">
              <LineChart data={lineData} margin={{ top: 29, right: 28, left: 3, bottom: lineData.length > 31 ? 22 : 4 }}>
                <defs>
                  <linearGradient id="lineGradient" x1="0" y1="0" x2="1" y2="0"><stop offset="0%" stopColor="#9c5cff" /><stop offset="100%" stopColor="#ed4fc2" /></linearGradient>
                </defs>
                <CartesianGrid vertical={false} stroke="rgba(255,255,255,.06)" />
                <XAxis dataKey="label" interval={lineData.length > 31 ? 3 : 0} angle={lineData.length > 31 ? -35 : 0} textAnchor={lineData.length > 31 ? 'end' : 'middle'} tick={{ fill: '#827789', fontSize: 9 }} axisLine={false} tickLine={false} />
                <YAxis tickFormatter={(value) => `${Math.round(value / 3600)}h`} tick={{ fill: '#827789', fontSize: 9 }} axisLine={false} tickLine={false} width={38} />
                <Tooltip cursor={{ stroke: 'rgba(237,79,194,.25)', strokeDasharray: '3 4' }} content={({ active, payload, label }) => active && payload?.length ? <div className="bar-tooltip line-tooltip"><span>{label}</span><strong>{formatHoursMinutes(payload[0].value)} hours</strong></div> : null} />
                <Line type="monotone" dataKey="seconds" name="Tracked time" stroke="url(#lineGradient)" strokeWidth={3} dot={{ r: 4, fill: '#c94bdc', stroke: '#18101e', strokeWidth: 2 }} activeDot={{ r: 7, fill: '#fa85e9', stroke: '#fff', strokeWidth: 2, className: 'active-line-dot' }} isAnimationActive={false} />
              </LineChart>
            </ResponsiveContainer>
          </div> : <div className="empty-state statistics-empty"><div className="empty-icon"><Icon name="statistics" size={28} /></div><h3>Choose a period</h3><p>Select a week or month to build this chart.</p></div>}
        </div>
        <aside className="statistics-filters line-filters panel">
          <div className="panel-header"><div><h2>Chart period</h2><p>Choose the timeline</p></div></div>
          <div className="bar-filter-group">
            <span>Period</span>
            <div className="bar-period-options">{[['week', 'Week'], ['lastWeek', 'Last week'], ['customWeek', 'Custom week'], ['month', 'Month'], ['lastMonth', 'Last month'], ['customMonth', 'Custom month'], ['year', 'Year']].map(([key, label]) => <button key={key} className={linePeriod === key ? 'active' : ''} onClick={() => { setLinePeriod(key); setLineGrouping(key === 'year' ? 'months' : 'days') }}>{label}</button>)}</div>
          </div>
          {linePeriod === 'customWeek' && <div className="bar-custom-period"><label>Choose week<input type="week" value={customLineWeek} onChange={(event) => setCustomLineWeek(event.target.value)} /></label></div>}
          {linePeriod === 'customMonth' && <div className="bar-custom-period"><label>Choose month<input type="month" value={customLineMonth} onChange={(event) => setCustomLineMonth(event.target.value)} /></label></div>}
          {['month', 'lastMonth', 'customMonth'].includes(linePeriod) && <div className="bar-filter-group"><span>Display by</span><div><button className={lineGrouping === 'days' ? 'active' : ''} onClick={() => setLineGrouping('days')}>Days</button><button className={lineGrouping === 'weeks' ? 'active' : ''} onClick={() => setLineGrouping('weeks')}>4 weeks</button></div></div>}
          {linePeriod === 'year' && <div className="bar-filter-group"><span>Display by</span><div><button className={lineGrouping === 'months' ? 'active' : ''} onClick={() => setLineGrouping('months')}>Months</button><button className={lineGrouping === 'weeks' ? 'active' : ''} onClick={() => setLineGrouping('weeks')}>Weeks</button></div></div>}
          <div className="bar-filter-note"><Icon name="statistics" size={17} /><span>Each point includes tracked sessions and manual time.</span></div>
        </aside>
      </section>
    </div>
  )
}

function App() {
  const [authenticated, setAuthenticated] = useState(false)
  const [authenticationChecked, setAuthenticationChecked] = useState(false)
  const [username, setUsername] = useState('')
  const [page, setPage] = useState('dashboard')
  const [projects, setProjects] = useState([])
  const [entries, setEntries] = useState([])
  const [legacyProjects, setLegacyProjects] = useState({ Clockify: [], TogglTrack: [] })
  const [runningTimer, setRunningTimer] = useState(null)
  const [elapsed, setElapsed] = useState(0)
  const timerBaselineRef = useRef({ elapsedSeconds: 0, synchronizedAt: 0 })
  const [modal, setModal] = useState(null)
  const [loading, setLoading] = useState(false)
  const [notice, setNotice] = useState('')
  const [actionEntryId, setActionEntryId] = useState(null)

  const synchronizeElapsed = useCallback((elapsedSeconds = 0) => {
    const normalizedElapsed = Math.max(0, Math.floor(elapsedSeconds))
    timerBaselineRef.current = {
      elapsedSeconds: normalizedElapsed,
      synchronizedAt: Date.now(),
    }
    setElapsed(normalizedElapsed)
  }, [])

  const clearSession = useCallback(() => {
    accessToken = null
    setAuthenticated(false)
    setUsername('')
    setProjects([])
    setEntries([])
    setLegacyProjects({ Clockify: [], TogglTrack: [] })
    setRunningTimer(null)
  }, [])

  const logout = useCallback(async () => {
    try {
      await apiRequest('/api/auth/logout', {
        method: 'POST',
        skipAuthRefresh: true,
      })
    } catch {
      // Local session state must still be cleared if the token already expired.
    }
    clearSession()
  }, [clearSession])

  const loadData = useCallback(async (signal) => {
    if (!authenticated) return
    setLoading(true)
    try {
      const [projectResponse, entryResponse, clockifyResponse, togglResponse] = await Promise.all([
        apiRequest('/api/projects', { signal }),
        apiRequest('/api/projects/time-entries', { signal }),
        apiRequest('/api/legacy-projects/Clockify', { signal }),
        apiRequest('/api/legacy-projects/TogglTrack', { signal }),
      ])
      if (signal?.aborted) return
      setProjects(projectResponse.data || [])
      setEntries(entryResponse.data || [])
      setLegacyProjects({ Clockify: clockifyResponse.data || [], TogglTrack: togglResponse.data || [] })
      try {
        const timerResponse = await apiRequest('/api/projects/timer/running', { signal })
        if (signal?.aborted) return
        setRunningTimer(timerResponse.data)
        synchronizeElapsed(timerResponse.data.elapsedSeconds)
      } catch (error) {
        if (error.name === 'AbortError') return
        if (error.status === 401) throw error
        setRunningTimer(null)
      }
    } catch (error) {
      if (error.name === 'AbortError') return
      if (error.status === 401 || error.status === 403) clearSession()
      else setNotice(error.message)
    } finally {
      if (!signal?.aborted) setLoading(false)
    }
  }, [authenticated, clearSession, synchronizeElapsed])

  useEffect(() => {
    const controller = new AbortController()
    async function checkAuthentication() {
      try {
        const session = await refreshAccessToken()
        if (controller.signal.aborted) return
        setUsername(session.username || '')
        setAuthenticated(true)
      } catch (error) {
        if (error.name !== 'AbortError' && error.status !== 401 && error.status !== 403)
          setNotice(error.message)
      } finally {
        if (!controller.signal.aborted) setAuthenticationChecked(true)
      }
    }
    void checkAuthentication()
    return () => controller.abort()
  }, [])

  useEffect(() => {
    if (!authenticationChecked || !authenticated) return undefined
    const controller = new AbortController()
    const timeout = window.setTimeout(() => void loadData(controller.signal), 0)
    return () => {
      window.clearTimeout(timeout)
      controller.abort()
    }
  }, [authenticationChecked, authenticated, loadData])
  useEffect(() => {
    if (!runningTimer) return undefined

    const updateElapsed = () => {
      const { elapsedSeconds, synchronizedAt } = timerBaselineRef.current
      const secondsSinceSynchronization = Math.max(
        0,
        Math.floor((Date.now() - synchronizedAt) / 1000),
      )
      setElapsed(elapsedSeconds + secondsSinceSynchronization)
    }

    const handleVisibilityChange = () => {
      if (document.visibilityState === 'visible') updateElapsed()
    }

    updateElapsed()
    const interval = window.setInterval(updateElapsed, 1000)
    document.addEventListener('visibilitychange', handleVisibilityChange)

    return () => {
      window.clearInterval(interval)
      document.removeEventListener('visibilitychange', handleVisibilityChange)
    }
  }, [runningTimer])

  async function handleLogin(data) {
    try {
      if (!data?.token || typeof data.token !== 'string')
        throw new Error('The login response did not include a JWT. Redeploy the Railway API with the latest authentication changes.')
      accessToken = data.token
      setUsername(data.username || '')
      setAuthenticated(true)
      setAuthenticationChecked(true)
    } catch (error) {
      clearSession()
      throw error
    }
  }

  function projectSaved(project) {
    setProjects((current) => {
      const exists = current.some((item) => item.id === project.id)
      return (exists ? current.map((item) => item.id === project.id ? project : item) : [...current, project]).sort((a, b) => a.name.localeCompare(b.name))
    })
  }

  function legacyProjectSaved(project) {
    setLegacyProjects((current) => ({
      ...current,
      [project.provider]: [...(current[project.provider] || []).filter((item) => item.id !== project.id), project]
        .sort((a, b) => a.name.localeCompare(b.name)),
    }))
  }

  async function stopTimer() {
    setActionEntryId(runningTimer?.timeEntryId || null)
    try {
      await apiRequest('/api/projects/timer/stop', { method: 'POST' })
      setRunningTimer(null)
      synchronizeElapsed(0)
      await loadData()
    } catch (error) {
      setNotice(error.message)
    } finally {
      setActionEntryId(null)
    }
  }

  async function resumeTimer(timeEntryId) {
    setActionEntryId(timeEntryId)
    try {
      const response = await apiRequest(`/api/projects/time-entries/${timeEntryId}/resume`, { method: 'POST' })
      setRunningTimer(response.data)
      synchronizeElapsed(response.data.elapsedSeconds)
      await loadData()
    } catch (error) {
      setNotice(error.message)
    } finally {
      setActionEntryId(null)
    }
  }

  async function deleteTimeEntry(timeEntryId) {
    setActionEntryId(timeEntryId)
    try {
      await apiRequest(`/api/projects/time-entries/${timeEntryId}`, { method: 'DELETE' })
      setModal(null)
      await loadData()
    } catch (error) {
      setNotice(error.message)
    } finally {
      setActionEntryId(null)
    }
  }

  async function deleteProject(projectId) {
    try {
      await apiRequest(`/api/projects/${projectId}`, { method: 'DELETE' })
      setModal(null)
      await loadData()
    } catch (error) {
      setNotice(error.message)
    }
  }

  async function deleteLegacyProject(project) {
    try {
      await apiRequest(`/api/legacy-projects/${project.id}`, { method: 'DELETE' })
      setLegacyProjects((current) => ({
        ...current,
        [project.provider]: (current[project.provider] || []).filter((item) => item.id !== project.id),
      }))
      setModal(null)
    } catch (error) {
      setNotice(error.message)
    }
  }

  return (
    <div className="app-shell">
      <Sidebar page={page} setPage={setPage} authenticated={authenticated} onLogout={logout} />
      <div className="app-main">
        <header className="topbar">
          <div className="wordmark">Time<span>Tracker</span></div>
          {authenticated && (
            <div className="header-actions">
              {runningTimer ? (
                <div className="running-pill">
                  <span className="project-dot" style={{ background: runningTimer.projectColor }} />
                  <div><small>{runningTimer.projectName}</small><strong>{formatDuration(elapsed)}</strong></div>
                  <button onClick={stopTimer} title="Stop timer"><Icon name="stop" size={18} /></button>
                </div>
              ) : <button className="timer-button" onClick={() => setModal({ type: 'timer' })}><Icon name="play" size={16} />Start timer</button>}
              <div className="user-chip"><span>{username.slice(0, 1).toUpperCase()}</span><div><small>Signed in as</small><strong>{username}</strong></div></div>
            </div>
          )}
        </header>
        {!authenticationChecked ? <div className="loading-screen"><div className="loader" /><span>Checking your session…</span></div> : !authenticated ? <Login onLogin={handleLogin} /> : loading && projects.length === 0 ? <div className="loading-screen"><div className="loader" /><span>Loading your workspace…</span></div> : page === 'dashboard' ? <Dashboard entries={entries} projects={projects} runningTimer={runningTimer} elapsed={elapsed} onResume={resumeTimer} onStop={stopTimer} onDelete={(entry) => setModal({ type: 'delete-entry', entry })} actionEntryId={actionEntryId} /> : page === 'statistics' ? <Statistics projects={projects} entries={entries} runningTimer={runningTimer} elapsed={elapsed} /> : page === 'legacy' ? <Legacy legacyProjects={legacyProjects} onCreate={(provider) => setModal({ type: 'legacy-project', provider })} onEdit={(project) => setModal({ type: 'legacy-project', provider: project.provider, project })} onDelete={(project) => setModal({ type: 'delete-legacy-project', project })} onAddTime={(project) => setModal({ type: 'legacy-time', project })} /> : <Projects projects={projects} onCreate={() => setModal({ type: 'project' })} onEdit={(project) => setModal({ type: 'project', project })} onDelete={(project) => setModal({ type: 'delete-project', project })} onAddTime={(project) => setModal({ type: 'manual-time', project })} />}
      </div>
      {modal?.type === 'project' && <ProjectFormModal project={modal.project} onClose={() => setModal(null)} onSaved={projectSaved} />}
      {modal?.type === 'timer' && <TimerModal projects={projects} onClose={() => setModal(null)} onStarted={(timer) => { setRunningTimer(timer); synchronizeElapsed(timer.elapsedSeconds); void loadData() }} />}
      {modal?.type === 'delete-entry' && <DeleteConfirmationModal title="Delete time entry?" subtitle="This action cannot be undone." color={modal.entry.projectColor} name={modal.entry.projectName} details={`${formatDate(modal.entry.startedAtUtc)} · ${formatDuration(modal.entry.durationSeconds)}`} warning="Are you sure you want to permanently delete this time entry?" confirmLabel="Delete entry" onClose={() => setModal(null)} onConfirm={() => deleteTimeEntry(modal.entry.id)} />}
      {modal?.type === 'delete-project' && <DeleteConfirmationModal title="Delete project?" subtitle="This action cannot be undone." color={modal.project.color} name={modal.project.name} details={modal.project.description || 'No description'} warning="Are you sure? Deleting this project will also permanently delete all of its time entries." confirmLabel="Delete project" onClose={() => setModal(null)} onConfirm={() => deleteProject(modal.project.id)} />}
      {modal?.type === 'manual-time' && <ManualTimeModal project={modal.project} onClose={() => setModal(null)} onSaved={projectSaved} />}
      {modal?.type === 'legacy-project' && <LegacyProjectModal provider={modal.provider} project={modal.project} onClose={() => setModal(null)} onSaved={legacyProjectSaved} />}
      {modal?.type === 'legacy-time' && <LegacyTimeModal project={modal.project} onClose={() => setModal(null)} onSaved={legacyProjectSaved} />}
      {modal?.type === 'delete-legacy-project' && <DeleteConfirmationModal title="Delete legacy project?" subtitle="This action cannot be undone." color={modal.project.color} name={modal.project.name} details={`${modal.project.provider === 'TogglTrack' ? 'Toggle Track' : modal.project.provider} · ${formatHoursMinutes(modal.project.totalTimeSeconds)}`} warning="Are you sure? The project and its complete legacy time total will be permanently deleted." confirmLabel="Delete project" onClose={() => setModal(null)} onConfirm={() => deleteLegacyProject(modal.project)} />}
      {notice && <button className="toast" onClick={() => setNotice('')}>{notice}<Icon name="close" size={16} /></button>}
    </div>
  )
}

export default App
