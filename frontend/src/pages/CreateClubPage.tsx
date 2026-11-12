import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { api } from "../api.ts";
import { Back } from "../components.tsx";
import { sportName, SPORTS } from "../format.ts";
import { useApp } from "../state.tsx";
import type { Club, Sport, VenueList } from "../types.ts";
import { useEffect } from "react";

const COLORS = ["#0f7a45", "#1d4e89", "#c2412d", "#0e7490", "#8a5a00", "#5b3a8c"];

function monogram(name: string) {
  const trimmed = name.trim();
  if (!trimmed) return "局";
  const latin = trimmed.match(/[A-Za-z]/);
  return (latin?.[0] ?? trimmed[0] ?? "局").toUpperCase();
}

export function CreateClubPage() {
  const navigate = useNavigate();
  const { lang, tx, toast } = useApp();
  const [venues, setVenues] = useState<VenueList["venues"]>([]);
  const [name, setName] = useState("");
  const [bio, setBio] = useState("");
  const [sport, setSport] = useState<Sport>("Badminton");
  const [homeVenueId, setHomeVenueId] = useState("");
  const [mark, setMark] = useState("局");
  const [color, setColor] = useState(COLORS[0]);
  const [avatarUrl, setAvatarUrl] = useState<string | null>(null);
  const [accepted, setAccepted] = useState(false);
  const [busy, setBusy] = useState(false);
  const [created, setCreated] = useState<Club | null>(null);

  useEffect(() => {
    api<VenueList>("/api/venues").then((list) => setVenues(list.venues)).catch(() => setVenues([]));
  }, []);

  async function onFile(file: File | undefined) {
    if (!file) return;
    const bitmap = await createImageBitmap(file);
    const size = 256;
    const canvas = document.createElement("canvas");
    canvas.width = size;
    canvas.height = size;
    const ctx = canvas.getContext("2d");
    if (!ctx) return;
    const scale = Math.max(size / bitmap.width, size / bitmap.height);
    const width = bitmap.width * scale;
    const height = bitmap.height * scale;
    ctx.drawImage(bitmap, (size - width) / 2, (size - height) / 2, width, height);
    const url = canvas.toDataURL("image/jpeg", 0.8);
    if (url.length > 180000) {
      toast(tx("图片还是太大", "That image is still too large"));
      return;
    }
    setAvatarUrl(url);
  }

  async function submit() {
    setBusy(true);
    try {
      const club = await api<Club>("/api/clubs", {
        method: "POST",
        body: JSON.stringify({
          name,
          bio,
          sport,
          homeVenueId: homeVenueId || null,
          monogram: mark,
          color,
          avatarUrl,
          acceptedTerms: accepted,
        }),
      });
      setCreated(club);
    } catch (reason) {
      toast(reason instanceof Error ? reason.message : tx("没有创建成功", "Could not create the club"));
    } finally {
      setBusy(false);
    }
  }

  if (created) {
    return (
      <div className="page">
        <h1>{tx("俱乐部已建好", "Club created")}</h1>
        <p>{tx("下一步连接收款账户，然后发布第一场活动。报名费会进你的账户。", "Next, connect payouts and publish the first session. Fees go to your account.")}</p>
        <div className="row">
          <button type="button" className="btn ghost" onClick={() => navigate(`/clubs/${created.id}`)}>{tx("去连接收款", "Connect payouts")}</button>
          <Link className="btn" to={`/publish?clubId=${created.id}`}>{tx("发布第一场活动", "Publish the first session")}</Link>
        </div>
      </div>
    );
  }

  const choices = venues.filter((venue) => venue.sports.includes(sport));

  return (
    <div className="page">
      <Back fallback="/clubs" />
      <h1>{tx("创建俱乐部", "Create a club")}</h1>
      <p className="muted">{tx("一个人也能办俱乐部", "You can run a club on your own")}</p>
      <ol className="steps">
        <li>{tx("建好俱乐部，连接收款账户", "Create the club and connect payouts")}</li>
        <li>{tx("自己去球馆订好场地", "Book the court yourself")}</li>
        <li>{tx("发布活动，报名费直接进你的账户", "Publish a session. Fees go straight to you")}</li>
      </ol>
      <form className="form" onSubmit={(event) => { event.preventDefault(); void submit(); }}>
        <label className="field">
          <span>{tx("头像", "Avatar")}</span>
          <div className="club-row">
            <span className="avatar xl" style={{ background: color }}>{avatarUrl ? <img src={avatarUrl} alt="" /> : mark}</span>
            <input type="file" accept="image/*" onChange={(event) => void onFile(event.target.files?.[0])} />
          </div>
          <small className="muted">{tx("头像会显示在每一场活动卡片上，建议用俱乐部 logo 或球馆照片。", "This shows on every session card. A logo or a photo of the hall works well.")}</small>
        </label>
        <label className="field">
          <span>{tx("头像文字", "Monogram")}</span>
          <input value={mark} maxLength={2} onChange={(event) => setMark(event.target.value)} />
        </label>
        <div className="swatches">
          {COLORS.map((item) => (
            <button key={item} type="button" className={item === color ? "swatch on" : "swatch"} style={{ background: item }} onClick={() => setColor(item)} aria-label={item} />
          ))}
        </div>
        <label className="field">
          <span>{tx("名称", "Name")}</span>
          <input value={name} onChange={(event) => { setName(event.target.value); if (mark === monogram(name) || mark === "局") setMark(monogram(event.target.value)); }} placeholder={tx("例如 Epsom 周末羽球会", "e.g. Epsom weekend badminton")} />
        </label>
        <label className="field">
          <span>{tx("运动", "Sport")}</span>
          <select value={sport} onChange={(event) => setSport(event.target.value as Sport)}>
            {SPORTS.map((item) => <option key={item} value={item}>{sportName(item, lang)}</option>)}
          </select>
        </label>
        <label className="field">
          <span>{tx("常驻场馆", "Home venue")}</span>
          <select value={homeVenueId} onChange={(event) => setHomeVenueId(event.target.value)}>
            <option value="">{tx("先不选", "Decide later")}</option>
            {choices.map((venue) => <option key={venue.id} value={venue.id}>{venue.name}</option>)}
          </select>
        </label>
        <label className="field">
          <span>{tx("简介", "About")}</span>
          <textarea rows={4} value={bio} onChange={(event) => setBio(event.target.value)} />
        </label>
        <details className="card compact">
          <summary>{tx("《组织者守则》", "Organiser rules")}</summary>
          <ul>
            <li>{tx("按时到场，提前打开场地。", "Show up on time and open the courts early.")}</li>
            <li>{tx("如实描述场地、用球和水平范围。", "Describe the courts, shuttle and level honestly.")}</li>
            <li>{tx("开始前 24 小时内不退款时，协助转让名额。", "If the refund window has closed, help people transfer their spot.")}</li>
            <li>{tx("对新手和女性参与者保持友好。", "Be welcoming to newcomers and women.")}</li>
          </ul>
        </details>
        <label className="check">
          <input type="checkbox" checked={accepted} onChange={(event) => setAccepted(event.target.checked)} />
          <span>{tx("我已阅读并同意《组织者守则》，会按时到场、如实描述场地和水平", "I agree to the organiser rules, and I'll show up and describe the session honestly")}</span>
        </label>
        <button className="btn block" type="submit" disabled={busy || !accepted}>{tx("创建并发布第一场活动", "Create and publish the first session")}</button>
      </form>
    </div>
  );
}
