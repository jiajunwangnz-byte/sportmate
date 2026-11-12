import { createContext, useContext, useEffect, useState } from "react";
import type { ReactNode } from "react";
import { api } from "./api.ts";
import type { Me, Meta, User } from "./types.ts";

export interface Toast {
  id: number;
  text: string;
}

interface AppState {
  lang: "zh" | "en";
  setLang: (lang: "zh" | "en") => void;
  tx: (zh: string, en: string) => string;
  meta: Meta | null;
  user: User | null;
  users: User[];
  ready: boolean;
  error: string | null;
  reload: () => Promise<void>;
  switchUser: (id: string) => Promise<void>;
  toast: (text: string) => void;
  toasts: Toast[];
}

const AppContext = createContext<AppState | null>(null);

export function AppProvider({ children }: { children: ReactNode }) {
  const stored = localStorage.getItem("sportmate-lang");
  const [lang, setLangState] = useState<"zh" | "en">(stored === "en" ? "en" : "zh");
  const [meta, setMeta] = useState<Meta | null>(null);
  const [user, setUser] = useState<User | null>(null);
  const [users, setUsers] = useState<User[]>([]);
  const [ready, setReady] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [toasts, setToasts] = useState<Toast[]>([]);

  function setLang(next: "zh" | "en") {
    localStorage.setItem("sportmate-lang", next);
    setLangState(next);
  }

  function toast(text: string) {
    const id = Date.now() + Math.round(Math.random() * 1000);
    setToasts((list) => [...list, { id, text }]);
    window.setTimeout(() => setToasts((list) => list.filter((item) => item.id !== id)), 3200);
  }

  async function load(userId?: string) {
    setError(null);
    const nextMeta = await api<Meta>("/api/meta");
    const directory = await api<User[]>("/api/users");
    const saved = userId ?? localStorage.getItem("sportmate-user");
    const pick = directory.find((item) => item.id === saved)?.id
      ?? directory.find((item) => item.name === "林可")?.id
      ?? directory[0]?.id;
    if (!pick) throw new Error("没有可用的演示账号");
    localStorage.setItem("sportmate-user", pick);
    const me = await api<Me>("/api/me");
    setMeta(nextMeta);
    setUsers(directory);
    setUser(me.user);
    setReady(true);
  }

  async function reload() {
    try {
      await load();
    } catch (reason) {
      const message = reason instanceof Error ? reason.message : "连不上服务器";
      setError(message);
      setReady(false);
    }
  }

  useEffect(() => {
    void reload();
  }, []);

  const value: AppState = {
    lang,
    setLang,
    tx: (zh, en) => (lang === "zh" ? zh : en),
    meta,
    user,
    users,
    ready,
    error,
    reload,
    switchUser: async (id: string) => {
      await load(id);
    },
    toast,
    toasts,
  };

  return <AppContext.Provider value={value}>{children}</AppContext.Provider>;
}

export function useApp() {
  const value = useContext(AppContext);
  if (!value) throw new Error("AppProvider missing");
  return value;
}
