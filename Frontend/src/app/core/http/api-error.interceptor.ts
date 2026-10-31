import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';

import { toApiError } from './api-error';

/**
 * Interceptor único de errores (`IF-02`): traduce el `ProblemDetails` del backend a
 * `ApiError` para que los componentes solo lean `code` y `message`.
 */
export const apiErrorInterceptor: HttpInterceptorFn = (request, next) =>
  next(request).pipe(
    catchError((error: unknown) =>
      throwError(() => (error instanceof HttpErrorResponse ? toApiError(error) : error))
    )
  );
