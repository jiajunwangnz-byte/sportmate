import { useEffect } from "react";
import { Route, Routes, useLocation } from "react-router-dom";
import { ActivityPage } from "./pages/ActivityPage.tsx";
import { ClubPage } from "./pages/ClubPage.tsx";
import { ClubsPage } from "./pages/ClubsPage.tsx";
import { CreateClubPage } from "./pages/CreateClubPage.tsx";
import { HomePage } from "./pages/HomePage.tsx";
import { MePage } from "./pages/MePage.tsx";
import { PublishPage } from "./pages/PublishPage.tsx";
import { VenuePage } from "./pages/VenuePage.tsx";
import { VenuesPage } from "./pages/VenuesPage.tsx";
import { Shell } from "./shell.tsx";
import { AppProvider, useApp } from "./state.tsx";

function ScrollToTop() {
  const { pathname } = useLocation();
  useEffect(() => { window.scrollTo(0, 0); }, [pathname]);
  return null;
}

function Gate() {
  const { ready, error, reload, tx } = useApp();
  const location = useLocation();
  if (!ready) {
    return (
      <div className="boot">
        <span className="mark">羽</span>
        {error ? (
          <>
            <h1>{tx("连不上服务器", "Can't reach the server")}</h1>
            <p>{error}</p>
            <button className="btn" type="button" onClick={() => void reload()}>{tx("再试一次", "Try again")}</button>
          </>
        ) : <p>{tx("正在打开球场…", "Opening the court…")}</p>}
      </div>
    );
  }
  const showTabs = ["/", "/venues", "/clubs", "/me"].includes(location.pathname);
  return (
    <Shell showTabs={showTabs}>
      <ScrollToTop />
      <Routes>
        <Route path="/" element={<HomePage />} />
        <Route path="/activities/:id" element={<ActivityPage />} />
        <Route path="/venues" element={<VenuesPage />} />
        <Route path="/venues/:id" element={<VenuePage />} />
        <Route path="/clubs" element={<ClubsPage />} />
        <Route path="/clubs/new" element={<CreateClubPage />} />
        <Route path="/clubs/:id" element={<ClubPage />} />
        <Route path="/publish" element={<PublishPage />} />
        <Route path="/me" element={<MePage />} />
      </Routes>
    </Shell>
  );
}

export default function App() {
  return (
    <AppProvider>
      <Gate />
    </AppProvider>
  );
}
