import { act } from 'react';

/**
 * Runs a test action and waits for React to apply any state changes it
 * caused before returning.
 *
 * React's `act` only gives you a Promise to wait on when its callback is
 * async. Many test helpers (advancing fake timers, firing DOM events, the
 * first render of a Suspense tree) are not async themselves, but the state
 * updates they cause still need a tick to apply. Wrapping them this way
 * gives the test something to `await` so assertions run after React is done.
 */
// eslint-disable-next-line @typescript-eslint/require-await
export const flushAct = (action: () => void) => act(async () => action());
