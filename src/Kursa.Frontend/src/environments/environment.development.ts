export const environment = {
  production: false,
  apiBaseUrl: 'http://localhost:5024',
  oidc: {
    issuer: 'https://auth.gaggao.com',
    clientId: '59793f91-bdda-4425-9e5c-1bdfd9baa5b6',
    redirectUri: 'http://localhost:4200/callback',
    postLogoutRedirectUri: 'http://localhost:4200',
    scope: 'openid profile email',
    responseType: 'code',
    showDebugInformation: true,
    strictDiscoveryDocumentValidation: false,
  },
};
