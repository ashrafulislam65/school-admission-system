import { useState } from 'react'
import type { FormEvent } from 'react'
import { api } from '../api'

export default function CreateClass() {
  const [name, setName] = useState('')
  const [seats, setSeats] = useState('')
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const [saving, setSaving] = useState(false)

  async function submit(e: FormEvent) {
    e.preventDefault()
    setError('')
    setSuccess('')

    const trimmed = name.trim()
    const total = Number(seats)
    if (!trimmed) {
      setError('Please enter a class name, for example "Class 6 - Morning Shift".')
      return
    }
    if (!Number.isInteger(total) || total < 1) {
      setError('Please enter the number of seats as a whole number, 1 or more.')
      return
    }

    setSaving(true)
    try {
      const created = await api.createClass(trimmed, total)
      setSuccess(`"${created.name}" was created with ${created.totalSeats} seats.`)
      setName('')
      setSeats('')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Could not create the class.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <form className="card" onSubmit={submit} style={{ maxWidth: 480 }}>
      <h2>Create a class</h2>
      <p className="muted">
        Each shift is its own class, for example "Class 6 - Morning Shift" and "Class 6 - Day Shift".
      </p>
      {error && <div className="alert error">{error}</div>}
      {success && <div className="alert success">{success}</div>}
      <div className="field">
        <label htmlFor="className">Class name</label>
        <input
          id="className"
          maxLength={200}
          placeholder="Class 6 - Morning Shift"
          value={name}
          onChange={(e) => setName(e.target.value)}
        />
      </div>
      <div className="field">
        <label htmlFor="totalSeats">Total seats</label>
        <input
          id="totalSeats"
          inputMode="numeric"
          maxLength={6}
          placeholder="120"
          value={seats}
          onChange={(e) => setSeats(e.target.value.replace(/\D/g, ''))}
        />
      </div>
      <button className="btn" type="submit" disabled={saving}>
        {saving ? 'Creating...' : 'Create class'}
      </button>
    </form>
  )
}