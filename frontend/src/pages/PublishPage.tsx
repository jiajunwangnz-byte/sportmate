import { useEffect, useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { api } from "../api.ts";
import { Back, Empty, SkillScale } from "../components.tsx";
import { aucklandDate, clock, copyText, moneyRound, nzd, sportName } from "../format.ts";
import { useApp } from "../state.tsx";
import type { Activity, Me, MyClub, SaveActivityResult, Venue, VenueList } from "../types.ts";

interface DraftForm {
  clubId: string;
  venueId: string;
  title: string;
  titleEn: string;
  date: string;
  startTime: string;
  endTime: string;
  capacity: number;
  price: number;
  womenDiscount: number;
  skillMin: number;
  skillMax: number;
  courtsBooked: boolean;
  courtsUsed: string;
  playersPerCourt: string;
  courtNumbers: string;
  shuttlecock: string;
  refundPolicy: string;
  parkingFree: boolean;
  womenFriendly: boolean;
  welcomeNewcomers: boolean;
  competitive: boolean;
  casual: boolean;
}

const EMPTY: DraftForm = {
  clubId: "",
  venueId: "",
  title: "",
  titleEn: "",
  date: "",
  startTime: "18:00",
  endTime: "21:00",
  capacity: 24,
  price: 15,
  womenDiscount: 0,
  skillMin: 2,
  skillMax: 5,
  courtsBooked: true,
  courtsUsed: "3",
  playersPerCourt: "6",
  courtNumbers: "",
  shuttlecock: "",
  refundPolicy: "h24",
  parkingFree: false,
  womenFriendly: true,
  welcomeNewcomers: true,
  competitive: false,
  casual: false,
};

function fromActivity(activity: Activity): DraftForm {
  return {
    ...EMPTY,
    clubId: activity.club.id,
    venueId: activity.venue.id,
    title: activity.title,
    titleEn: activity.titleEn,
    date: aucklandDate(activity.startAt),
    startTime: clock(activity.startAt),
    endTime: clock(activity.endAt),
    capacity: activity.capacity,
    price: activity.price,
    womenDiscount: activity.womenDiscount,
    skillMin: activity.skillMin,
    skillMax: activity.skillMax,
    courtsBooked: activity.courtsBooked,
    courtsUsed: activity.courtsUsed?.toString() ?? "",
    playersPerCourt: activity.playersPerCourt?.toString() ?? "",
    courtNumbers: activity.courtNumbers ?? "",
    shuttlecock: activity.shuttlecock ?? "",
    refundPolicy: activity.refundPolicy,
    parkingFree: activity.parkingFree,
    womenFriendly: activity.womenFriendly,
    welcomeNewcomers: activity.welcomeNewcomers,
    competitive: activity.competitive,
    casual: activity.casual,
  };
}

function optionalNumber(value: string) {
  if (!value.trim()) return null;
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : null;
}

export function PublishPage() {
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const { meta, lang, tx, toast } = useApp();
  const [clubs, setClubs] = useState<MyClub[]>([]);
  const [venues, setVenues] = useState<Venue[]>([]);
  const [form, setForm] = useState<DraftForm>(EMPTY);
  const [saved, setSaved] = useState<SaveActivityResult | null>(null);
  const [ready, setReady] = useState(false);
  const [busy, setBusy] = useState(false);
  const draftId = params.get("id");

  useEffect(() => {
    let alive = true;
    Promise.all([api<Me>("/api/me"), api<VenueList>("/api/venues")]).then(async ([me, list]) => {
      if (!alive) return;
      setClubs(me.clubs);
      setVenues(list.venues);
      if (draftId) {
        const draft = await api<Activity>(`/api/activities/${draftId}`);
        if (alive) setForm(fromActivity(draft));
        if (alive) setReady(true);
        return;
      }
      const clubId = params.get("clubId");
      const venueId = params.get("venueId");
      setForm((current) => ({
        ...current,
        clubId: me.clubs.find((club) => club.id === clubId)?.id ?? me.clubs[0]?.id ?? "",
        venueId: list.venues.find((venue) => venue.id === venueId)?.id ?? "",
        date: meta?.suggestedDate ?? current.date,
      }));
      if (alive) setReady(true);
    }).catch((reason: Error) => { toast(reason.message); setReady(true); });
    return () => { alive = false; };
  }, [draftId]);

  useEffect(() => {
    if (meta && !form.date) setForm((current) => ({ ...current, date: meta.suggestedDate }));
  }, [meta, form.date]);

  function patch(partial: Partial<DraftForm>) {
    setForm((current) => ({ ...current, ...partial }));
  }

  const club = clubs.find((item) => item.id === form.clubId);
  const venueChoices = venues.filter((venue) => !club || venue.sports.includes(club.sport));
  const percent = meta?.platformFeePercent ?? 8;
  const gross = moneyRound(form.capacity * form.price);
  const fee = moneyRound(gross * percent / 100);
  const net = moneyRound(gross - fee);

  async function save(publish: boolean) {
    setBusy(true);
    try {
      const body = {
        ...form,
        courtsUsed: optionalNumber(form.courtsUsed),
        playersPerCourt: optionalNumber(form.playersPerCourt),
        publish,
      };
      const path = draftId ? `/api/activities/${draftId}` : "/api/activities";
      const result = await api<SaveActivityResult>(path, { method: draftId ? "PUT" : "POST", body: JSON.stringify(body) });
      if (publish) setSaved(result);
      else {
        toast(tx("草稿已保存", "Draft saved"));
        navigate("/me");
      }
    } catch (reason) {
      toast(reason instanceof Error ? reason.message : tx("没有保存成功", "Could not save"));
    } finally {
      setBusy(false);
    }
  }

  async function removeDraft() {
    if (!draftId) return;
    await api(`/api/activities/${draftId}`, { method: "DELETE" });
    toast(tx("草稿已删除", "Draft deleted"));
    navigate("/me");
  }

  if (!ready) return <div className="page"><div className="skeleton tall" /></div>;
  if (clubs.length === 0) {
    return (
      <div className="page">
        <Back />
        <Empty title={tx("先建一个俱乐部", "Create a club first")} body={tx("活动挂在俱乐部下面，报名费也进俱乐部的收款账户。", "Sessions belong to a club, and the fees settle to that club.")} action={<Link className="btn" to="/clubs/new">{tx("创建俱乐部", "Create club")}</Link>} />
      </div>
    );
  }

  if (saved) {
    return (
      <div className="page">
        <h1>{tx("发布成功", "Published")}</h1>
        <p>{tx("把链接发到群里，别人打开就能报名。", "Send the link to a group chat. People can join from it.")}</p>
        <code className="code">{saved.shareUrl}</code>
        <p className="muted">{tx(`报满预计收入 ${nzd(saved.estimatedGross)} · 平台服务费 ${nzd(saved.platformFee)} · 到手 ${nzd(saved.estimatedNet)}`, `Full session ${nzd(saved.estimatedGross)} · fee ${nzd(saved.platformFee)} · you receive ${nzd(saved.estimatedNet)}`)}</p>
        <div className="row">
          <button type="button" className="btn ghost" onClick={() => void copyText(saved.shareUrl).then(() => toast(tx("链接已复制", "Link copied")))}>{tx("复制链接", "Copy link")}</button>
          <Link className="btn" to={`/activities/${saved.activity.id}`}>{tx("查看活动", "View session")}</Link>
        </div>
      </div>
    );
  }

  return (
    <div className="page">
      <div className="spread">
        <Back />
        <button type="button" className="back" disabled={busy} onClick={() => void save(false)}>{tx("存草稿", "Save draft")}</button>
      </div>
      <h1>{tx("发布活动", "Publish a session")}</h1>
      <form className="form" onSubmit={(event) => { event.preventDefault(); void save(true); }}>
        <label className="field">
          <span>{tx("俱乐部", "Club")}</span>
          <select value={form.clubId} onChange={(event) => patch({ clubId: event.target.value })}>
            {clubs.map((item) => <option key={item.id} value={item.id}>{item.name}</option>)}
          </select>
          {club && <small className="muted">{sportName(club.sport, lang)}</small>}
        </label>
        <label className="field">
          <span>{tx("标题", "Title")}</span>
          <input value={form.title} onChange={(event) => patch({ title: event.target.value })} placeholder={tx("例如：周日晚双打畅打 · 新手友好", "e.g. Sunday night doubles")} />
        </label>
        <label className="field">
          <span>{tx("英文标题", "English title")}</span>
          <input value={form.titleEn} onChange={(event) => patch({ titleEn: event.target.value })} />
        </label>
        <label className="field">
          <span>{tx("场馆", "Venue")}</span>
          <select value={form.venueId} onChange={(event) => patch({ venueId: event.target.value })}>
            <option value="">{tx("选择场馆", "Choose a venue")}</option>
            {venueChoices.map((venue) => <option key={venue.id} value={venue.id}>{venue.name}</option>)}
          </select>
        </label>
        <div className="grid-3">
          <label className="field"><span>{tx("日期", "Date")}</span><input type="date" value={form.date} onChange={(event) => patch({ date: event.target.value })} /></label>
          <label className="field"><span>{tx("开始", "Start")}</span><input type="time" value={form.startTime} onChange={(event) => patch({ startTime: event.target.value })} /></label>
          <label className="field"><span>{tx("结束", "End")}</span><input type="time" value={form.endTime} onChange={(event) => patch({ endTime: event.target.value })} /></label>
        </div>
        <label className="check">
          <input type="checkbox" checked={form.courtsBooked} onChange={(event) => patch({ courtsBooked: event.target.checked })} />
          <span>{tx("已订场地", "Court already booked")}</span>
        </label>
        <div className="grid-3">
          <label className="field"><span>{tx("场地号", "Court numbers")}</span><input value={form.courtNumbers} onChange={(event) => patch({ courtNumbers: event.target.value })} placeholder="5、6、7" /></label>
          <label className="field"><span>{tx("几片", "Courts")}</span><input value={form.courtsUsed} onChange={(event) => patch({ courtsUsed: event.target.value })} /></label>
          <label className="field"><span>{tx("每片人数", "Players per court")}</span><input value={form.playersPerCourt} onChange={(event) => patch({ playersPerCourt: event.target.value })} /></label>
        </div>
        <label className="field">
          <span>{tx("用球", "Shuttle")}</span>
          <input value={form.shuttlecock} onChange={(event) => patch({ shuttlecock: event.target.value })} placeholder="Victor Master Ace" />
        </label>
        <div className="field">
          <span>{tx("人数上限", "Capacity")}</span>
          <div className="stepper">
            <button type="button" onClick={() => patch({ capacity: Math.max(2, form.capacity - 1) })}>−</button>
            <strong>{form.capacity}</strong>
            <button type="button" onClick={() => patch({ capacity: Math.min(80, form.capacity + 1) })}>+</button>
          </div>
        </div>
        <div className="grid-3">
          <label className="field"><span>{tx("每人费用 NZ$", "Price per person NZ$")}</span><input type="number" min="0" step="1" value={form.price} onChange={(event) => patch({ price: Number(event.target.value) })} /></label>
          <label className="field"><span>{tx("女士优惠，每人减 NZ$", "Women's discount NZ$")}</span><input type="number" min="0" step="1" value={form.womenDiscount} onChange={(event) => patch({ womenDiscount: Number(event.target.value) })} /></label>
        </div>
        <p className="estimate">{tx(`报满预计收入 ${nzd(gross)} · 平台服务费 ${percent}% · 到手 ${nzd(net)}`, `If full: ${nzd(gross)} · platform fee ${percent}% · you receive ${nzd(net)}`)}</p>
        <p className="muted">{tx("预计收入按标价计算，女士优惠会在实际报名时扣减。", "The estimate uses the listed price. The women's discount is applied when someone joins.")}</p>
        <div className="field">
          <span>{tx("水平范围", "Level")} L{form.skillMin} – L{form.skillMax}</span>
          <SkillScale min={form.skillMin} max={form.skillMax} onChange={(skillMin, skillMax) => patch({ skillMin, skillMax })} />
        </div>
        <div className="field">
          <span>{tx("标签", "Tags")}</span>
          <div className="filters">
            <button type="button" className={form.womenFriendly ? "chip on" : "chip"} onClick={() => patch({ womenFriendly: !form.womenFriendly })}>{tx("女生友好", "Women friendly")}</button>
            <button type="button" className={form.welcomeNewcomers ? "chip on" : "chip"} onClick={() => patch({ welcomeNewcomers: !form.welcomeNewcomers })}>{tx("欢迎新人", "Newcomers")}</button>
            <button type="button" className={form.competitive ? "chip on" : "chip"} onClick={() => patch({ competitive: !form.competitive })}>{tx("对抗局", "Competitive")}</button>
            <button type="button" className={form.casual ? "chip on" : "chip"} onClick={() => patch({ casual: !form.casual })}>{tx("养生局", "Easy pace")}</button>
            <button type="button" className={form.parkingFree ? "chip on" : "chip"} onClick={() => patch({ parkingFree: !form.parkingFree })}>{tx("停车免费", "Free parking")}</button>
          </div>
        </div>
        <label className="field">
          <span>{tx("退款规则", "Refund rule")}</span>
          <select value={form.refundPolicy} onChange={(event) => patch({ refundPolicy: event.target.value })}>
            <option value="h24">{tx("开始前 24 小时内不可退，可转让名额", "No refund within 24 hours, transfer allowed")}</option>
            <option value="h12">{tx("开始前 12 小时内不可退，可转让名额", "No refund within 12 hours, transfer allowed")}</option>
            <option value="none">{tx("不支持退款，可转让名额", "No refunds, transfer allowed")}</option>
          </select>
        </label>
        <button className="btn block" disabled={busy} type="submit">{tx("发布并生成分享链接", "Publish and get a share link")}</button>
        {draftId && <button type="button" className="btn ghost block" disabled={busy} onClick={() => void removeDraft()}>{tx("删除草稿", "Delete draft")}</button>}
      </form>
    </div>
  );
}
