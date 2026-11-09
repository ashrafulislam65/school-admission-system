import { useState } from 'react'
import Login from './components/Login'
import { clearStudentId, getStudentId, setStudentId } from './session'

export default function App() {
  const [studentId, setId] = useState<string | null>(getStudentId())

  function handleLogin(id: string) {
    setStudentId(id)
    setId(id)
  }

  function handleLogout() {
    clearStudentId()
    setId(null)
  }

  if (!studentId) return <Login onLogin={handleLogin} />

  return (
    <div>
      <header className="topbar">
        <div className="brand">School Admission</div>
        <div className="who">
          <span>
            Student ID: <strong>{studentId}</strong>
          </span>
          <button className="link" onClick={handleLogout}>
            Log out
          </button>
        </div>
      </header>
      <main className="content">
        <p className="muted">Logged in. Classes will appear here.</p>
      </main>
    </div>
  )
}