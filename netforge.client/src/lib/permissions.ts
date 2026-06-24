// Mirrors the backend wildcard semantics (Platform/Authorization/PermissionClaims.Grants) so the UI
// gates on exactly what the API enforces. A granted "*" covers everything; "users.*" covers
// "users.read"; otherwise an exact match is required. UI gating is convenience only — the API is the
// real boundary, so never rely on this for anything but hiding controls the user can't use.
export function grants(granted: string, required: string): boolean {
  if (granted === '*') return true;
  if (granted.toLowerCase() === required.toLowerCase()) return true;
  if (!granted.endsWith('.*')) return false;
  return required.toLowerCase().startsWith(granted.slice(0, -1).toLowerCase());
}

/** True when any granted permission satisfies the requirement. */
export function hasPermission(grantedSet: readonly string[], required: string): boolean {
  return grantedSet.some((g) => grants(g, required));
}

/** True when any of the required permissions is satisfied (OR semantics). */
export function hasAnyPermission(grantedSet: readonly string[], required: readonly string[]): boolean {
  return required.some((r) => hasPermission(grantedSet, r));
}
