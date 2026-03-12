import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { GraphTokenService } from '../services/graph-token.service';

/**
 * Intercepts requests to Graph API endpoints and attaches the X-Graph-Token header
 * when a Microsoft Graph token is available.
 */
export const graphTokenInterceptor: HttpInterceptorFn = (req, next) => {
  if (!req.url.includes('/api/Graph/')) {
    return next(req);
  }

  const graphTokenService = inject(GraphTokenService);
  const token = graphTokenService.getToken();

  if (token) {
    const clonedReq = req.clone({
      setHeaders: { 'X-Graph-Token': token },
    });
    return next(clonedReq);
  }

  return next(req);
};
