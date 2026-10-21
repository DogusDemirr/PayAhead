export function canUseDesignPreview(development: boolean, enabled: string | undefined) {
  return development && enabled === 'true';
}
