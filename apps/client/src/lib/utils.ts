import { randomBytes } from "crypto";

const numChars = 32;

export function generateFriendCode(): string {
  const chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
  const bytes = randomBytes(numChars);
  let friendCode = "";
  for (let i = 0; i < numChars; i++) {
    friendCode += chars[bytes[i] % chars.length];
  }
  return friendCode;
}
