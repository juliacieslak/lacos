import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import tailwindcss from "@tailwindcss/vite";

export default defineConfig({
  plugins: [react(), tailwindcss()],
  // porta 5173 está liberada no CORS de desenvolvimento do backend.
  server: { port: 5173, strictPort: true },
  preview: { port: 4173 },
  build: {
    rolldownOptions: {
      output: {
        // separa as bibliotecas (mudam pouco e ficam em cache no navegador) do código da aplicação.
        codeSplitting: {
          groups: [
            { name: "react-vendor", test: /node_modules[\\/](react|react-dom|react-router|react-router-dom|scheduler)[\\/]/ },
            { name: "auth0", test: /node_modules[\\/]@auth0[\\/]/ },
            { name: "icons", test: /node_modules[\\/]lucide-react[\\/]/ },
          ],
        },
      },
    },
  },
});
