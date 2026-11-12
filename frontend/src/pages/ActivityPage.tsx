import { useEffect, useRef, useState } from "react";
import { Link, useParams, useSearchParams } from "react-router-dom";
import { api } from "../api.ts";
import { Avatar, Back, Tag } from "../components.tsx";
import { chargeFor, clock, copyText, genderText, level, nzd, refundLocked, refundText, titleOf, whenLine } from "../format.ts";
import { useApp } from "../state.tsx";
import type { Activity, JoinResult, TransferResult } from "../types.ts";

export function ActivityPage() {
  const { id } = useParams();
  const [params, setParams] = useSearchParams();
  const { user, meta, lang, tx, toast, reload } = useApp();
  const [activity, setActivity] = useState<Activity | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [pay, setPay] = useState<JoinResult | null>(null);
  const [transfer, setTransfer] = useState<TransferResult | null>(null);
  const [expanded, setExpanded] = useState(false);
  const [busy, setBusy] = useState(false);
  const handled = useRef("");

  async function load() {
    const row = await api<Activity>(`/api/activities/${id}`);
    setActivity(row);
    setError(null);
  }

  useEffect(() => {
    if (!id) return;
    let alive = true;
    load().catch((reason: Error) => { if (alive) setError(reason.message); });
    return () => { alive = false; };
  }, [id, user?.id]);

  useEffect(() => {
    const key = params.toString();
    if (!key || handled.current === key || !id) return;
    const paid = params.get("paid");
    const sessionId = params.get("session_id");
    const registration = params.get("registration");
    if (paid !== "1" && paid !== "0") return;
    handled.current = key;
    const finish = () => setParams({}, { replace: true });
    if (paid === "1" && sessionId) {
      api("/api/payments/confirm", { method: "POST", body: JSON.stringify({ sessionId }) })
        .then(async () => { toast(tx("支付成功，名额已锁定", "Payment received. You're in.")); await load(); await reload(); })
        .catch((reason: Error) => toast(reason.message))
        .finally(finish);
    }
    if (paid === "0" && registration) {
      api(`/api/registrations/${registration}/cancel`, { method: "POST" })
        .catch(() => undefined)
        .finally(() => { toast(tx("支付已取消", "Payment cancelled")); finish(); void load(); });
    }
  }, [params, id]);

  if (error) return <div className="page"><Back /><p className="banner">{error}</p></div>;
  if (!activity || !meta || !user) return <div className="page"><div className="skeleton tall" /></div>;

  const today = meta.today;
  const paidPeople = activity.participants.filter((person) => person.status === "Paid");
  const waiting = activity.participants.filter((person) => person.status === "Waitlist");
  const shown = expanded ? paidPeople : paidPeople.slice(0, 5);
  const mine = chargeFor(activity.price, activity.womenDiscount, user.gender);
  const outOfRange = user.skillLevel < activity.skillMin || user.skillLevel > activity.skillMax;
  const locked = refundLocked(activity.startAt, activity.refundPolicy);
  const transferCode = params.get("transfer");

  async function join() {
    setBusy(true);
    try {
      const result = await api<JoinResult>(`/api/activities/${activity!.id}/join`, { method: "POST" });
      if (result.checkoutUrl) {
        window.location.href = result.checkoutUrl;
        return;
      }
      if (result.status === "Waitlist") {
        toast(tx(`已加入候补，当前第 ${result.waitlistPosition} 位`, `You're on the waitlist, #${result.waitlistPosition}`));
        await load();
        return;
      }
      setPay(result);
    } catch (reason) {
      toast(reason instanceof Error ? reason.message : tx("报名没有成功", "Could not join"));
    } finally {
      setBusy(false);
    }
  }

  async function confirmDemo() {
    if (!pay) return;
    setBusy(true);
    try {
      await api("/api/payments/demo", { method: "POST", body: JSON.stringify({ registrationId: pay.registrationId }) });
      setPay(null);
      toast(tx("报名成功", "You're in"));
      await load();
      await reload();
    } catch (reason) {
      toast(reason instanceof Error ? reason.message : tx("支付没有完成", "Payment failed"));
    } finally {
      setBusy(false);
    }
  }

  async function cancel() {
    if (!activity?.myRegistrationId) return;
    setBusy(true);
    try {
      await api(`/api/registrations/${activity.myRegistrationId}/cancel`, { method: "POST" });
      toast(tx("名额已取消", "Spot cancelled"));
      await load();
      await reload();
    } catch (reason) {
      toast(reason instanceof Error ? reason.message : tx("取消失败", "Could not cancel"));
    } finally {
      setBusy(false);
    }
  }

  async function makeTransfer() {
    if (!activity?.myRegistrationId) return;
    setBusy(true);
    try {
      const result = await api<TransferResult>(`/api/registrations/${activity.myRegistrationId}/transfer`, { method: "POST" });
      setTransfer(result);
    } catch (reason) {
      toast(reason instanceof Error ? reason.message : tx("没能生成转让", "Could not transfer"));
    } finally {
      setBusy(false);
    }
  }

  async function claim() {
    if (!transferCode) return;
    setBusy(true);
    try {
      await api(`/api/transfers/${transferCode}/claim`, { method: "POST" });
      toast(tx("已接受这个名额", "Spot claimed"));
      setParams({}, { replace: true });
      await load();
      await reload();
    } catch (reason) {
      toast(reason instanceof Error ? reason.message : tx("没能接受转让", "Could not claim"));
    } finally {
      setBusy(false);
    }
  }

  async function share() {
    const heading = titleOf(activity!, lang);
    const text = `${heading}\n${activity!.venue.name}\n${whenLine(activity!.startAt, activity!.endAt, today, lang)}\n${nzd(activity!.price)} / ${tx("人", "person")}`;
    const url = window.location.href.split("?")[0] ?? window.location.href;
    if (navigator.share) {
      try {
        await navigator.share({ title: heading, text, url });
        return;
      } catch {
        return;
      }
    }
    await copyText(`${text}\n${url}`);
    toast(tx("分享内容已复制", "Share text copied"));
  }

  return (
    <div className="page">
      <div className="spread">
        <Back />
        <button type="button" className="back" onClick={() => void share()}>{tx("分享到群", "Share")}</button>
      </div>
      <p className="kicker">{nzd(activity.price)} / {tx("人", "person")}</p>
      <div className="tags">
        {activity.courtsBooked && <Tag>{tx("已订场", "Court booked")}</Tag>}
        {activity.courtsUsed != null && <Tag tone="line">{tx(`${activity.courtsUsed} 片`, `${activity.courtsUsed} courts`)}</Tag>}
        {activity.womenFriendly && <Tag>{tx("女生友好", "Women friendly")}</Tag>}
        {activity.welcomeNewcomers && <Tag>{tx("欢迎新人", "Newcomers welcome")}</Tag>}
        {activity.playersPerCourt != null && <Tag tone="line">{tx(`每片 ${activity.playersPerCourt} 人轮换`, `${activity.playersPerCourt} per court`)}</Tag>}
        {activity.competitive && <Tag tone="amber">{tx("对抗局", "Competitive")}</Tag>}
        {activity.casual && <Tag tone="line">{tx("养生局", "Easy pace")}</Tag>}
        {activity.parkingFree && <Tag tone="line">{tx("停车免费", "Free parking")}</Tag>}
      </div>
      <h1>{titleOf(activity, lang)}{activity.parkingFree ? tx(" · 停车免费", " · free parking") : ""}</h1>
      <p className="place"><Link to={`/venues/${activity.venue.id}`}>{activity.venue.name}</Link> · {activity.venue.address}</p>
      <p className="when">{whenLine(activity.startAt, activity.endAt, today, lang)} · {tx("水平", "Level")} {level(activity.skillMin)}–{level(activity.skillMax)}</p>
      <div className="spread spots">
        <strong>{tx(`已报名 ${activity.joined}/${activity.capacity}`, `${activity.joined}/${activity.capacity} joined`)}</strong>
        <span>{tx(`男 ${activity.maleCount} 女 ${activity.femaleCount}`, `${activity.maleCount} M · ${activity.femaleCount} F`)}</span>
      </div>
      <div className={`bar ${activity.spotsLeft === 0 ? "full" : ""}`}><span style={{ width: `${Math.min(100, (activity.joined / activity.capacity) * 100)}%` }} /></div>

      {transferCode && activity.myStatus !== "Paid" && (
        <div className="banner">
          <p>{tx("有人把这场的名额转让出来。接受后不需要再付款。", "Someone is transferring a paid spot. Claiming it is free.")}</p>
          <button type="button" className="btn" disabled={busy} onClick={() => void claim()}>{tx("接受名额", "Claim spot")}</button>
        </div>
      )}

      <section className="people">
        {shown.map((person) => (
          <div className="person" key={person.userId}>
            <Avatar text={person.name} color={person.isOrganizer ? activity.club.color : "#60746a"} />
            <div>
              <strong>{person.name}</strong>
              <small>
                {person.isOrganizer && tx("组织者 · ", "Host · ")}
                {genderText(person.gender, lang)} {level(person.skillLevel)}
                {person.isNewcomer && tx(" · 新人", " · new")}
              </small>
            </div>
          </div>
        ))}
        {paidPeople.length > 5 && (
          <button type="button" className="more" onClick={() => setExpanded((value) => !value)}>
            {expanded ? tx("收起", "Show less") : tx(`展开全部 ${paidPeople.length} 人`, `Show all ${paidPeople.length}`)}
          </button>
        )}
        {waiting.length > 0 && <p className="muted">{tx(`候补 ${waiting.length} 人`, `${waiting.length} on the waitlist`)} · {waiting.map((person) => person.name).join("、")}</p>}
      </section>

      <Link className="club-card" to={`/clubs/${activity.club.id}`}>
        <Avatar text={activity.club.monogram} url={activity.club.avatarUrl} color={activity.club.color} size={48} />
        <div>
          <strong>{activity.club.name}</strong>
          <p>{tx(`已办 ${activity.club.eventsHosted} 场`, `${activity.club.eventsHosted} hosted`)} · {tx("评分", "Rating")} {activity.club.rating.toFixed(1)} · {tx("回头率", "Return rate")} {Math.round(activity.club.returnRate * 100)}%</p>
        </div>
      </Link>

      <ul className="facts">
        {activity.courtNumbers && <li><span>{tx("场地", "Courts")}</span><strong>{activity.courtNumbers} {tx("号场（已付场地费）", "(court fee paid)")}</strong></li>}
        <li><span>{tx("用球", "Shuttle")}</span><strong>{activity.shuttlecock || tx("组织者还没填写", "Not listed yet")}</strong></li>
        <li><span>{tx("退款", "Refund")}</span><strong>{refundText(activity.refundPolicy, lang)}</strong></li>
        <li><span>{tx("时间", "Time")}</span><strong>{clock(activity.startAt)}–{clock(activity.endAt)}</strong></li>
      </ul>

      {activity.status === "Draft" ? (
        <div className="dock">
          <div><strong>{tx("这是草稿", "This is a draft")}</strong></div>
          <Link className="btn" to={`/publish?id=${activity.id}`}>{tx("继续编辑", "Continue editing")}</Link>
        </div>
      ) : (
        <div className="dock">
          <div>
            <strong>
              {activity.myStatus === "Paid" && tx("你已报名", "You're in")}
              {activity.myStatus === "Waitlist" && tx("你在候补里", "You're waitlisted")}
              {activity.myStatus === "PendingPayment" && tx("还有一笔未完成的支付", "Payment still open")}
              {!activity.myStatus && (activity.spotsLeft > 0 ? tx(`剩余 ${activity.spotsLeft} 个名额`, `${activity.spotsLeft} spots left`) : tx("已满，可以候补", "Full, waitlist is open"))}
            </strong>
            <p className="muted">{meta.stripeEnabled ? tx("通过 Stripe 安全支付", "Pay securely with Stripe") : tx("演示支付，不会真实扣款", "Demo payment, no real charge")}</p>
          </div>
          <div className="row">
            {activity.myStatus === "Paid" && (
              <>
                <button type="button" className="btn ghost" disabled={busy} onClick={() => void makeTransfer()}>{tx("转让名额", "Transfer")}</button>
                <button type="button" className="btn ghost" disabled={busy || locked} onClick={() => void cancel()}>{tx("取消报名", "Cancel")}</button>
              </>
            )}
            {activity.myStatus === "Waitlist" && <button type="button" className="btn ghost" disabled={busy} onClick={() => void cancel()}>{tx("退出候补", "Leave waitlist")}</button>}
            {(!activity.myStatus || activity.myStatus === "PendingPayment") && (
              <button type="button" className="btn" disabled={busy || outOfRange} onClick={() => void join()}>
                {activity.spotsLeft > 0 ? tx(`立即报名 · ${nzd(mine)}`, `Join · ${nzd(mine)}`) : tx("加入候补", "Join waitlist")}
              </button>
            )}
          </div>
        </div>
      )}
      {outOfRange && !activity.myStatus && <p className="banner">{tx(`这场是 ${level(activity.skillMin)}–${level(activity.skillMax)}，你现在是 ${level(user.skillLevel)}。可以在「我的」里改演示水平。`, `This session is ${level(activity.skillMin)}–${level(activity.skillMax)}. You are ${level(user.skillLevel)}. Adjust it in Me.`)}</p>}
      {activity.myStatus === "Paid" && locked && <p className="muted">{refundText(activity.refundPolicy, lang)}</p>}
      {activity.fromTransfer && <p className="muted">{tx("你这个名额来自转让。", "This spot was transferred to you.")}</p>}

      {pay && (
        <div className="sheet-back" onClick={() => setPay(null)}>
          <div className="sheet" onClick={(event) => event.stopPropagation()}>
            <h2>{tx("确认报名", "Confirm")}</h2>
            <p>{titleOf(activity, lang)}</p>
            <p className="price">{nzd(pay.amount)}</p>
            {user.gender === "Female" && activity.womenDiscount > 0 && <p className="muted">{tx(`女士优惠已减 ${nzd(activity.womenDiscount)}`, `Women's discount ${nzd(activity.womenDiscount)} applied`)}</p>}
            <p className="muted">{meta.stripeEnabled ? tx("接下来会跳到 Stripe。", "You'll continue on Stripe.") : tx("当前没有配置 Stripe 密钥，这笔只会记在本地数据库，不会扣银行卡。", "Stripe is not configured. This only writes a local record and does not charge a card.")}</p>
            <button type="button" className="btn block" disabled={busy} onClick={() => void confirmDemo()}>{tx(`确认支付 ${nzd(pay.amount)}`, `Pay ${nzd(pay.amount)}`)}</button>
          </div>
        </div>
      )}
      {transfer && (
        <div className="sheet-back" onClick={() => setTransfer(null)}>
          <div className="sheet" onClick={(event) => event.stopPropagation()}>
            <h2>{tx("转让名额", "Transfer spot")}</h2>
            <p>{tx("把链接发给对方。对方接受后，你的名额会转过去，对方不用再付款。", "Send this link. Once they accept, the spot moves and they are not charged again.")}</p>
            <code className="code">{transfer.url}</code>
            <button type="button" className="btn block" onClick={() => void copyText(transfer.url).then(() => toast(tx("链接已复制", "Link copied")))}>{tx("复制链接", "Copy link")}</button>
          </div>
        </div>
      )}
    </div>
  );
}
