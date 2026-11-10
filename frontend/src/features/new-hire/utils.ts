const upper = 'ABCDEFGHJKLMNPQRSTUVWXYZ';
const lower = 'abcdefghijkmnpqrstuvwxyz';
const digits = '23456789';
const symbols = '!@#$%*?-';

function pick(characters: string, random: (max: number) => number) {
  return characters[random(characters.length)];
}

function secureRandom(max: number) {
  const value = new Uint32Array(1);
  crypto.getRandomValues(value);
  return value[0] % max;
}

/**
 * A temporary password that meets the account policy (12+ characters with upper, lower, digit, and symbol).
 * Look-alike characters (0/O, 1/l/I) are left out so it can be read out loud or copied by hand.
 */
export function generateTemporaryPassword(length = 16, random: (max: number) => number = secureRandom): string {
  const all = upper + lower + digits + symbols;
  const characters = [pick(upper, random), pick(lower, random), pick(digits, random), pick(symbols, random)];
  while (characters.length < Math.max(12, length)) characters.push(pick(all, random));
  for (let i = characters.length - 1; i > 0; i--) {
    const j = random(i + 1);
    [characters[i], characters[j]] = [characters[j], characters[i]];
  }
  return characters.join('');
}

/** A readable suggestion such as "EMP-261110-4821"; HR can change it before saving. */
export function suggestEmployeeNumber(now = new Date(), random: (max: number) => number = secureRandom): string {
  const date = now.toISOString().slice(2, 10).replaceAll('-', '');
  return `EMP-${date}-${String(1000 + random(9000))}`;
}
