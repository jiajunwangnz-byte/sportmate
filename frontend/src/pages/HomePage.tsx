import { useEffect, useState } from "react";
import { ActivityCard, Empty, SkillScale } from "../components.tsx";
import { api } from "../api.ts";
import { addDays, monthDay, sportName, weekday } from "../format.ts";
import { useApp } from "../state.tsx";
import type { Activity, DayCount, Sport } from "../types.ts";
import { SPORTS } from "../format.ts";

const AREAS = ["Epsom", "Massey", "Pakuranga", "North Shore", "Parnell", "Grey Lynn", "Onehunga"];

export function HomePage() {
  const { meta, lang, tx } = useApp();
  const [sport, setSport] = useState<Sport>("Badminton");
  const [date, setDate] = useState<string | null>(null);
  const [sort, setSort] = useState("nearest");
  const [area, setArea] = useState("all");
  const [skillMin, setSkillMin] = useState(2);
  const [skillMax, setSkillMax] = useState(5);
  const [openOnly, setOpenOnly] = useState(false);
  const [items, setItems] = useState<Activity[]>([]);
  const [summary, setSummary] = useState<DayCount[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const selected = date ?? meta?.today ?? null;

  useEffect(() => {
    if (!selected) return;
    let alive = true;
    setLoading(true);
    const params = new URLSearchParams({
      sport,
      date: selected,
      sort,
      skillMin: String(skillMin),
      skillMax: String(skillMax),
      openOnly: String(openOnly),
    });
    if (area !== "all") params.set("area", area);
    api<Activity[]>(`/api/activities?${params}`)
      .then((rows) => { if (alive) { setItems(rows); setError(null); } })
      .catch((reason: Error) => { if (alive) setError(reason.message); })
      .finally(() => { if (alive) setLoading(false); });
    return () => { alive = false; };
  }, [sport, selected, sort, area, skillMin, skillMax, openOnly]);

  useEffect(() => {
    if (!meta) return;
    const params = new URLSearchParams({ sport, from: meta.today, to: addDays(meta.today, 6) });
    api<DayCount[]>(`/api/activities/summary?${params}`).then(setSummary).catch(() => setSummary([]));
  }, [sport, meta]);

  const days = meta ? Array.from({ length: 7 }, (_, index) => addDays(meta.today, index)) : [];
  const hottest = [...summary].sort((a, b) => b.count - a.count)[0];

  return (
    <div className="page">
      <div className="sport-tabs" role="tablist">
        {SPORTS.map((item) => (
          <button key={item} className={item === sport ? "on" : ""} onClick={() => setSport(item)} type="button">
            {sportName(item, lang)}
          </button>
        ))}
      </div>
      <div className="date-strip">
        {days.map((day) => {
          const count = summary.find((item) => item.date === day)?.count ?? 0;
          const isToday = day === meta?.today;
          return (
            <button key={day} type="button" className={day === selected ? "date-chip on" : "date-chip"} onClick={() => setDate(day)}>
              <small>{isToday ? tx("今天", "Today") : weekday(day, lang)}</small>
              <strong>{Number(day.slice(8, 10))}</strong>
              {count > 0 && <i className="dot" />}
            </button>
          );
        })}
      </div>
      <div className="filters">
        <select value={sort} onChange={(event) => setSort(event.target.value)} aria-label={tx("排序", "Sort")}>
          <option value="nearest">{tx("离我最近", "Nearest")}</option>
          <option value="soon">{tx("即将开始", "Starting soon")}</option>
          <option value="price">{tx("价格从低到高", "Price")}</option>
        </select>
        <select value={area} onChange={(event) => setArea(event.target.value)} aria-label={tx("区域", "Area")}>
          <option value="all">{tx("全城", "All Auckland")}</option>
          {AREAS.map((item) => <option key={item} value={item}>{item}</option>)}
        </select>
        <details className="filter">
          <summary>{tx("水平", "Level")} {skillMin === 1 && skillMax === 8 ? tx("不限", "Any") : `L${skillMin}–L${skillMax}`}</summary>
          <div className="menu">
            <SkillScale min={skillMin} max={skillMax} onChange={(min, max) => { setSkillMin(min); setSkillMax(max); }} />
          </div>
        </details>
        <button type="button" className={openOnly ? "chip on" : "chip"} onClick={() => setOpenOnly((value) => !value)}>
          {tx("有空位", "Open spots")}
        </button>
      </div>
      {hottest && selected && hottest.date !== selected && hottest.count > 0 && (
        <button type="button" className="hint" onClick={() => setDate(hottest.date)}>
          {monthDay(hottest.date, lang)} {weekday(hottest.date, lang)} · {hottest.count} {tx("场", "sessions")}
        </button>
      )}
      {error && <p className="banner">{error}</p>}
      {loading && items.length === 0 && (
        <div className="stack">{[0, 1, 2].map((item) => <div key={item} className="skeleton" />)}</div>
      )}
      {!loading && items.length === 0 && (
        <Empty
          title={tx("这一天还没有局", "No sessions this day")}
          body={tx("换个日期，或者看看上面提示的那天。", "Pick another day, or jump to the busiest one above.")}
        />
      )}
      <div className="stack">
        {items.map((activity) => <ActivityCard key={activity.id} activity={activity} />)}
      </div>
    </div>
  );
}
