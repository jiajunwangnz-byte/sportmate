import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { api } from "../api.ts";
import { Avatar } from "../components.tsx";
import { aucklandDate, level, monthDay, nzd, sportName, statusLabel, titleOf } from "../format.ts";
import { useApp } from "../state.tsx";
import type { Gender, Me } from "../types.ts";

export function MePage() {
  const { user, users, lang, tx, toast, reload, switchUser, meta } = useApp();
  const [me, setMe] = useState<Me | null>(null);
  const [name, setName] = useState(user?.name ?? "");
  const [skill, setSkill] = useState(user?.skillLevel ?? 3);
  const [gender, setGender] = useState<Gender>(user?.gender ?? "Female");
  const [busy, setBusy] = useState(false);

  useEffect(() => {
    if (!user) return;
    setName(user.name);
    setSkill(user.skillLevel);
    setGender(user.gender);
    api<Me>("/api/me").then(setMe).catch((reason: Error) => toast(reason.message));
  }, [user?.id]);

  async function save() {
    setBusy(true);
    try {
      await api("/api/me", { method: "PATCH", body: JSON.stringify({ name, skillLevel: skill, gender }) });
      await reload();
      toast(tx("资料已更新", "Profile updated"));
    } catch (reason) {
      toast(reason instanceof Error ? reason.message : tx("没有保存", "Could not save"));
    } finally {
      setBusy(false);
    }
  }

  if (!user || !meta) return null;

  return (
    <div className="page">
      <h1>{tx("我的", "Me")}</h1>
      <section className="card">
        <div className="club-row">
          <Avatar text={name || user.name} size={64} color="#0e7a45" />
          <div>
            <strong>{user.name}</strong>
            <p className="muted">{level(user.skillLevel)} · {user.hint ? (lang === "en" ? user.hintEn : user.hint) : ""}</p>
          </div>
        </div>
        <label className="field"><span>{tx("名字", "Name")}</span><input value={name} onChange={(event) => setName(event.target.value)} /></label>
        <label className="field">
          <span>{tx("性别", "Gender")}</span>
          <select value={gender} onChange={(event) => setGender(event.target.value as Gender)}>
            <option value="Female">{tx("女", "Female")}</option>
            <option value="Male">{tx("男", "Male")}</option>
            <option value="Other">{tx("其他", "Other")}</option>
          </select>
        </label>
        <label className="field">
          <span>{tx("水平", "Level")} {level(skill)}</span>
          <input type="range" min="1" max="8" step="0.5" value={skill} onChange={(event) => setSkill(Number(event.target.value))} />
        </label>
        <button type="button" className="btn" disabled={busy} onClick={() => void save()}>{tx("保存资料", "Save profile")}</button>
      </section>

      <section>
        <h2>{tx("切换演示账号", "Switch demo account")}</h2>
        <div className="stack">
          {users.map((item) => (
            <button key={item.id} type="button" className={item.id === user.id ? "card account on" : "card account"} onClick={() => void switchUser(item.id)}>
              <strong>{item.name}</strong>
              <span>{level(item.skillLevel)} · {lang === "en" ? item.hintEn : item.hint}</span>
            </button>
          ))}
        </div>
      </section>

      <section>
        <h2>{tx("我的报名", "My sessions")}</h2>
        <div className="stack">
          {me?.registrations.map((item) => (
            <Link key={item.id} to={`/activities/${item.activityId}`} className="card compact">
              <div className="spread">
                <strong>{titleOf({ title: item.activityTitle, titleEn: item.activityTitleEn }, lang)}</strong>
                <span>{statusLabel(item.status, lang)}</span>
              </div>
              <p className="muted">{monthDay(aucklandDate(item.startAt), lang)} · {item.venueName} · {nzd(item.amount)} · {sportName(item.sport, lang)}</p>
            </Link>
          ))}
          {me && me.registrations.length === 0 && <p className="muted">{tx("还没有报名。", "You haven't joined a session yet.")}</p>}
        </div>
      </section>

      <section>
        <div className="spread"><h2>{tx("我的俱乐部", "My clubs")}</h2><Link to="/clubs/new">{tx("新建", "New")}</Link></div>
        <div className="stack">
          {me?.clubs.map((club) => (
            <Link key={club.id} to={`/clubs/${club.id}`} className="card compact club-row">
              <Avatar text={club.monogram} url={club.avatarUrl} color={club.color} />
              <div>
                <strong>{club.name}</strong>
                <p className="muted">{sportName(club.sport, lang)} · {club.stripeConnected ? tx("收款已连接", "Payouts connected") : tx("收款未连接", "Payouts not connected")}</p>
              </div>
            </Link>
          ))}
          {me && me.clubs.length === 0 && <p className="muted">{tx("你还没有俱乐部。", "You don't have a club yet.")}</p>}
        </div>
      </section>

      <section>
        <h2>{tx("草稿", "Drafts")}</h2>
        <div className="stack">
          {me?.drafts.map((draft) => (
            <Link key={draft.id} to={`/publish?id=${draft.id}`} className="card compact">
              <strong>{draft.title || tx("未命名草稿", "Untitled draft")}</strong>
            </Link>
          ))}
          {me && me.drafts.length === 0 && <p className="muted">{tx("没有草稿。", "No drafts.")}</p>}
        </div>
      </section>

      <section className="card compact">
        <strong>{tx("支付", "Payments")}</strong>
        <p className="muted">
          {meta.stripeEnabled
            ? tx("Stripe 已配置，报名会跳转到 Stripe Checkout。", "Stripe is configured. Joining opens Stripe Checkout.")
            : tx("还没有配置 Stripe。报名使用演示支付，只写入数据库，不会扣款。启动 API 前设置 Stripe__SecretKey 和 Stripe__WebhookSecret 即可切到真实支付。", "Stripe is not configured. Join uses a demo payment that only writes to the database. Set Stripe__SecretKey and Stripe__WebhookSecret before starting the API to turn on real charges.")}
        </p>
      </section>
    </div>
  );
}
