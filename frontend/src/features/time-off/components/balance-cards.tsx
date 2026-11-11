import { Card } from '@/components/ui/card';

import type { LeaveBalance } from '../types';
import { balanceExplanation, formatDays } from '../utils';

/** One card per kind of leave: the days available now, and where they come from. */
export function BalanceCards({ balances }: { balances: LeaveBalance[] }) {
  return (
    <div className="leave-balances">
      {balances.map((balance) => (
        <Card className="leave-balance" key={balance.leaveTypeId}>
          <span className="leave-balance__swatch" style={{ background: balance.color }} aria-hidden="true" />
          <div>
            <p>{balance.leaveTypeName}</p>
            {balance.deductsFromBalance ? (
              <strong>
                {formatDays(balance.available)} <small>available</small>
              </strong>
            ) : (
              <strong>
                {formatDays(balance.approved)} <small>taken</small>
              </strong>
            )}
            <small>{balanceExplanation(balance)}</small>
            {balance.pending > 0 ? <small>{formatDays(balance.pending)} waiting for approval</small> : null}
          </div>
        </Card>
      ))}
    </div>
  );
}
