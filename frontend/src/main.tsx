import React from "react";
import ReactDOM from "react-dom/client";
import { BrowserRouter } from "react-router-dom";
import Auth0ProviderWithNavigate from "./contexts/authContext";
import { ToastProvider } from "./contexts/toastContext";
import App from "./App";
import "./index.css";

ReactDOM.createRoot(document.getElementById("root")!).render(
  <React.StrictMode>
    {/* o router precisa estar acima do provider do Auth0 porque ele usa useNavigate(). */}
    <BrowserRouter>
      <Auth0ProviderWithNavigate>
        <ToastProvider>
          <App />
        </ToastProvider>
      </Auth0ProviderWithNavigate>
    </BrowserRouter>
  </React.StrictMode>,
);
