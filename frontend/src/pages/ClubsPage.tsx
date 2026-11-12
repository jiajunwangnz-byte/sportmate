import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { api } from "../api.ts";
import { Avatar } from "../components.tsx";
import { sportName } from "../format.ts";
import { useApp } from "../state.tsx";
import type { Club, Sport } from "../types.ts";
import { SPORTS } from "../format.ts";

export function ClubsPage() {
  const { lang, tx } = useApp();
  const [sport, setSport] = useState<Sport | "">("");
  const [clubs, setClubs] = useState<Club[]>([]);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let alive = true;
    const params = sport ? `?sport=${sport}` : "";
    api<Club[]>(`/api/clubs${params}`)
      .then((rows) => { if (alive) setClubs(rows); })
      .catch((reason: Error) => { if (alive) setError(reason.message); });
    return () => { alive = false; };
  }, [sport]);

  return (
    <div className="page">
      <div className="spread">
        <h1>{tx("俱乐部", "Clubs")}</h1>
        <Link className="btn" to="/clubs/new">{tx("创建俱乐部", "Create club")}</Link>
      </div>
      <div className="filters">
        <button type="button" className={sport === "" ? "chip on" : "chip"} onClick={() => setSport("")}>{tx("全部", "All")}</button>
        {SPORTS.map((item) => (
          <button key={item} type="button" className={sport === item ? "chip on" : "chip"} onClick={() => setSport(item)}>{sportName(item, lang)}</button>
        ))}
      </div>
      {error && <p className="banner">{error}</p>}
      <div className="stack">
        {clubs.map((club) => (
          <Link key={club.id} to={`/clubs/${club.id}`} className="card club-row">
            <Avatar text={club.monogram} url={club.avatarUrl} color={club.color} size={52} />
            <div>
              <h2>{club.name} {club.mine && <span className="tag green">{tx("我的", "Mine")}</span>}</h2>
              <p className="muted">{sportName(club.sport, lang)} · {tx(`已办 ${club.eventsHosted} 场`, `${club.eventsHosted} hosted`)} · {club.rating.toFixed(1)} · {tx("回头率", "Return")} {Math.round(club.returnRate * 100)}%</p>
              {club.homeVenueName && <p className="place">{club.homeVenueName}</p>}
            </div>
          </Link>
        ))}
      </div>
    </div>
  );
}
