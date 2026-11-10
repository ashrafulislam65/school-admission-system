import { useEffect, useState } from 'react'
import { api } from '../api'
import { useNow } from '../hooks/useNow'
import { usePolling } from '../hooks/usePolling'
import type { HoldStatus, MyHold } from '../types'
import { formatRemaining } from '../utils/time'
import StatusBadge, { statusHint } from './StatusBadge'

export default function MyApplications() {
  const { data, error, loading, refresh } = usePolling(api.myHolds, 3000)
  const now = useNow()
  const [busyId, setBusyId] = useState<string | null>(null)
  const [notice, setNotice] = useState('')
  const [actionError, setActionError] = useState('')

  // When a countdown reaches zero, ask the server straight away instead of waiting for the next poll
  const justExpired = (data ?? [])
    .filter((h) => h.status === 'HELD' && new Date(h.expiresAt).getTime() <= now)
    .map((h) => h.holdId)
    .join(',')

  useEffect(() => {
    if (justExpired) void refresh()
  }, [justExpired, refresh])

  async function payFee(h: MyHold) {
    setBusyId(h.holdId)
    setNotice('')
    setActionError('')
    try {
      await api.confirm(h.holdId)
      setNotice(`Fee received. Your admission in "${h.className}" is confirmed.`)
    } catch (e) {
      setActionError(e instanceof Error ? e.message : 'Could not complete the payment. Please try again.')
    } finally {
      setBusyId(null)
      void refresh()
    }
  }

  async function cancel(h: MyHold) {
    if (!window.confirm(`Cancel your application for "${h.className}"? Your seat will go to another student.`)) {
      return
    }
    setBusyId(h.holdId)
    setNotice('')
    setActionError('')
    try {
      await api.cancel(h.holdId)
      setNotice(`Your application for "${h.className}" was cancelled.`)
    } catch (e) {
      setActionError(e instanceof Error ? e.message : 'Could not cancel. Please try again.')
    } finally {
      setBusyId(null)
      void refresh()
    }
  }

  if (loading && !data) return <p className="muted">Loading your applications...</p>

  if (!data) {
    return (
      <div className="alert error">
        {error || 'Could not load your applications.'}{' '}
        <button className="link" onClick={() => void refresh()}>
          Try again
        </button>
      </div>
    )
  }

  return (
    <div>
      {notice && <div className="alert success">{notice}</div>}
      {actionError && <div className="alert error">{actionError}</div>}
      {error && <div className="alert error">Could not refresh just now. Showing the last information we have.</div>}

      {data.length === 0 ? (
        <p className="muted">You have not applied to any class yet. Go to the Classes tab to apply.</p>
      ) : (
        <div className="grid">
          {data.map((h) => {
            const remaining = new Date(h.expiresAt).getTime() - now
            // Show EXPIRED the moment the timer ends, even before the next server refresh
            const status: HoldStatus = h.status === 'HELD' && remaining <= 0 ? 'EXPIRED' : h.status
            const active = status === 'HELD'
            const busy = busyId === h.holdId

            return (
              <div className="card" key={h.holdId}>
                <h3>{h.className}</h3>
                <div className="row" style={{ marginBottom: 8 }}>
                  <StatusBadge status={status} />
                  {active && (
                    <span>
                      Time left: <strong>{formatRemaining(remaining)}</strong>
                    </span>
                  )}
                </div>
                <p className="muted">{statusHint(status)}</p>
                <div className="row">
                  <button className="btn" disabled={!active || busy} onClick={() => void payFee(h)}>
                    {status === 'CONFIRMED' ? 'Fee paid' : busy ? 'Please wait...' : 'Pay admission fee'}
                  </button>
                  <button className="btn secondary" disabled={!active || busy} onClick={() => void cancel(h)}>
                    Cancel
                  </button>
                </div>
              </div>
            )
          })}
        </div>
      )}
    </div>
  )
}