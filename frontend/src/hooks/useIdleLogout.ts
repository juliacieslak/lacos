// RNF03: encerra a sessão após um período sem interação (padrão: 15 minutos, informado pelo backend).
import { useEffect, useRef } from "react";

const EVENTS = ["mousemove", "keydown", "pointerdown", "scroll", "touchstart"] as const;

export function useIdleLogout(timeoutMinutes: number | undefined, onIdle: () => void) {
  const onIdleRef = useRef(onIdle);
  useEffect(() => {
    onIdleRef.current = onIdle;
  });

  useEffect(() => {
    if (!timeoutMinutes) return;
    let timer = 0;
    const reset = () => {
      window.clearTimeout(timer);
      timer = window.setTimeout(() => onIdleRef.current(), timeoutMinutes * 60_000);
    };
    EVENTS.forEach((e) => window.addEventListener(e, reset, { passive: true }));
    reset();
    return () => {
      window.clearTimeout(timer);
      EVENTS.forEach((e) => window.removeEventListener(e, reset));
    };
  }, [timeoutMinutes]);
}
