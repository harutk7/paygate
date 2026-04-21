import { useState, useEffect } from 'react';
import { Link } from 'react-router';
import { createApiClient, createPlansApi, type TokenStorage } from '@payment-gateway/api-client';
import type { PlanDto } from '@payment-gateway/types';

const tokenStorage: TokenStorage = {
  getAccessToken: () => localStorage.getItem('access_token'),
  setAccessToken: (token) => localStorage.setItem('access_token', token),
  getRefreshToken: () => localStorage.getItem('refresh_token'),
  setRefreshToken: (token) => localStorage.setItem('refresh_token', token),
  clear: () => {
    localStorage.removeItem('access_token');
    localStorage.removeItem('refresh_token');
  },
};

const billingClient = createApiClient('', tokenStorage);
const plansApi = createPlansApi(billingClient);

const fallbackPlans: PlanDto[] = [
  {
    id: 'starter',
    name: 'Starter',
    tier: 0,
    priceMonthly: 49,
    apiKeyLimit: 2,
    description: 'Perfect for small businesses just getting started with online payments.',
    transactionLimit: 1000,
    rateLimit: 100,
    features: [
      'Up to 1,000 transactions/mo',
      '2 API keys',
      'Email support',
      'Basic analytics',
      'Webhook notifications',
    ],
    isActive: true,
  },
  {
    id: 'business',
    name: 'Business',
    tier: 1,
    priceMonthly: 199,
    apiKeyLimit: 10,
    description: 'For growing businesses that need more power and flexibility.',
    transactionLimit: 10000,
    rateLimit: 500,
    features: [
      'Up to 10,000 transactions/mo',
      '10 API keys',
      'Priority support',
      'Advanced analytics',
      'Webhook notifications',
      'Team management',
      'Custom webhooks',
    ],
    isActive: true,
  },
  {
    id: 'enterprise',
    name: 'Enterprise',
    tier: 2,
    priceMonthly: 799,
    apiKeyLimit: 100,
    description: 'For large organizations with high-volume payment needs.',
    transactionLimit: 100000,
    rateLimit: 2000,
    features: [
      'Up to 100,000 transactions/mo',
      'Unlimited API keys',
      '24/7 dedicated support',
      'Real-time analytics',
      'Webhook notifications',
      'Team management',
      'Custom webhooks',
      'SOC 2 compliance report',
      'Dedicated account manager',
    ],
    isActive: true,
  },
];

const faqs = [
  {
    q: 'Can I change my plan later?',
    a: 'Yes, you can upgrade or downgrade your plan at any time. Changes take effect at the start of your next billing cycle.',
  },
  {
    q: 'Is there a free trial?',
    a: 'Yes, all plans come with a 14-day free trial. No credit card required to get started.',
  },
  {
    q: 'What payment methods do you support?',
    a: 'We support all major credit cards (Visa, Mastercard, American Express) and bank transfers through Authorize.net.',
  },
  {
    q: 'What happens if I exceed my transaction limit?',
    a: 'You will receive a notification when you reach 80% of your limit. Transactions above the limit are charged at a per-transaction rate.',
  },
  {
    q: 'Do you offer custom enterprise pricing?',
    a: 'Yes, for high-volume businesses we offer custom pricing and dedicated infrastructure. Contact our sales team for details.',
  },
];

export function Pricing() {
  const [plans, setPlans] = useState<PlanDto[]>(fallbackPlans);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    plansApi
      .getPlans()
      .then((data) => {
        if (data.length > 0) setPlans(data);
      })
      .catch(() => {
        // Use fallback plans
      })
      .finally(() => setLoading(false));
  }, []);

  return (
    <div>
      {/* Header */}
      <section className="bg-gradient-to-br from-primary-600 to-secondary-600 py-16 sm:py-20">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h1 className="text-4xl sm:text-5xl font-bold text-white mb-4">
            Simple, Transparent Pricing
          </h1>
          <p className="text-lg text-primary-100 max-w-2xl mx-auto">
            Choose the plan that fits your business needs. All plans include a 14-day free trial.
          </p>
        </div>
      </section>

      {/* Plan Cards */}
      <section className="py-16 sm:py-20 -mt-8">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          {loading ? (
            <div className="grid md:grid-cols-3 gap-8">
              {[1, 2, 3].map((i) => (
                <div key={i} className="bg-white dark:bg-gray-800 rounded-2xl shadow-lg p-8 animate-pulse">
                  <div className="h-6 bg-gray-200 dark:bg-gray-700 rounded w-1/3 mb-4" />
                  <div className="h-10 bg-gray-200 dark:bg-gray-700 rounded w-1/2 mb-6" />
                  <div className="space-y-3">
                    {[1, 2, 3, 4].map((j) => (
                      <div key={j} className="h-4 bg-gray-200 dark:bg-gray-700 rounded w-full" />
                    ))}
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <div className="grid md:grid-cols-3 gap-8">
              {plans.map((plan) => {
                const isPopular = plan.name === 'Business';
                return (
                  <div
                    key={plan.id}
                    className={`relative bg-white dark:bg-gray-800 rounded-2xl p-8 transition-all duration-300 hover:shadow-xl ${
                      isPopular
                        ? 'ring-2 ring-primary-600 shadow-xl scale-[1.02]'
                        : 'shadow-lg hover:scale-[1.01]'
                    }`}
                  >
                    {isPopular && (
                      <div className="absolute -top-4 left-1/2 -translate-x-1/2">
                        <span className="bg-primary-600 text-white text-sm font-semibold px-4 py-1.5 rounded-full">
                          Most Popular
                        </span>
                      </div>
                    )}
                    <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-2">{plan.name}</h3>
                    {plan.description && (
                      <p className="text-sm text-gray-500 dark:text-gray-400 mb-6">{plan.description}</p>
                    )}
                    <div className="mb-6">
                      <span className="text-4xl font-bold text-gray-900 dark:text-white">
                        ${plan.priceMonthly ?? plan.monthlyPrice}
                      </span>
                      <span className="text-gray-500 dark:text-gray-400">/mo</span>
                    </div>
                    <ul className="space-y-3 mb-8">
                      {plan.features.map((feature) => (
                        <li key={feature} className="flex items-start gap-3 text-sm text-gray-600 dark:text-gray-300">
                          <svg className="w-5 h-5 text-primary-600 shrink-0 mt-0.5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                          </svg>
                          {feature}
                        </li>
                      ))}
                    </ul>
                    <div className="text-xs text-gray-400 dark:text-gray-500 mb-4 space-y-1">
                      <p>Transaction limit: {plan.transactionLimit.toLocaleString()}/mo</p>
                      <p>Rate limit: {plan.rateLimit.toLocaleString()} req/min</p>
                    </div>
                    <Link
                      to={`/register?plan=${plan.id}`}
                      className={`block text-center py-3 rounded-lg font-semibold transition-colors ${
                        isPopular
                          ? 'bg-primary-600 text-white hover:bg-primary-700'
                          : 'bg-gray-100 dark:bg-gray-700 text-gray-900 dark:text-white hover:bg-gray-200 dark:hover:bg-gray-600'
                      }`}
                    >
                      Start Free Trial
                    </Link>
                  </div>
                );
              })}
            </div>
          )}
        </div>
      </section>

      {/* FAQ */}
      <section className="py-16 sm:py-20 bg-gray-50 dark:bg-gray-800">
        <div className="max-w-3xl mx-auto px-4 sm:px-6 lg:px-8">
          <h2 className="text-3xl font-bold text-gray-900 dark:text-white text-center mb-12">
            Frequently Asked Questions
          </h2>
          <div className="space-y-6">
            {faqs.map((faq) => (
              <div key={faq.q} className="bg-white dark:bg-gray-800 rounded-xl p-6 shadow-sm">
                <h3 className="text-lg font-semibold text-gray-900 dark:text-white mb-2">{faq.q}</h3>
                <p className="text-gray-600 dark:text-gray-300 text-sm leading-relaxed">{faq.a}</p>
              </div>
            ))}
          </div>
        </div>
      </section>
    </div>
  );
}
