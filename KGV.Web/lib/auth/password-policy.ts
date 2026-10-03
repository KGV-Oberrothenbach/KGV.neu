export function validateNewPassword(password: string, confirmation: string): string | null {
  if (password.length < 8) return "Passwort muss mindestens 8 Zeichen haben.";
  if (!/\p{Lu}/u.test(password) || !/\p{Ll}/u.test(password)) return "Passwort braucht Groß- und Kleinbuchstaben.";
  if (!/\p{Nd}/u.test(password)) return "Passwort braucht mindestens eine Zahl.";
  if (!/[^\p{L}\p{N}]/u.test(password)) return "Passwort braucht mindestens ein Sonderzeichen.";
  if (password !== confirmation) return "Passwort und Wiederholung stimmen nicht überein.";
  return null;
}
