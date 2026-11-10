import type { HoldStatus } from '../types'

const HINTS: Record<HoldStatus, string> = {
  HELD: 'Seat is on hold. Pay the fee before time runs out.',
  CONFIRMED: 'Admission is confirmed. Fee received.',
  EXPIRED: 'Time to pay ended. The seat was released. You can apply again.',
  CANCELLED: 'This application was cancelled.',
}

export function statusHint(status: HoldStatus): string {
  return HINTS[status]
}

export default function StatusBadge({ status }: { status: HoldStatus }) {
  return <span className={`badge ${status.toLowerCase()}`}>{status}</span>
}