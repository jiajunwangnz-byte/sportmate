import type { ReactNode } from "react";
import { NavLink } from "react-router-dom";
import { useApp } from "./state.tsx";

function CourtIcon() {
  return <svg viewBox="0 0 24 24" aria-hidden="true"><rect x="3" y="4" width="18" height="16" rx="2" fill="none" stroke="currentColor" strokeWidth="1.8" /><path d="M12 4v16M3 12h18" stroke="currentColor" strokeWidth="1.4" /></svg>;
}
function PinIcon() {
  return <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M12 21s7-6.2 7-11a7 7 0 1 0-14 0c0 4.8 7 11 7 11z" fill="none" stroke="currentColor" strokeWidth="1.8" /><circle cx="12" cy="10" r="2.2" fill="currentColor" /></svg>;
}
function ClubIcon() {
  return <svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="8" cy="9" r="3" fill="none" stroke="currentColor" strokeWidth="1.8" /><circle cx="16" cy="9" r="3" fill="none" stroke="currentColor" strokeWidth="1.8" /><path d="M3.5 19c.8-2.6 2.6-4 4.5-4s3.7 1.4 4.5 4M12 19c.8-2.6 2.6-4 4.5-4s3.7 1.4 4.5 4" fill="none" stroke="currentColor" strokeWidth="1.8" /></svg>;
}
function MeIcon() {
  return <svg viewBox="0 0 24 24" aria-hidden="true"><circle cx="12" cy="8" r="3.2" fill="none" stroke="currentColor" strokeWidth="1.8" /><path d="M5 19c1.2-3 3.4-4.5 7-4.5S17.8 16 19 19" fill="none" stroke="currentColor" strokeWidth="1.8" /></svg>;
}

const TABS = [
  { to: "/", zh: "活动", en: "Sessions", Icon: CourtIcon, end: true },
  { to: "/venues", zh: "找场馆", en: "Venues", Icon: PinIcon, end: false },
  { to: "/clubs", zh: "俱乐部", en: "Clubs", Icon: ClubIcon, end: false },
  { to: "/me", zh: "我的", en: "Me", Icon: MeIcon, end: true },
];

export function Shell({ showTabs, children }: { showTabs: boolean; children: ReactNode }) {
  const { lang, setLang, tx, toasts } = useApp();
  return (
    <div className="app">
      <aside className="side">
        <div className="brand">
          <span className="mark">羽</span>
          <span><strong>约局</strong><small>SportMate · {tx("奥克兰", "Auckland")}</small></span>
        </div>
        <nav className="side-nav">
          {TABS.map((tab) => (
            <NavLink key={tab.to} to={tab.to} end={tab.end} className={({ isActive }) => isActive ? "nav-link active" : "nav-link"}>
              <tab.Icon />
              {lang === "en" ? tab.en : tab.zh}
            </NavLink>
          ))}
        </nav>
      </aside>
      <div className="main">
        <header className="topbar">
          <div className="brand mobile-brand">
            <span className="mark">羽</span>
            <strong>约局</strong>
          </div>
          <div className="loc" title={tx("目前只开通了奥克兰", "Auckland only for now")}>{tx("奥克兰", "Auckland")}</div>
          <div className="lang">
            <button type="button" className={lang === "zh" ? "on" : ""} onClick={() => setLang("zh")}>中文</button>
            <button type="button" className={lang === "en" ? "on" : ""} onClick={() => setLang("en")}>EN</button>
          </div>
        </header>
        <div className={showTabs ? "content with-tabs" : "content"}>{children}</div>
      </div>
      {showTabs && (
        <nav className="tabbar">
          {TABS.map((tab) => (
            <NavLink key={tab.to} to={tab.to} end={tab.end} className={({ isActive }) => isActive ? "tab active" : "tab"}>
              <tab.Icon />
              <span>{lang === "en" ? tab.en : tab.zh}</span>
            </NavLink>
          ))}
        </nav>
      )}
      <div className="toasts">
        {toasts.map((item) => <div key={item.id} className="toast">{item.text}</div>)}
      </div>
    </div>
  );
}
