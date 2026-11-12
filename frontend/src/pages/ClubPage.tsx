import { useEffect, useState } from "react";
import { Link, useParams, useSearchParams } from "react-router-dom";
import { api } from "../api.ts";
import { Avatar, Back } from "../components.tsx";
import { clock, sportName, titleOf } from "../format.ts";
import { useApp } from "../state.tsx";
import type { Club, ConnectResult } from "../types.ts";

export function ClubPage() {
  const { id } = useParams();
  const [params, setParams] = useSearchParams();
  const { lang, tx, toast, meta } = useApp();
  const [club, setClub] = useState<Club | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  async function load() {
    const row = await api<Club>(`/api/clubs/${id}`);
    setClub(row);
  }

  useEffect(() => {
    if (!id) return;
    let alive = true;
    load().catch((reason: Error) => { if (alive) setError(reason.message); });
    return () => { alive = false; };
  }, [id]);

  useEffect(() => {
    const flag = params.get("stripe");
    if (!flag || !id || !club?.mine) return;
    setParams({}, { replace: true });
    api<ConnectResult>(`/api/clubs/${id}/stripe/refresh`, { method: "POST" })
      .then(async (result) => {
        toast(result.connected ? tx("收款账户已连接", "Payout account connected") : tx("Stripe 还没完成审核", "Stripe has not finished reviewing the account"));
        await load();
      })
      .catch((reason: Error) => toast(reason.message));
  }, [params, club?.mine, id]);

  async function connect() {
    if (!club) return;
    setBusy(true);
    try {
      if (meta?.stripeEnabled) {
        const result = await api<ConnectResult>(`/api/clubs/${club.id}/stripe/connect`, { method: "POST" });
        if (result.url) window.location.href = result.url;
        return;
      }
      await api(`/api/clubs/${club.id}/stripe/demo`, { method: "POST" });
      toast(tx("已模拟连接收款账户", "Demo payout account connected"));
      await load();
    } catch (reason) {
      toast(reason instanceof Error ? reason.message : tx("连接失败", "Could not connect"));
    } finally {
      setBusy(false);
    }
  }

  if (error) return <div className="page"><Back fallback="/clubs" /><p className="banner">{error}</p></div>;
  if (!club) return <div className="page"><div className="skeleton tall" /></div>;

  return (
    <div className="page">
      <Back fallback="/clubs" />
      <div className="club-row">
        <Avatar text={club.monogram} url={club.avatarUrl} color={club.color} size={72} />
        <div>
          <h1>{club.name}</h1>
          <p className="muted">{sportName(club.sport, lang)} · {club.ownerName}</p>
        </div>
      </div>
      <p>{club.bio}</p>
      <p className="when">{tx(`已办 ${club.eventsHosted} 场`, `${club.eventsHosted} hosted`)} · {tx("评分", "Rating")} {club.rating.toFixed(1)} · {tx("回头率", "Return rate")} {Math.round(club.returnRate * 100)}%</p>
      {club.homeVenueName && <p className="place"><Link to={`/venues/${club.homeVenueId}`}>{club.homeVenueName}</Link></p>}
      <div className="card compact">
        <strong>{tx("收款账户", "Payouts")}</strong>
        <p className="muted">
          {club.stripeConnected
            ? (club.stripeDemo ? tx("已模拟连接。配置 Stripe 密钥后会改成真实开户。", "Demo connection. Add a Stripe key to switch to a real account.") : tx("Stripe 已连接，报名费会进入这个账户。", "Stripe is connected. Fees settle to this account."))
            : tx("还没连接。连接后，报名费直接进入你的银行卡。", "Not connected yet. Once it is, fees go to your bank account.")}
        </p>
        {club.mine && !club.stripeConnected && <button type="button" className="btn" disabled={busy} onClick={() => void connect()}>{tx("去连接", "Connect")}</button>}
        {club.mine && <Link className="btn ghost" to={`/publish?clubId=${club.id}`}>{tx("发布活动", "Publish a session")}</Link>}
      </div>
      <h2>{tx("即将开始", "Coming up")}</h2>
      <div className="stack">
        {club.upcoming.map((session) => (
          <Link key={session.id} to={`/activities/${session.id}`} className="card compact">
            <strong>{titleOf(session, lang)}</strong>
            <p className="muted">{clock(session.startAt)}–{clock(session.endAt)} · {session.joined}/{session.capacity}</p>
          </Link>
        ))}
        {club.upcoming.length === 0 && <p className="muted">{tx("还没有即将开始的活动", "No upcoming sessions")}</p>}
      </div>
    </div>
  );
}
