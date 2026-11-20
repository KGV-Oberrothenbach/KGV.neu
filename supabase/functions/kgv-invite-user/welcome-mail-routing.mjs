// RB-05: A test email override is permissible only inside the dedicated DemoDB.
// The production project must never route invitations via SMTP_TEST_RECIPIENT.
const DEMO_PROJECT_HOSTNAME = 'ymxhszxfutjnljiuciyj.supabase.co';

/**
 * Pure function, usable by the Deno Edge Function and Node regression tests.
 * Without an override, normal email delivery remains unchanged in both DBs.
 */
export function resolveWelcomeMailRouting(supabaseUrl, originalRecipient, configuredTestRecipient) {
  const recipient = String(originalRecipient ?? '').trim();
  const testRecipient = String(configuredTestRecipient ?? '').trim();

  if (!testRecipient) {
    return { recipient, testRecipient: '' };
  }

  let projectUrl;
  try {
    projectUrl = new URL(String(supabaseUrl ?? ''));
  } catch {
    throw new Error('SMTP_TEST_RECIPIENT: Supabase-Projekt nicht eindeutig erkennbar.');
  }

  if (
    projectUrl.protocol !== 'https:' ||
    projectUrl.hostname.toLowerCase() !== DEMO_PROJECT_HOSTNAME ||
    projectUrl.port !== '' ||
    projectUrl.username !== '' ||
    projectUrl.password !== ''
  ) {
    throw new Error('SMTP_TEST_RECIPIENT ist ausschliesslich in der DemoDB zulaessig.');
  }

  return { recipient: testRecipient, testRecipient };
}
