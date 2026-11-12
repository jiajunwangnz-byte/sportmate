import type { ReactNode } from "react";
import { Link, useNavigate } from "react-router-dom";
import { useApp } from "./state.tsx";
import type { Activity } from "./types.ts";
import { km, level, levelRange, nzd, sportName, titleOf, whenLine } from "./format.ts";

export function Avatar({ text, url, color, size = 40 }: { text: string; url?: string | null; color?: string; size?: number }) {
  const style = { width: size, height: size, background: color ?? "#0e7a45" };
  if (url) return <img className="avatar" src={url} alt="" style={{ width: size, height: size }} />;
  return <span className="avatar" style={style}>{text.slice(0, 1)}</span>;
}

export function Tag({ children, tone = "green" }: { children: ReactNode; tone?: "green" | "amber" | "line" | "ink" }) {
  return <span className={`tag ${tone}`}>{children}</span>;
}

export function SkillScale({ min, max, onChange }: { min: number; max: number; onChange: (min: number, max: number) => void }) {
  function pick(value: number) {
    if (value < min) onChange(value, max);
    else if (value > max) onChange(min, value);
    else if (value - min <= max - value) onChange(value, max);
    else onChange(min, value);
  }
  return (
    <div className="scale">
      {[1, 2, 3, 4, 5, 6, 7, 8].map((value) => (
        <button key={value} type="button" className={value >= min && value <= max ? "on" : ""} onClick={() => pick(value)}>
          L{value}
        </button>
      ))}
    </div>
  );
}

export function Back({ fallback = "/" }: { fallback?: string }) {
  const navigate = useNavigate();
  const { tx } = useApp();
  return (
    <button type="button" className="back" onClick={() => navigate(fallback)}>
      {tx("返回", "Back")}
    </button>
  );
}

export function Empty({ title, body, action }: { title: string; body: string; action?: ReactNode }) {
  return (
    <div className="empty">
      <div className="court" aria-hidden="true" />
      <h2>{title}</h2>
      <p>{body}</p>
      {action}
    </div>
  );
}

export function ActivityCard({ activity }: { activity: Activity }) {
  const { lang, meta, tx } = useApp();
  const today = meta?.today ?? "";
  const full = activity.spotsLeft <= 0;
  return (
    <Link className="card activity-card" to={`/activities/${activity.id}`}>
      {activity.myStatus === "Paid" && <span className="ribbon">{tx("已报名", "Joined")}</span>}
      {activity.myStatus === "Waitlist" && <span className="ribbon wait">{tx("候补中", "Waitlist")}</span>}
      <div className="spread">
        <h2>{titleOf(activity, lang)}</h2>
        <strong className="price">{nzd(activity.price)}</strong>
      </div>
      <div className="tags">
        {activity.courtsBooked && <Tag>{tx("已订场", "Court booked")}</Tag>}
        {activity.womenFriendly && <Tag>{tx("女生友好", "Women friendly")}</Tag>}
        {activity.welcomeNewcomers && <Tag>{tx("欢迎新人", "Newcomers welcome")}</Tag>}
        {activity.competitive && <Tag tone="amber">{tx("对抗局", "Competitive")}</Tag>}
        {activity.casual && <Tag tone="line">{tx("养生局", "Easy pace")}</Tag>}
        {activity.parkingFree && <Tag tone="line">{tx("停车免费", "Free parking")}</Tag>}
      </div>
      <p className="place">{activity.venue.name} · {activity.venue.suburb} · {km(activity.venue.distanceKm)}</p>
      <p className="when">{whenLine(activity.startAt, activity.endAt, today, lang)} · {sportName(activity.sport, lang)}</p>
      <div className="spread spots">
        <span>{full ? (activity.waitlist > 0 ? tx(`已满 · 候补 ${activity.waitlist} 人`, `Full · ${activity.waitlist} waiting`) : tx("已满", "Full")) : tx(`报名 ${activity.joined}/${activity.capacity}`, `${activity.joined}/${activity.capacity} in`)}</span>
        <span>{levelRange(activity.skillMin, activity.skillMax)}</span>
      </div>
      <div className={`bar ${full ? "full" : ""}`}><span style={{ width: `${Math.min(100, (activity.joined / activity.capacity) * 100)}%` }} /></div>
      <div className="club-line">
        <Avatar text={activity.club.monogram} url={activity.club.avatarUrl} color={activity.club.color} size={28} />
        <span>{activity.club.name} · {tx(`已办 ${activity.club.eventsHosted} 场`, `${activity.club.eventsHosted} hosted`)} · {activity.club.rating.toFixed(1)}</span>
      </div>
    </Link>
  );
}

export function levelText(value: number) {
  return level(value);
}
