using SportMate.Api.Infrastructure;
using SportMate.Api.Models;

namespace SportMate.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        if (await db.Users.AnyAsync())
            return;

        var now = DateTimeOffset.UtcNow;
        var today = AucklandTime.Today();
        var sunday = AucklandTime.Next(today, DayOfWeek.Sunday);
        if (AucklandTime.At(sunday, new TimeOnly(18, 0)) <= now.AddMinutes(30))
            sunday = sunday.AddDays(7);

        var venues = BuildVenues();
        var users = BuildUsers(now);
        var clubs = BuildClubs(users, venues, now);
        db.AddRange(venues);
        db.AddRange(users);
        db.AddRange(clubs);
        await db.SaveChangesAsync();

        var activities = BuildActivities(users, venues, clubs, today, sunday, now);
        db.AddRange(activities);
        await db.SaveChangesAsync();
    }

    static Guid Id(char kind, int n) => Guid.Parse($"{kind}0000000-0000-0000-0000-{n:D12}");

    static List<Venue> BuildVenues() =>
    [
        Venue(1, "Auckland Badminton Hall", "99 Gillies Ave, Epsom, Auckland", "Epsom", "Epsom", -36.889, 174.774, 3.2, 54, 58, 12, null, true, "Badminton"),
        Venue(2, "Waitakere Badminton Centre", "Triangle Road, Massey, Auckland", "Massey", "Massey", -36.835, 174.612, 14.8, 18, 46, 8, 28, false, "Badminton"),
        Venue(3, "Lloyd Elsmore Park", "Bells Road, Pakuranga, Auckland", "Pakuranga", "Pakuranga", -36.912, 174.905, 11.5, 78, 64, 6, 24, false, "Badminton"),
        Venue(4, "Badminton North Harbour", "Paul Matthews Road, Rosedale, Auckland", "Rosedale", "North Shore", -36.744, 174.712, 16.4, 58, 22, 10, 32, true, "Badminton"),
        Venue(5, "ASB Tennis Centre", "72 Stanley Street, Parnell, Auckland", "Parnell", "Parnell", -36.856, 174.778, 2.1, 64, 48, 8, 36, false, "Tennis"),
        Venue(6, "Grey Lynn Community Centre", "510 Richmond Road, Grey Lynn, Auckland", "Grey Lynn", "Grey Lynn", -36.858, 174.735, 4.6, 36, 44, 4, 16, true, "Pickleball"),
        Venue(7, "Auckland Table Tennis Centre", "Church Street, Onehunga, Auckland", "Onehunga", "Onehunga", -36.920, 174.785, 8.9, 52, 74, 12, 12, false, "TableTennis")
    ];

    static Venue Venue(int n, string name, string address, string suburb, string area, double lat, double lng, double km, double x, double y, int courts, decimal? hourly, bool walkIn, string sports) => new()
    {
        Id = Id('b', n),
        Name = name,
        Address = address,
        Suburb = suburb,
        Area = area,
        Latitude = lat,
        Longitude = lng,
        DistanceKm = km,
        MapX = x,
        MapY = y,
        CourtCount = courts,
        HourlyRate = hourly,
        WalkInBooking = walkIn,
        Sports = sports,
        OpensAt = "09:00",
        ClosesAt = "22:30"
    };

    static List<AppUser> BuildUsers(DateTimeOffset now)
    {
        var users = new List<AppUser>
        {
            User(1, "小郑", Gender.Male, 4, false, true, "Epsom 俱乐部组织者", "Organiser at Epsom", now),
            User(2, "林可", Gender.Female, 3, false, true, "用来体验报名", "Use this account to join", now),
            User(3, "Bella", Gender.Female, 2, false, true, "已在周日畅打名单里", "Already in the Sunday social", now),
            User(4, "Jason", Gender.Male, 2.5m, true, true, "新人，水平 L2.5", "Newcomer, L2.5", now),
            User(5, "Nick", Gender.Male, 6, false, true, "水平 L6，可试候补", "L6, try the waitlist", now),
            User(6, "Priya", Gender.Female, 5, false, true, "东区养生俱乐部", "Eastside casual club", now),
            User(7, "涛", Gender.Male, 4.5m, false, false, null, null, now),
            User(8, "Yuki", Gender.Female, 3, false, false, null, null, now),
            User(9, "阿凯", Gender.Male, 3.5m, false, false, null, null, now),
            User(10, "浩然", Gender.Male, 5, false, false, null, null, now),
            User(11, "子墨", Gender.Male, 4, false, false, null, null, now),
            User(12, "大伟", Gender.Male, 4.5m, false, false, null, null, now),
            User(13, "志强", Gender.Male, 3, false, false, null, null, now),
            User(14, "国栋", Gender.Male, 5, false, false, null, null, now),
            User(15, "俊杰", Gender.Male, 3.5m, false, false, null, null, now),
            User(16, "Ryan", Gender.Male, 4, false, false, null, null, now),
            User(17, "文博", Gender.Male, 2, false, false, null, null, now),
            User(18, "Oscar", Gender.Male, 3, false, false, null, null, now),
            User(19, "凯杰", Gender.Male, 4.5m, false, false, null, null, now),
            User(20, "Mina", Gender.Female, 3, false, false, null, null, now),
            User(21, "Sophie", Gender.Female, 2.5m, false, false, null, null, now),
            User(22, "Chloe", Gender.Female, 3.5m, false, false, null, null, now),
            User(23, "Emma", Gender.Female, 2, false, false, null, null, now),
            User(24, "Hana", Gender.Female, 4, false, false, null, null, now),
            User(25, "Lily", Gender.Female, 2.5m, true, false, null, null, now),
            User(26, "Sam", Gender.Male, 4, false, false, null, null, now)
        };

        string[] pool =
        [
            "思远", "Nora", "鹏飞", "Grace", "嘉豪", "Ivy", "明辉", "可馨", "Leo", "宇轩",
            "Mia", "建国", "安琪", "Tom", "雪儿", "博文", "Hannah", "立伟", "诗涵", "Ben",
            "佳宁", "子涵", "Olivia", "晨曦", "Dylan", "悦悦", "浩宇", "Chloe W", "国华", "Mia L",
            "子豪", "Emma W", "俊熙", "Hana P", "文昊", "Lily C", "凯旋", "Sophie L", "天佑", "Nina"
        ];
        for (var i = 0; i < pool.Length; i++)
        {
            var skill = i < 16 ? 3m : i < 34 ? 5m : 2m;
            users.Add(User(40 + i, pool[i], i % 2 == 0 ? Gender.Male : Gender.Female, skill, false, false, null, null, now));
        }
        return users;
    }

    static AppUser User(int n, string name, Gender gender, decimal skill, bool newbie, bool listed, string? hint, string? hintEn, DateTimeOffset now) => new()
    {
        Id = Id('a', n),
        Name = name,
        Email = $"player{n}@sportmate.local",
        Gender = gender,
        SkillLevel = skill,
        IsNewcomer = newbie,
        Listed = listed,
        Hint = hint,
        HintEn = hintEn,
        CreatedAt = now.AddDays(-40)
    };

    static List<Club> BuildClubs(List<AppUser> users, List<Venue> venues, DateTimeOffset now)
    {
        Club Make(int n, int owner, int? venue, string name, string bio, SportKind sport, string mono, string color, int hosted, decimal rating, decimal back) => new()
        {
            Id = Id('c', n),
            OwnerId = Id('a', owner),
            HomeVenueId = venue is int v ? Id('b', v) : null,
            Name = name,
            Bio = bio,
            Sport = sport,
            Monogram = mono,
            Color = color,
            EventsHosted = hosted,
            Rating = rating,
            ReturnRate = back,
            CreatedAt = now.AddDays(-80)
        };

        return
        [
            Make(1, 1, 1, "Epsom 周末羽球会", "每周在 Gillies Ave 订好场地，新手和老手一起轮转。球拍可以借，停车免费。", SportKind.Badminton, "E", "#0f7a45", 36, 4.9m, 0.68m),
            Make(2, 5, 2, "Waitakere 对抗组", "中高水平混双，对位认真，但散场一起吃饭。", SportKind.Badminton, "W", "#1d4e89", 18, 4.7m, 0.54m),
            Make(3, 6, 3, "东区养生羽球", "慢节奏、少杀球，下班来出出汗。", SportKind.Badminton, "东", "#c2412d", 22, 4.8m, 0.61m),
            Make(4, 26, 6, "North Harbour Picklers", "匹克球入门局，拍子现场可以借。", SportKind.Pickleball, "P", "#0e7490", 11, 4.6m, 0.48m),
            Make(5, 26, 5, "Parnell Tennis Circle", "帕内尔晚间对拉，四片场地轮换。", SportKind.Tennis, "T", "#8a5a00", 9, 4.5m, 0.52m),
            Make(6, 26, 7, "桌球星期三", "乒乓球练习局，单打双打都有。", SportKind.TableTennis, "桌", "#5b3a8c", 14, 4.8m, 0.57m)
        ];
    }

    static List<Activity> BuildActivities(List<AppUser> users, List<Venue> venues, List<Club> clubs, DateOnly today, DateOnly sunday, DateTimeOffset now)
    {
        var byUser = users.ToDictionary(u => u.Id);
        var byVenue = venues.ToDictionary(v => v.Id);
        var byClub = clubs.ToDictionary(c => c.Id);
        var list = new List<Activity>();
        var seq = 0;

        Activity Make(
            int club, int venue, string title, string titleEn, DateOnly day, string start, string end,
            int capacity, decimal price, decimal skillMin, decimal skillMax,
            decimal womenDiscount = 0, bool courtsBooked = true, bool womenFriendly = false,
            bool welcome = false, bool competitive = false, bool casual = false, bool parking = false,
            int? courtsUsed = null, int? playersPerCourt = null, string? courtNumbers = null, string? shuttle = null)
        {
            seq++;
            var home = byClub[Id('c', club)];
            var startAt = AucklandTime.At(day, TimeOnly.Parse(start));
            return new Activity
            {
                Id = Id('d', seq),
                ClubId = home.Id,
                VenueId = Id('b', venue),
                OrganizerId = home.OwnerId,
                Sport = home.Sport,
                Title = title,
                TitleEn = titleEn,
                Status = ActivityStatus.Published,
                StartAt = startAt,
                EndAt = AucklandTime.At(day, TimeOnly.Parse(end)),
                Capacity = capacity,
                Price = price,
                WomenDiscount = womenDiscount,
                SkillMin = skillMin,
                SkillMax = skillMax,
                CourtsBooked = courtsBooked,
                CourtsUsed = courtsUsed,
                PlayersPerCourt = playersPerCourt,
                CourtNumbers = courtNumbers,
                Shuttlecock = shuttle,
                RefundPolicy = "h24",
                ParkingFree = parking,
                WomenFriendly = womenFriendly,
                WelcomeNewcomers = welcome,
                Competitive = competitive,
                Casual = casual,
                CreatedAt = now.AddDays(-2)
            };
        }

        void Fill(Activity activity, IEnumerable<int> ids, RegistrationStatus status)
        {
            var n = 0;
            foreach (var id in ids)
            {
                n++;
                var user = byUser[Id('a', id)];
                activity.Registrations.Add(new Registration
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Status = status,
                    Amount = status == RegistrationStatus.Paid ? Fees.Charge(activity.Price, activity.WomenDiscount, user.Gender) : 0,
                    WaitlistPosition = status == RegistrationStatus.Waitlist ? n : null,
                    CreatedAt = now.AddMinutes(-300 + n)
                });
            }
        }

        void Keep(Activity activity)
        {
            if (activity.StartAt > now.AddMinutes(20))
                list.Add(activity);
        }

        var doubles = Make(1, 1, "周日晚双打畅打 · 新手友好", "Sunday night doubles · beginner friendly", sunday, "18:00", "21:00", 24, 15, 2, 5, 2, true, true, true, false, false, true, 3, 6, "5、6、7", "Victor Master Ace");
        Fill(doubles, [1, 3, 4, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25], RegistrationStatus.Paid);
        Keep(doubles);

        var singles = Make(1, 1, "单打练习局", "Singles practice", sunday, "19:30", "22:00", 8, 16, 3, 6, courtsUsed: 4, playersPerCourt: 2);
        Fill(singles, Enumerable.Range(40, 8), RegistrationStatus.Paid);
        Keep(singles);

        var drills = Make(1, 1, "上午进阶训练", "Morning drills", sunday, "09:00", "11:00", 12, 20, 3, 6, courtsUsed: 2, playersPerCourt: 6);
        Fill(drills, Enumerable.Range(40, 9), RegistrationStatus.Paid);
        Keep(drills);

        var mixed = Make(2, 2, "混双对抗局 · 中高水平", "Mixed doubles · intermediate to advanced", sunday, "19:00", "22:00", 16, 18, 4, 7, competitive: true, courtsUsed: 4, playersPerCourt: 4);
        Fill(mixed, Enumerable.Range(56, 16), RegistrationStatus.Paid);
        Fill(mixed, Enumerable.Range(72, 2), RegistrationStatus.Waitlist);
        Keep(mixed);

        var rally = Make(2, 2, "周末对拉", "Weekend rally", sunday, "10:00", "12:00", 14, 14, 2, 6, courtsUsed: 3, playersPerCourt: 4);
        Fill(rally, Enumerable.Range(48, 10), RegistrationStatus.Paid);
        Keep(rally);

        var easySunday = Make(3, 3, "周日慢节奏养生局", "Sunday easy session", sunday, "19:00", "21:00", 12, 12, 1, 5, casual: true, womenFriendly: true, courtsUsed: 2, playersPerCourt: 6);
        Fill(easySunday, Enumerable.Range(48, 7), RegistrationStatus.Paid);
        Keep(easySunday);

        if (today != sunday)
        {
            var easy = Make(3, 3, "工作日养生局 · 慢节奏", "Weekday easy session", today, "19:00", "21:00", 12, 12, 1, 5, casual: true, womenFriendly: true, courtsUsed: 2, playersPerCourt: 6);
            Fill(easy, Enumerable.Range(40, 7), RegistrationStatus.Paid);
            Keep(easy);

            var todaySingles = Make(1, 1, "单打练习局", "Singles practice", today, "19:30", "22:00", 8, 16, 3, 6, courtsUsed: 4, playersPerCourt: 2);
            Fill(todaySingles, Enumerable.Range(48, 8), RegistrationStatus.Paid);
            Keep(todaySingles);

            var noon = Make(1, 1, "午间多球", "Lunchtime multiball", today, "12:10", "13:10", 10, 10, 2, 6, welcome: true, courtsUsed: 2, playersPerCourt: 4);
            Fill(noon, Enumerable.Range(56, 4), RegistrationStatus.Paid);
            Keep(noon);
        }

        var thursday = Upcoming(today, DayOfWeek.Thursday, new TimeOnly(19, 0), now);
        var hit = Make(1, 1, "下班畅打", "After-work hit", thursday, "19:00", "21:00", 10, 14, 2, 6, welcome: true, womenFriendly: true, courtsUsed: 2, playersPerCourt: 4);
        Fill(hit, Enumerable.Range(60, 6), RegistrationStatus.Paid);
        Keep(hit);

        var tuesday = Upcoming(today, DayOfWeek.Tuesday, new TimeOnly(18, 30), now);
        var pickle = Make(4, 6, "匹克球入门轮转", "Pickleball social", tuesday, "18:30", "20:30", 12, 10, 1, 8, welcome: true, womenFriendly: true, courtsBooked: true, courtsUsed: 2, playersPerCourt: 4);
        Fill(pickle, Enumerable.Range(40, 5), RegistrationStatus.Paid);
        Keep(pickle);

        var friday = Upcoming(today, DayOfWeek.Friday, new TimeOnly(18, 0), now);
        var tennis = Make(5, 5, "晚间网球对拉", "Evening tennis hit", friday, "18:00", "20:00", 8, 20, 2, 6, courtsUsed: 2, playersPerCourt: 2);
        Fill(tennis, Enumerable.Range(45, 6), RegistrationStatus.Paid);
        Keep(tennis);

        var saturday = Upcoming(today, DayOfWeek.Saturday, new TimeOnly(10, 0), now);
        var table = Make(6, 7, "乒乓球练习局", "Table tennis practice", saturday, "10:00", "12:00", 10, 8, 1, 8, welcome: true, courtsUsed: 4, playersPerCourt: 2);
        Fill(table, Enumerable.Range(51, 4), RegistrationStatus.Paid);
        Keep(table);

        return list;
    }

    static DateOnly Upcoming(DateOnly today, DayOfWeek day, TimeOnly time, DateTimeOffset now)
    {
        var date = AucklandTime.Next(today, day);
        if (AucklandTime.At(date, time) <= now.AddMinutes(30))
            date = date.AddDays(7);
        return date;
    }
}
