export type Sport = "Badminton" | "Pickleball" | "Tennis" | "TableTennis";
export type Gender = "Female" | "Male" | "Other";

export interface Meta {
  city: string;
  currency: string;
  platformFeePercent: number;
  stripeEnabled: boolean;
  stripePublishableKey: string | null;
  paymentMode: "stripe" | "demo";
  today: string;
  timezone: string;
  suggestedDate: string;
}

export interface User {
  id: string;
  name: string;
  gender: Gender;
  skillLevel: number;
  isNewcomer: boolean;
  avatarUrl: string | null;
  listed: boolean;
  hint: string | null;
  hintEn: string | null;
}

export interface VenueBrief {
  id: string;
  name: string;
  address: string;
  suburb: string;
  area: string;
  distanceKm: number;
  mapX: number;
  mapY: number;
}

export interface ClubBrief {
  id: string;
  name: string;
  monogram: string;
  color: string;
  avatarUrl: string | null;
  eventsHosted: number;
  rating: number;
  returnRate: number;
}

export interface Participant {
  userId: string;
  name: string;
  gender: Gender;
  skillLevel: number;
  isNewcomer: boolean;
  isOrganizer: boolean;
  status: string;
}

export interface Activity {
  id: string;
  title: string;
  titleEn: string;
  sport: Sport;
  status: string;
  startAt: string;
  endAt: string;
  capacity: number;
  joined: number;
  maleCount: number;
  femaleCount: number;
  waitlist: number;
  spotsLeft: number;
  price: number;
  womenDiscount: number;
  skillMin: number;
  skillMax: number;
  courtsBooked: boolean;
  courtsUsed: number | null;
  playersPerCourt: number | null;
  courtNumbers: string | null;
  shuttlecock: string | null;
  refundPolicy: string;
  parkingFree: boolean;
  womenFriendly: boolean;
  welcomeNewcomers: boolean;
  competitive: boolean;
  casual: boolean;
  venue: VenueBrief;
  club: ClubBrief;
  myStatus: string | null;
  myRegistrationId: string | null;
  fromTransfer: boolean;
  participants: Participant[];
}

export interface SessionBrief {
  id: string;
  title: string;
  titleEn: string;
  sport: Sport;
  startAt: string;
  endAt: string;
  joined: number;
  capacity: number;
  spotsLeft: number;
  price: number;
  skillMin: number;
  skillMax: number;
}

export interface Venue {
  id: string;
  name: string;
  address: string;
  suburb: string;
  area: string;
  distanceKm: number;
  latitude: number;
  longitude: number;
  mapX: number;
  mapY: number;
  courtCount: number;
  hourlyRate: number | null;
  walkInBooking: boolean;
  isOpen: boolean;
  opensAt: string;
  closesAt: string;
  sports: Sport[];
  sessionsOnDate: number;
  sessions: SessionBrief[];
}

export interface AreaCount {
  area: string;
  sessions: number;
  walkIn: boolean;
}

export interface VenueList {
  areas: AreaCount[];
  venues: Venue[];
}

export interface Club {
  id: string;
  name: string;
  bio: string | null;
  sport: Sport;
  monogram: string;
  color: string;
  avatarUrl: string | null;
  ownerId: string;
  ownerName: string;
  homeVenueId: string | null;
  homeVenueName: string | null;
  eventsHosted: number;
  rating: number;
  returnRate: number;
  stripeConnected: boolean;
  stripeDemo: boolean;
  mine: boolean;
  upcoming: SessionBrief[];
}

export interface MyClub {
  id: string;
  name: string;
  sport: Sport;
  monogram: string;
  color: string;
  avatarUrl: string | null;
  stripeConnected: boolean;
  stripeDemo: boolean;
  eventsHosted: number;
  rating: number;
}

export interface MyRegistration {
  id: string;
  activityId: string;
  activityTitle: string;
  activityTitleEn: string;
  startAt: string;
  venueName: string;
  status: string;
  amount: number;
  fromTransfer: boolean;
  sport: Sport;
}

export interface Me {
  user: User;
  clubs: MyClub[];
  registrations: MyRegistration[];
  drafts: Activity[];
}

export interface JoinResult {
  registrationId: string;
  status: string;
  amount: number;
  waitlistPosition: number | null;
  paymentMode: string;
  checkoutUrl: string | null;
}

export interface TransferResult {
  code: string;
  url: string;
}

export interface SaveActivityResult {
  activity: Activity;
  shareUrl: string;
  estimatedGross: number;
  platformFee: number;
  estimatedNet: number;
}

export interface ConnectResult {
  mode: string;
  url: string | null;
  connected: boolean;
}

export interface DayCount {
  date: string;
  count: number;
}
