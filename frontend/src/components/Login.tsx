import { useState } from 'react'
import type { FormEvent } from 'react'
import { isValidStudentId } from '../session'

interface Props {
  onLogin: (id: string) => void
}

export default function Login({ onLogin }: Props) {
  const [value, setValue] = useState('')
  const [error, setError] = useState('')

  function submit(e: FormEvent) {
    e.preventDefault()
    const id = value.trim()
    if (!isValidStudentId(id)) {
      setError('Please enter your 17-digit birth registration number. Digits only.')
      return
    }
    onLogin(id)
  }

  return (
    <div className="login">
      <form className="card" onSubmit={submit}>
        <h1>School Admission</h1>
        <p className="muted">Log in with the student's birth registration number.</p>
        {error && <div className="alert error">{error}</div>}
        <div className="field">
          <label htmlFor="studentId">Birth registration number</label>
          <input
            id="studentId"
            inputMode="numeric"
            autoComplete="off"
            maxLength={17}
            placeholder="17 digits"
            value={value}
            onChange={(e) => {
              setValue(e.target.value.replace(/\D/g, ''))
              setError('')
            }}
          />
        </div>
        <button className="btn" type="submit">
          Log in
        </button>
      </form>
    </div>
  )
}