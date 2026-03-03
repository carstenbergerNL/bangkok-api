import { useEffect } from 'react';
import { Link, useParams } from 'react-router-dom';
import { addToast } from '../utils/toast';

export function BillingReturn() {
  const { status } = useParams<{ status: 'success' | 'cancel' }>();

  useEffect(() => {
    if (status === 'success') {
      addToast('success', 'Subscription updated successfully.');
    } else if (status === 'cancel') {
      addToast('info', 'Checkout was cancelled.');
    }
  }, [status]);

  return (
    <div className="space-y-6">
      <div className="page-header">
        <h1>Billing</h1>
      </div>
      <div className="rounded-xl border border-gray-200 dark:border-[#2d3d5c] bg-white dark:bg-[#1e2a4a] p-8 text-center">
        {status === 'success' && (
          <>
            <p className="text-lg text-gray-900 dark:text-white">Thank you. Your subscription has been updated.</p>
            <p className="mt-2 text-sm text-gray-500 dark:text-gray-400">You can close this tab or return to Billing.</p>
          </>
        )}
        {status === 'cancel' && (
          <>
            <p className="text-lg text-gray-900 dark:text-white">Checkout was cancelled.</p>
            <p className="mt-2 text-sm text-gray-500 dark:text-gray-400">You can try again from the Billing page.</p>
          </>
        )}
        <Link to="/billing" className="btn-primary mt-6 inline-block">
          Back to Billing
        </Link>
      </div>
    </div>
  );
}
