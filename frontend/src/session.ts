const KEY = 'studentId'

export function isValidStudentId(id: string): boolean {
  return /^\d{17}$/.test(id)
}

export function getStudentId(): string | null {
  try {
    const id = localStorage.getItem(KEY)
    return id && isValidStudentId(id) ? id : null
  } catch {
    return null
  }
}

export function setStudentId(id: string): void {
  try {
    localStorage.setItem(KEY, id)
  } catch {
    // storage unavailable: login still works until the page is closed
  }
}

export function clearStudentId(): void {
  try {
    localStorage.removeItem(KEY)
  } catch {
    // ignore
  }
}