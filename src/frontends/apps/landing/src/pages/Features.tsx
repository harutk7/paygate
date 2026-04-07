const features = [
  {
    title: 'Payment Processing',
    description:
      'Accept credit and debit cards securely via Authorize.net. Our PCI-compliant tokenization ensures sensitive card data never touches your servers. Support for one-time charges, refunds, and partial refunds out of the box.',
    details: [
      'Authorize.net integration',
      'PCI-compliant tokenization',
      'Automatic retry on failures',
      'Support for 135+ currencies',
    ],
    icon: (
      <svg className="w-10 h-10" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M3 10h18M7 15h1m4 0h1m-7 4h12a3 3 0 003-3V8a3 3 0 00-3-3H6a3 3 0 00-3 3v8a3 3 0 003 3z" />
      </svg>
    ),
  },
  {
    title: 'API Key Management',
    description:
      'Create, rotate, and revoke API keys with granular permissions. Separate test and live environments keep your integration safe. Track usage per key and set expiration dates for enhanced security.',
    details: [
      'Test and live environments',
      'Key rotation without downtime',
      'Usage tracking per key',
      'Automatic expiration support',
    ],
    icon: (
      <svg className="w-10 h-10" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M15 7a2 2 0 012 2m4 0a6 6 0 01-7.743 5.743L11 17H9v2H7v2H4a1 1 0 01-1-1v-2.586a1 1 0 01.293-.707l5.964-5.964A6 6 0 1121 9z" />
      </svg>
    ),
  },
  {
    title: 'Webhook System',
    description:
      'Get real-time notifications for every payment event. Each webhook delivery is signed with HMAC-SHA256 so you can verify authenticity. Automatic retries with exponential backoff ensure you never miss an event.',
    details: [
      'HMAC-SHA256 signatures',
      'Automatic retry with backoff',
      'Delivery logs and debugging',
      'Filter by event type',
    ],
    icon: (
      <svg className="w-10 h-10" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M15 17h5l-1.405-1.405A2.032 2.032 0 0118 14.158V11a6.002 6.002 0 00-4-5.659V5a2 2 0 10-4 0v.341C7.67 6.165 6 8.388 6 11v3.159c0 .538-.214 1.055-.595 1.436L4 17h5m6 0v1a3 3 0 11-6 0v-1m6 0H9" />
      </svg>
    ),
  },
  {
    title: 'Real-time Analytics',
    description:
      'Track transaction volume, success rates, and revenue in real time. Visualize trends with interactive charts and export reports for your team. Set up alerts for anomalies and failed payments.',
    details: [
      'Live transaction monitoring',
      'Revenue trend analysis',
      'Success rate tracking',
      'Custom date range reports',
    ],
    icon: (
      <svg className="w-10 h-10" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M9 19v-6a2 2 0 00-2-2H5a2 2 0 00-2 2v6a2 2 0 002 2h2a2 2 0 002-2zm0 0V9a2 2 0 012-2h2a2 2 0 012 2v10m-6 0a2 2 0 002 2h2a2 2 0 002-2m0 0V5a2 2 0 012-2h2a2 2 0 012 2v14a2 2 0 01-2 2h-2a2 2 0 01-2-2z" />
      </svg>
    ),
  },
  {
    title: 'Team Management',
    description:
      'Invite team members and assign role-based access controls. Admins get full access while developers can manage API keys and view transactions. Audit logs track every action for compliance.',
    details: [
      'Role-based access control',
      'Team member invitations',
      'Activity audit logs',
      'Per-user permissions',
    ],
    icon: (
      <svg className="w-10 h-10" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z" />
      </svg>
    ),
  },
  {
    title: 'Enterprise Security',
    description:
      'SOC 2 Type II compliance, encryption at rest and in transit, and full tenant isolation. Regular penetration testing and vulnerability scanning keep your data protected at every layer.',
    details: [
      'SOC 2 Type II compliance',
      'Encryption at rest & in transit',
      'Tenant data isolation',
      'Regular penetration testing',
    ],
    icon: (
      <svg className="w-10 h-10" fill="none" stroke="currentColor" viewBox="0 0 24 24">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M9 12l2 2 4-4m5.618-4.016A11.955 11.955 0 0112 2.944a11.955 11.955 0 01-8.618 3.04A12.02 12.02 0 003 9c0 5.591 3.824 10.29 9 11.622 5.176-1.332 9-6.03 9-11.622 0-1.042-.133-2.052-.382-3.016z" />
      </svg>
    ),
  },
];

export function Features() {
  return (
    <div>
      {/* Header */}
      <section className="bg-gradient-to-br from-primary-600 to-secondary-600 py-16 sm:py-20">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h1 className="text-4xl sm:text-5xl font-bold text-white mb-4">
            Powerful Features
          </h1>
          <p className="text-lg text-primary-100 max-w-2xl mx-auto">
            Everything you need to accept and manage payments at scale, with enterprise-grade
            security and reliability.
          </p>
        </div>
      </section>

      {/* Feature Sections */}
      <section className="py-16 sm:py-20">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 space-y-20 sm:space-y-28">
          {features.map((feature, idx) => (
            <div
              key={feature.title}
              className={`flex flex-col lg:flex-row items-center gap-12 lg:gap-16 ${
                idx % 2 === 1 ? 'lg:flex-row-reverse' : ''
              }`}
            >
              {/* Text */}
              <div className="flex-1">
                <div className="inline-flex items-center justify-center w-14 h-14 bg-primary-50 text-primary-600 rounded-xl mb-6">
                  {feature.icon}
                </div>
                <h2 className="text-2xl sm:text-3xl font-bold text-gray-900 mb-4">
                  {feature.title}
                </h2>
                <p className="text-gray-600 leading-relaxed mb-6">{feature.description}</p>
                <ul className="space-y-3">
                  {feature.details.map((detail) => (
                    <li key={detail} className="flex items-center gap-3 text-sm text-gray-700">
                      <svg className="w-5 h-5 text-primary-600 shrink-0" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 13l4 4L19 7" />
                      </svg>
                      {detail}
                    </li>
                  ))}
                </ul>
              </div>

              {/* Visual placeholder */}
              <div className="flex-1 w-full">
                <div className={`rounded-2xl p-8 sm:p-12 ${
                  idx % 2 === 0 ? 'bg-gradient-to-br from-primary-50 to-secondary-50' : 'bg-gradient-to-br from-gray-50 to-primary-50'
                }`}>
                  <div className="bg-white rounded-xl shadow-sm p-6 space-y-4">
                    <div className="flex items-center gap-3 mb-4">
                      <div className="w-3 h-3 rounded-full bg-red-400" />
                      <div className="w-3 h-3 rounded-full bg-yellow-400" />
                      <div className="w-3 h-3 rounded-full bg-green-400" />
                    </div>
                    <div className="space-y-3">
                      <div className="h-3 bg-gray-100 rounded w-3/4" />
                      <div className="h-3 bg-primary-100 rounded w-1/2" />
                      <div className="h-3 bg-gray-100 rounded w-5/6" />
                      <div className="h-3 bg-primary-50 rounded w-2/3" />
                      <div className="h-3 bg-gray-100 rounded w-3/5" />
                    </div>
                  </div>
                </div>
              </div>
            </div>
          ))}
        </div>
      </section>

      {/* CTA */}
      <section className="py-16 sm:py-20 bg-gray-50">
        <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 text-center">
          <h2 className="text-3xl font-bold text-gray-900 mb-4">
            Ready to integrate?
          </h2>
          <p className="text-lg text-gray-600 mb-8 max-w-xl mx-auto">
            Start building with PayGate today. Our documentation and SDKs make integration fast
            and straightforward.
          </p>
          <div className="flex flex-col sm:flex-row justify-center gap-4">
            <a
              href="/register"
              className="bg-primary-600 text-white px-8 py-3 rounded-lg font-semibold hover:bg-primary-700 transition-colors"
            >
              Get Started Free
            </a>
            <a
              href="#"
              className="border border-gray-300 text-gray-700 px-8 py-3 rounded-lg font-semibold hover:bg-gray-50 transition-colors"
            >
              Read Documentation
            </a>
          </div>
        </div>
      </section>
    </div>
  );
}
