import { useState } from 'react'
import { api } from '../api'
import { usePolling } from '../hooks/usePolling'

async function load() {
  const [classes, holds] = await Promise.all([api.listClasses(), api.myHolds()])
  return { classes, holds }
}

export default function ClassList({ onViewApplications }: { onViewApplications: () => void }) {
  const { data, error, loading, refresh } = usePolling(load, 3000)
  const [applyingId, setApplyingId] = useState<string | null>(null)
  const [notice, setNotice] = useState('')
  const [actionError, setActionError] = useState('')

  async function apply(classId: string, className: string) {
    setApplyingId(classId)
    setNotice('')
    setActionError('')
    try {
      const hold = await api.apply(classId)
      const until = new Date(hold.expiresAt).toLocaleTimeString()
      setNotice(
        `Your seat in "${className}" is on hold. Pay the admission fee before ${until} to keep it.`,
      )
    } catch (e) {
      setActionError(e instanceof Error ? e.message : 'Could not apply. Please try again.')
    } finally {
      setApplyingId(null)
      void refresh()
    }
  }

  if (loading && !data) return <p className="muted">Loading classes...</p>

  if (!data) {
    return (
      <div className="alert error">
        {error || 'Could not load classes.'}{' '}
        <button className="link" onClick={() => void refresh()}>
          Try again
        </button>
      </div>
    )
  }

  // Student already has an active application or admission in these classes
  const mine = new Map<string, string>()
  for (const h of data.holds) {
    if (h.status === 'HELD' || h.status === 'CONFIRMED') mine.set(h.classId, h.status)
  }

  return (
    <div>
            {notice && (
        <div className="alert success">
          {notice}{' '}
          <button className="link" onClick={onViewApplications}>
            See my applications
          </button>
        </div>
      )}
      {actionError && <div className="alert error">{actionError}</div>}
      {error && (
        <div className="alert error">Could not refresh just now. Showing the last numbers we have.</div>
      )}

      {data.classes.length === 0 ? (
        <p className="muted">No classes yet. Create one from the "Create class" tab.</p>
      ) : (
        <div className="grid">
          {data.classes.map((c) => {
            const state = mine.get(c.id)
            let label = 'Apply now'
            let disabled = false
            if (state === 'CONFIRMED') {
              label = 'Admitted'
              disabled = true
            } else if (state === 'HELD') {
              label = 'Application pending'
              disabled = true
            } else if (c.availableSeats <= 0) {
              label = 'No seats left'
              disabled = true
            }

            return (
              <div className="card" key={c.id}>
                <h3>{c.name}</h3>
                <div className="stats">
                  <span>Total seats</span>
                  <strong>{c.totalSeats}</strong>
                  <span>On hold</span>
                  <strong>{c.heldSeats}</strong>
                  <span>Confirmed</span>
                  <strong>{c.confirmedSeats}</strong>
                  <span>Available</span>
                  <strong>{c.availableSeats}</strong>
                </div>
                <button
                  className="btn"
                  disabled={disabled || applyingId === c.id}
                  onClick={() => void apply(c.id, c.name)}
                >
                  {applyingId === c.id ? 'Applying...' : label}
                </button>
              </div>
            )
          })}
        </div>
      )}
    </div>
  )
}