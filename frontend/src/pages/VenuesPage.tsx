import { useEffect, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { api } from "../api.ts";
import { km, nzd, sportName, titleOf } from "../format.ts";
import { useApp } from "../state.tsx";
import type { Sport, VenueList } from "../types.ts";
import { SPORTS } from "../format.ts";

export function VenuesPage() {
  const navigate = useNavigate();
  const { meta, lang, tx } = useApp();
  const [sport, setSport] = useState<Sport | "">("");
  const [walkIn, setWalkIn] = useState(false);
  const [hasSessions, setHasSessions] = useState(false);
  const [area, setArea] = useState("all");
  const [data, setData] = useState<VenueList | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!meta) return;
    let alive = true;
    const params = new URLSearchParams({ date: meta.today });
    if (sport) params.set("sport", sport);
    if (walkIn) params.set("walkIn", "true");
    if (hasSessions) params.set("hasSessions", "true");
    api<VenueList>(`/api/venues?${params}`)
      .then((rows) => { if (alive) setData(rows); })
      .catch((reason: Error) => { if (alive) setError(reason.message); });
    return () => { alive = false; };
  }, [meta, sport, walkIn, hasSessions]);

  const venues = (data?.venues ?? []).filter((venue) => area === "all" || venue.area === area);

  return (
    <div className="page-wide">
      <div className="split">
        <div className="map-card">
          <svg className="map" viewBox="0 0 320 420" role="img" aria-label={tx("奥克兰示意图", "Auckland map")}>
            <rect width="320" height="420" rx="28" fill="#e7f3ea" />
            <ellipse cx="168" cy="78" rx="118" ry="42" fill="#c5e4ea" />
            <ellipse cx="150" cy="348" rx="124" ry="46" fill="#c5e4ea" />
            <text x="24" y="36" className="map-label">{tx("市中心", "City")}</text>
            <text x="188" y="74" className="map-label">Waitematā</text>
            <text x="168" y="360" className="map-label">Manukau</text>
            {venues.map((venue) => (
              <g key={venue.id} className="pin-hit" onClick={() => navigate(`/venues/${venue.id}`)}>
                <circle cx={(venue.mapX / 100) * 320} cy={(venue.mapY / 100) * 420} r={venue.sessionsOnDate > 0 ? 11 : 7} className={venue.sessionsOnDate > 0 ? "pin hot" : "pin"} />
                <text x={(venue.mapX / 100) * 320 + 14} y={(venue.mapY / 100) * 420 + 4} className="pin-label">{venue.suburb}</text>
              </g>
            ))}
          </svg>
          <div className="area-row">
            <button type="button" className={area === "all" ? "chip on" : "chip"} onClick={() => setArea("all")}>{tx("全部场馆", "All venues")}</button>
            {data?.areas.map((item) => (
              <button key={item.area} type="button" className={area === item.area ? "chip on" : "chip"} onClick={() => setArea(item.area)}>
                {item.area} · {item.sessions > 0 ? tx(`${item.sessions} 场`, `${item.sessions}`) : item.walkIn ? tx("可订场", "Bookable") : tx("0 场", "0")}
              </button>
            ))}
          </div>
        </div>
        <div>
          <div className="filters">
            <select value={sport} onChange={(event) => setSport(event.target.value as Sport | "")}>
              <option value="">{tx("全部运动", "All sports")}</option>
              {SPORTS.map((item) => <option key={item} value={item}>{sportName(item, lang)}</option>)}
            </select>
            <button type="button" className={hasSessions ? "chip on" : "chip"} onClick={() => setHasSessions((value) => !value)}>{tx("今天有活动", "Sessions today")}</button>
            <button type="button" className={walkIn ? "chip on" : "chip"} onClick={() => setWalkIn((value) => !value)}>{tx("可散客订场", "Walk-in courts")}</button>
          </div>
          {error && <p className="banner">{error}</p>}
          <div className="stack">
            {venues.map((venue) => (
              <Link key={venue.id} to={`/venues/${venue.id}`} className="card venue-card">
                <div className="spread">
                  <h2>{venue.name}</h2>
                  <span className={venue.isOpen ? "open" : "closed"}>{venue.isOpen ? tx("营业中", "Open") : tx("已打烊", "Closed")}</span>
                </div>
                <p className="place">{venue.address} · {lang === "en" ? `${km(venue.distanceKm)} away` : `离你 ${km(venue.distanceKm)}`}</p>
                <p className="when">{tx(`${venue.courtCount} 片场地`, `${venue.courtCount} courts`)} · {venue.walkInBooking ? tx("可散客订场", "Walk-in courts") : tx("随活动开放", "Opens with sessions")} · {tx("场地费", "Court")} {venue.hourlyRate == null ? tx("待填", "TBC") : nzd(venue.hourlyRate)}/{tx("小时", "hr")}</p>
                {venue.sessions[0] && <p className="muted">{tx("今天", "Today")} · {titleOf(venue.sessions[0], lang)}</p>}
              </Link>
            ))}
            {venues.length === 0 && <p className="muted">{tx("没有符合条件的场馆", "No venues match")}</p>}
          </div>
        </div>
      </div>
    </div>
  );
}
