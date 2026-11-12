import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { api } from "../api.ts";
import { Back } from "../components.tsx";
import { clock, km, mapsUrl, nzd, sportName, titleOf } from "../format.ts";
import { useApp } from "../state.tsx";
import type { Venue } from "../types.ts";

export function VenuePage() {
  const { id } = useParams();
  const { meta, lang, tx } = useApp();
  const [venue, setVenue] = useState<Venue | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!meta || !id) return;
    let alive = true;
    api<Venue>(`/api/venues/${id}?date=${meta.today}`)
      .then((row) => { if (alive) setVenue(row); })
      .catch((reason: Error) => { if (alive) setError(reason.message); });
    return () => { alive = false; };
  }, [id, meta]);

  if (error) return <div className="page"><Back fallback="/venues" /><p className="banner">{error}</p></div>;
  if (!venue) return <div className="page"><div className="skeleton tall" /></div>;

  return (
    <div className="page">
      <Back fallback="/venues" />
      <div className="spread">
        <h1>{venue.name}</h1>
        <span className={venue.isOpen ? "open" : "closed"}>{venue.isOpen ? tx("营业中", "Open") : tx("已打烊", "Closed")}</span>
      </div>
      <p className="place">{venue.address}</p>
      <p className="when">{tx("离你", "Away")} {km(venue.distanceKm)} · {venue.opensAt}–{venue.closesAt}</p>
      <div className="tags">
        <span className="tag line">{tx(`${venue.courtCount} 片场地`, `${venue.courtCount} courts`)}</span>
        {venue.walkInBooking && <span className="tag green">{tx("可散客订场", "Walk-in courts")}</span>}
        <span className="tag line">{tx("场地费", "Court fee")} {venue.hourlyRate == null ? tx("待填", "TBC") : nzd(venue.hourlyRate)}/{tx("小时", "hr")}</span>
        {venue.sports.map((sport) => <span key={sport} className="tag line">{sportName(sport, lang)}</span>)}
      </div>
      <h2>{tx("今天这里的活动", "Sessions here today")}</h2>
      <div className="stack">
        {venue.sessions.map((session) => (
          <Link key={session.id} to={`/activities/${session.id}`} className="card compact">
            <div className="spread">
              <strong>{clock(session.startAt)}–{clock(session.endAt)} {titleOf(session, lang)}</strong>
              <span>{session.spotsLeft === 0 ? tx("已满", "Full") : `${session.joined}/${session.capacity}`}</span>
            </div>
          </Link>
        ))}
        {venue.sessions.length === 0 && <p className="muted">{tx("今天没有活动，你可以直接在这里发起一场。", "Nothing on today. You can start one here.")}</p>}
      </div>
      <div className="row dock-inline">
        <a className="btn ghost" href={mapsUrl(venue.address)} target="_blank" rel="noreferrer">{tx("导航", "Directions")}</a>
        <Link className="btn" to={`/publish?venueId=${venue.id}`}>{tx("在这里发起活动", "Host here")}</Link>
      </div>
    </div>
  );
}
