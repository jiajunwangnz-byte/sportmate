import type { Sport } from "./types.ts";

export const SPORTS: Sport[] = ["Badminton", "Pickleball", "Tennis", "TableTennis"];

const WEEK_ZH = ["周日", "周一", "周二", "周三", "周四", "周五", "周六"];
const WEEK_EN = ["Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat"];
const MONTH_EN = ["Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"];

export function sportName(sport: Sport, lang: string) {
  const zh: Record<Sport, string> = { Badminton: "羽毛球", Pickleball: "匹克球", Tennis: "网球", TableTennis: "乒乓球" };
  const en: Record<Sport, string> = { Badminton: "Badminton", Pickleball: "Pickleball", Tennis: "Tennis", TableTennis: "Table tennis" };
  return lang === "en" ? en[sport] : zh[sport];
}

export function nzd(value: number) {
  const rounded = Math.round(value * 100) / 100;
  return Number.isInteger(rounded) ? `NZ$${rounded}` : `NZ$${rounded.toFixed(2)}`;
}

export function level(value: number) {
  return `L${Number.isInteger(value) ? value.toFixed(0) : value}`;
}

export function levelRange(min: number, max: number) {
  return `${level(min)}–${level(max)}`;
}

export function km(value: number) {
  return value < 1 ? `${Math.round(value * 1000)} m` : `${value.toFixed(1)} km`;
}

export function addDays(date: string, days: number) {
  const [year, month, day] = date.split("-").map(Number);
  const next = new Date(Date.UTC(year, month - 1, day));
  next.setUTCDate(next.getUTCDate() + days);
  return next.toISOString().slice(0, 10);
}

export function weekdayIndex(date: string) {
  return new Date(`${date}T12:00:00Z`).getUTCDay();
}

export function weekday(date: string, lang: string) {
  const index = weekdayIndex(date);
  return lang === "en" ? WEEK_EN[index] : WEEK_ZH[index];
}

export function monthDay(date: string, lang: string) {
  const month = Number(date.slice(5, 7));
  const day = Number(date.slice(8, 10));
  return lang === "en" ? `${day} ${MONTH_EN[month - 1]}` : `${month}月${day}日`;
}

export function aucklandDate(iso: string) {
  return new Intl.DateTimeFormat("en-CA", {
    timeZone: "Pacific/Auckland",
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).format(new Date(iso));
}

export function clock(iso: string) {
  return new Intl.DateTimeFormat("en-GB", {
    timeZone: "Pacific/Auckland",
    hour: "2-digit",
    minute: "2-digit",
    hourCycle: "h23",
  }).format(new Date(iso));
}

export function whenLine(start: string, end: string, today: string, lang: string) {
  const date = aucklandDate(start);
  const prefix = date === today ? (lang === "en" ? "Today" : "今天") : monthDay(date, lang);
  return `${prefix} ${weekday(date, lang)} ${clock(start)}–${clock(end)}`;
}

export function titleOf(item: { title: string; titleEn: string }, lang: string) {
  return lang === "en" && item.titleEn ? item.titleEn : item.title;
}

export function genderText(gender: string, lang: string) {
  if (lang === "en") return gender === "Female" ? "F" : gender === "Male" ? "M" : "Other";
  return gender === "Female" ? "女" : gender === "Male" ? "男" : "其他";
}

export function refundText(policy: string, lang: string) {
  if (lang === "en") {
    if (policy === "h12") return "No refund within 12 hours of the start. Spots can be transferred.";
    if (policy === "none") return "No refunds. Spots can be transferred.";
    return "No refund within 24 hours of the start. Spots can be transferred.";
  }
  if (policy === "h12") return "开始前 12 小时内不可退，可转让名额";
  if (policy === "none") return "不支持退款，可转让名额";
  return "开始前 24 小时内不可退，可转让名额";
}

export function refundLocked(startIso: string, policy: string) {
  const hours = (new Date(startIso).getTime() - Date.now()) / 36e5;
  if (policy === "none") return true;
  if (policy === "h12") return hours < 12;
  return hours < 24;
}

export function chargeFor(price: number, discount: number, gender: string) {
  return gender === "Female" ? Math.max(0, price - discount) : price;
}

export function moneyRound(value: number) {
  return Math.round(value * 100) / 100;
}

export async function copyText(text: string) {
  await navigator.clipboard.writeText(text);
}

export function mapsUrl(address: string) {
  return `https://www.google.com/maps/search/?api=1&query=${encodeURIComponent(address)}`;
}

export function statusLabel(status: string, lang: string) {
  const zh: Record<string, string> = {
    Paid: "已报名",
    Waitlist: "候补",
    PendingPayment: "待支付",
    Transferred: "已转让",
    Cancelled: "已取消",
    Draft: "草稿",
    Published: "已发布",
  };
  const en: Record<string, string> = {
    Paid: "Joined",
    Waitlist: "Waitlist",
    PendingPayment: "Awaiting payment",
    Transferred: "Transferred",
    Cancelled: "Cancelled",
    Draft: "Draft",
    Published: "Published",
  };
  return (lang === "en" ? en : zh)[status] ?? status;
}
