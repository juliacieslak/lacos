// configuração do frontend lida das variáveis de ambiente do Vite (arquivo .env.local).
// apenas valores públicos ficam aqui: domínio/client id do Auth0 e URL da API.
// nunca coloque segredos (client secret, service role do Supabase) em variáveis VITE_.

export const env = {
  apiUrl: (import.meta.env.VITE_API_URL ?? "http://localhost:5080").replace(/\/$/, ""),
  auth0Domain: import.meta.env.VITE_AUTH0_DOMAIN ?? "",
  auth0ClientId: import.meta.env.VITE_AUTH0_CLIENT_ID ?? "",
  auth0Audience: import.meta.env.VITE_AUTH0_AUDIENCE ?? "",
};

// lista das variáveis do Auth0 que ainda não foram preenchidas.
export const missingAuthConfig = [
  ["VITE_AUTH0_DOMAIN", env.auth0Domain],
  ["VITE_AUTH0_CLIENT_ID", env.auth0ClientId],
  ["VITE_AUTH0_AUDIENCE", env.auth0Audience],
]
  .filter(([, value]) => !value)
  .map(([name]) => name);
