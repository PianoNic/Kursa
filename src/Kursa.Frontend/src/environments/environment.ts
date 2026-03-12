export const environment = {
  production: true,
  apiBaseUrl: window.location.origin,
  oidc: {
    issuer: 'https://auth.gaggao.com',
    clientId: '59793f91-bdda-4425-9e5c-1bdfd9baa5b6',
    redirectUri: '/callback',
    postLogoutRedirectUri: '/',
    scope: 'openid profile email',
    responseType: 'code',
    showDebugInformation: false,
    strictDiscoveryDocumentValidation: false,
  },
};
