import { QueryClient } from '@tanstack/react-query';
import { ApiError } from './api';
export const queryClient = new QueryClient({ defaultOptions: { queries: { staleTime: 30000, retry: (count, error) => !(error instanceof ApiError && [401, 403, 429].includes(error.status ?? 0)) && count < 1 }, mutations: { retry: false } } });
