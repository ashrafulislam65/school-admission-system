import { useState } from 'react'
import ClassList from './components/ClassList'
import CreateClass from './components/CreateClass'
import Login from './components/Login'
import { clearStudentId, getStudentId, setStudentId } from './session'

type Tab = 'classes' | 'create'

export default function App() {
  const [studentId, setId] = useState<string | null>(getStudentId())
  const [tab, setTab] = useState<Tab>('classes')

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
        <nav className="tabs">
          <button
            className={`tab ${tab === 'classes' ? 'active' : ''}`}
            onClick={() => setTab('classes')}
          >
            Classes
          </button>
          <button
            className={`tab ${tab === 'create' ? 'active' : ''}`}
            onClick={() => setTab('create')}
          >
            Create class
          </button>
        </nav>
        {tab === 'classes' ? <ClassList /> : <CreateClass />}
      </main>
    </div>
  )
}