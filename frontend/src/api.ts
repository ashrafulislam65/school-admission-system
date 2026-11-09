import { API_URL } from './config'
import { getStudentId } from './session'
import type { ClassCreated, HoldCreated, HoldInfo, HoldStatus, MyHold, SchoolClass } from './types'

export class ApiError extends Error {
  status: number

  constructor(message: string, status: number) {
    super(message)
    this.status = status
  }
}

const FALLBACK: Record<number, string> = {
  400: 'Some of the information is not valid. Please check it and try again.',
  403: 'You are not allowed to do this.',
  404: 'We could not find what you asked for. It may have been removed.',
  409: 'This action is not possible right now.',
  500: 'Something went wrong on our side. Please try again in a moment.',
}

async function request<T>(
  method: string,
  path: string,
  body?: unknown,
  withStudent = false,
): Promise<T> {
  const headers: Record<string, string> = {}
  if (body !== undefined) headers['Content-Type'] = 'application/json'
  if (withStudent) {
    const id = getStudentId()
    if (id) headers['X-Student-Id'] = id
  }

  let res: Response
  try {
    res = await fetch(`${API_URL}${path}`, {
      method,
      headers,
      body: body !== undefined ? JSON.stringify(body) : undefined,
    })
  } catch {
    throw new ApiError('Cannot reach the server. Please check your connection and try again.', 0)
  }

  if (res.status === 204) return undefined as T

  let data: unknown = null
  try {
    data = await res.json()
  } catch {
    // response had no JSON body
  }

  if (!res.ok) {
    const serverMessage =
      typeof data === 'object' && data !== null && 'error' in data
        ? String((data as { error: unknown }).error)
        : ''
    throw new ApiError(serverMessage || FALLBACK[res.status] || FALLBACK[500], res.status)
  }
  return data as T
}

const enc = encodeURIComponent

export const api = {
  listClasses: () => request<SchoolClass[]>('GET', '/classes'),
  createClass: (name: string, totalSeats: number) =>
    request<ClassCreated>('POST', '/classes', { name, totalSeats }),
  apply: (classId: string) =>
    request<HoldCreated>('POST', `/classes/${enc(classId)}/holds`, undefined, true),
  getHold: (holdId: string) => request<HoldInfo>('GET', `/holds/${enc(holdId)}`),
  confirm: (holdId: string) =>
    request<{ holdId: string; status: HoldStatus }>(
      'POST',
      `/holds/${enc(holdId)}/confirm`,
      undefined,
      true,
    ),
  cancel: (holdId: string) => request<void>('DELETE', `/holds/${enc(holdId)}`, undefined, true),
  myHolds: () => request<MyHold[]>('GET', '/me/holds', undefined, true),
}