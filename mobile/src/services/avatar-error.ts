import { isCancel } from 'axios';
import { ApiError } from './api-error';
import { t } from '../i18n/runtime';

export type AvatarStage = 'selection' | 'preparation' | 'multipart' | 'request' | 'response';
function validCode(error: ApiError) {
  if (error.status === undefined) return ['avatar_permission', 'avatar_invalid', 'avatar_too_large', 'avatar_failed'].includes(error.code ?? '');
  return error.code === 'avatar_invalid' && [400, 415].includes(error.status)
    || error.code === 'avatar_too_large' && error.status === 413
    || error.code === 'avatar_changed' && error.status === 409
    || error.code === 'avatar_unavailable' && error.status === 503
    || error.code === 'avatar_unconfirmed' && error.status >= 200 && error.status < 300;
}
export class AvatarFailure extends ApiError {
  constructor(public readonly stage: AvatarStage, error: unknown) {
    const api = error instanceof ApiError ? error : undefined;
    const code = api?.kind === 'session' || api?.status === 401 ? 'avatar_session'
      : api?.kind === 'network' || api?.kind === 'timeout' ? 'avatar_network'
      : api?.status === 413 ? 'avatar_too_large'
      : api?.status === 415 ? 'avatar_invalid'
      : api?.code && validCode(api) ? api.code
      : stage === 'response' ? 'avatar_unconfirmed' : 'avatar_failed';
    super('', api?.status, undefined, undefined, api?.kind ?? 'http', code);
  }
}
export function avatarMessage(error: ApiError): string {
  if (error.code === 'avatar_session') return t('common:m_d1d749ecce55');
  if (error.code === 'avatar_network') return t('settings:avatar.network');
  if (error.code === 'avatar_permission') return t('settings:avatar.permission');
  if (error.code === 'avatar_too_large' || error.status === 413) return t('settings:avatar.large');
  if (error.code === 'avatar_invalid') return t('settings:avatar.invalid');
  if (error.code === 'avatar_changed' && error.status === 409) return t('settings:avatar.retry');
  if (error.code === 'avatar_unconfirmed') return t('settings:avatar.unconfirmed');
  if (error.code === 'avatar_unavailable') return t('settings:avatar.unavailable');
  return t('settings:avatar.failed');
}
export function classifyAvatarFailure(error: unknown, stage: AvatarStage): Error {
  if (isCancel(error) || error instanceof AvatarFailure) return error;
  const failure = new AvatarFailure(stage, error);
  if (__DEV__) console.info('avatar.failure', { stage: failure.stage, status: failure.status, code: failure.code });
  return failure;
}
