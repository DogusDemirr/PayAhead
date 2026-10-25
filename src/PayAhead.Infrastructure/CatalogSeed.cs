using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PayAhead.Application.Catalog;
using PayAhead.Domain;
namespace PayAhead.Infrastructure;

// Codes and this v1 namespace are permanent. No dates, prices or user data are seeded.
public static partial class CatalogSeed
{
    public static Guid Id(string code)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes("payahead.catalog.v1:" + code))[..16];
        bytes[7] = (byte)((bytes[7] & 15) | 64); bytes[8] = (byte)((bytes[8] & 63) | 128); return new Guid(bytes);
    }
    private static readonly (CatalogCategory[] Categories, CatalogService[] Services, CatalogServiceCategory[] Links) Data = Build();
    public static IReadOnlyList<CatalogCategory> Categories => Data.Categories;
    public static IReadOnlyList<CatalogService> Services => Data.Services;
    public static IReadOnlyList<CatalogServiceCategory> Links => Data.Links;
    private static (CatalogCategory[], CatalogService[], CatalogServiceCategory[]) Build()
    {
        var categories = new List<CatalogCategory>(); var services = new List<CatalogService>(); var links = new List<CatalogServiceCategory>();
        void Category(string code, string tr, string en, string icon, string? parent = null, CatalogApplicability applies = CatalogApplicability.Both)
            => categories.Add(new() { Id = Id(code), Code = code, NameTr = tr, NameEn = en, IconKey = icon, ParentId = parent is null ? null : Id(parent), SortOrder = categories.Count * 10, ApplicableTo = applies, SearchText = CatalogSearch.Normalize(tr + " " + en + " " + code) });
        const CatalogApplicability Payment = CatalogApplicability.Payment;
        Category("digital", "Dijital Abonelikler", "Digital subscriptions", "digital");
        Category("video", "Film ve Dizi", "Film and TV", "video", "digital"); Category("music", "Müzik ve Podcast", "Music and podcasts", "music", "digital");
        Category("gaming", "Oyun ve Eğlence", "Games and entertainment", "gaming", "digital"); Category("ai", "Yapay Zekâ", "Artificial intelligence", "ai", "digital");
        Category("software", "Yazılım ve Üretkenlik", "Software and productivity", "software", "digital"); Category("cloud", "Bulut ve Depolama", "Cloud and storage", "cloud", "digital");
        Category("education", "Eğitim", "Education", "education"); Category("fitness", "Spor ve Fitness", "Sport and fitness", "fitness");
        Category("telecom", "İnternet ve Telekom", "Internet and telecom", "telecom"); Category("utilities", "Elektrik, Su ve Doğalgaz", "Utilities", "utilities", applies: Payment);
        Category("home", "Ev ve Kira", "Home and rent", "home", applies: Payment); Category("transport", "Araç ve Ulaşım", "Vehicle and transport", "transport", applies: Payment);
        Category("insurance", "Sigorta", "Insurance", "insurance", applies: Payment); Category("finance", "Banka, Kredi ve Finans", "Banking and finance", "finance", applies: Payment);
        Category("shopping", "Alışveriş ve Üyelikler", "Shopping and memberships", "shopping"); Category("groceries", "Market ve Gıda", "Groceries and food", "groceries", applies: Payment);
        Category("health", "Sağlık", "Health", "health", applies: Payment); Category("tax", "Vergi ve Resmî Ödemeler", "Taxes and official payments", "tax", applies: Payment);
        Category("news", "Haber ve Yayıncılık", "News and publishing", "news"); Category("family", "Aile ve Çocuk", "Family and children", "family", applies: Payment);
        Category("personal", "Kişisel Bakım", "Personal care", "personal", applies: Payment); Category("travel", "Seyahat ve Konaklama", "Travel and accommodation", "travel", applies: Payment);
        Category("work", "İş ve Profesyonel Hizmetler", "Work and professional services", "work"); Category("other", "Diğer", "Other", "other");
        Category("transport.fuel", "Yakıt", "Fuel", "fuel", "transport", Payment);
        string[] transport = ["petrol|Benzin|Petrol", "diesel|Motorin|Diesel", "lpg|LPG|LPG", "charging|Elektrikli Araç Şarjı|EV charging", "maintenance|Periyodik Bakım|Scheduled maintenance", "repair|Araç Tamiri|Vehicle repair", "parts|Yedek Parça|Spare parts", "tyres|Lastik ve Jant|Tyres and wheels", "traffic-insurance|Trafik Sigortası|Traffic insurance", "comprehensive-insurance|Kasko|Comprehensive insurance", "vehicle-tax|Motorlu Taşıtlar Vergisi|Motor vehicle tax", "inspection|Araç Muayenesi|Vehicle inspection", "emissions|Egzoz Muayenesi|Emissions inspection", "hgs|HGS|HGS", "tolls|Köprü ve Otoyol|Bridge and motorway", "parking|Otopark|Parking", "wash|Araç Yıkama|Car wash", "roadside|Yol Yardım|Roadside assistance", "rental|Araç Kiralama|Vehicle rental", "loan|Araç Kredisi|Vehicle loan", "public|Toplu Taşıma|Public transport", "taxi|Taksi|Taxi", "other|Diğer Araç Giderleri|Other vehicle costs"];
        foreach (var row in transport) { var p = row.Split('|'); Category("transport." + p[0], p[1], p[2], p[0] is "petrol" or "diesel" or "lpg" ? "fuel" : "transport", p[0] is "petrol" or "diesel" or "lpg" ? "transport.fuel" : "transport", Payment); }
        foreach (var row in new[] { "home.rent|Kira|Rent|home", "home.dues|Aidat|Maintenance dues|home", "utilities.electricity|Elektrik Faturası|Electricity bill|utilities", "utilities.water|Su Faturası|Water bill|utilities", "utilities.gas|Doğalgaz Faturası|Gas bill|utilities", "telecom.internet|İnternet Faturası|Internet bill|telecom", "telecom.phone|Telefon Faturası|Phone bill|telecom" }) { var p = row.Split('|'); Category(p[0], p[1], p[2], p[3], p[0].Split('.')[0], Payment); }
        void Service(string code, string name, string category, PaymentFrequency? frequency = PaymentFrequency.Monthly, CatalogApplicability applies = CatalogApplicability.Both, bool popular = false, string? country = null, string keywords = "", bool active = true, string[]? extra = null)
        {
            var c = categories.Single(x => x.Code == category);
            var s = new CatalogService { Id = Id("service." + code), Code = code, DisplayName = name, CategoryId = c.Id, SearchKeywords = keywords, SearchText = CatalogSearch.Normalize(name + " " + code + " " + keywords + " " + c.NameTr + " " + c.NameEn), SortOrder = services.Count * 10, IsPopular = popular, ApplicableTo = applies, DefaultRecurrenceType = frequency, CountryCode = country, IsActive = active };
            services.Add(s); foreach (var target in new[] { category }.Concat(extra ?? []).Distinct()) links.Add(new() { ServiceId = s.Id, CategoryId = Id(target) });
        }
        foreach (var name in new[] { "Netflix", "Disney+", "Amazon Prime Video", "HBO Max", "Exxen", "TOD", "TV+", "MUBI" }) Service(Slug(name), name, "video", popular: name is "Netflix" or "Disney+", keywords: name == "HBO Max" ? "Max BluTV" : "");
        Service("apple-tv", "Apple TV", "video", frequency: null, active: false, keywords: "Apple TV+ Türkiye iCloud+ kapsamında ayrı abonelik değil");
        foreach (var name in new[] { "Spotify", "YouTube Music", "Apple Music", "Deezer", "TIDAL" }) Service(Slug(name), name, "music", popular: name == "Spotify");
        foreach (var name in new[] { "Xbox Game Pass", "PlayStation Plus", "Nintendo Switch Online", "EA Play", "GeForce NOW" }) Service(Slug(name), name, "gaming");
        foreach (var name in new[] { "ChatGPT", "Claude", "Google AI Pro", "Perplexity", "Midjourney" }) Service(Slug(name), name, "ai", popular: name == "ChatGPT", keywords: name == "Google AI Pro" ? "Gemini Google One AI Premium" : "");
        foreach (var name in new[] { "Microsoft 365", "Adobe Creative Cloud", "Canva", "Notion", "GitHub Copilot" }) Service(Slug(name), name, "software");
        foreach (var name in new[] { "Google One", "iCloud+", "Dropbox", "OneDrive" }) Service(Slug(name), name, "cloud", keywords: name == "iCloud+" ? "Apple TV Apple TV+ Apple Arcade paket Türkiye" : "", extra: name == "iCloud+" ? ["video", "gaming"] : null);
        foreach (var name in new[] { "Duolingo", "Coursera", "Udemy", "Skillshare" }) Service(Slug(name), name, "education", frequency: name == "Udemy" ? null : PaymentFrequency.Monthly);
        foreach (var name in new[] { "Strava", "Freeletics", "Fitbod" }) Service(Slug(name), name, "fitness");
        Service("amazon-prime", "Amazon Prime", "shopping", extra: ["video"]); Service("hepsiburada-premium", "Hepsiburada Premium", "shopping", country: "TR"); Service("trendyol-plus", "Trendyol Plus", "shopping", country: "TR");
        foreach (var name in new[] { "Shell", "Opet", "Petrol Ofisi", "bp", "TotalEnergies", "Aytemiz" }) Service(Slug(name), name, "transport.petrol", PaymentFrequency.Once, Payment, country: "TR", keywords: "benzin motorin lpg yakıt", active: name != "bp", extra: ["transport.diesel", "transport.lpg"]);
        foreach (var name in new[] { "Trugo", "ZES", "Eşarj" }) Service(Slug(name), name, "transport.charging", PaymentFrequency.Once, Payment, country: "TR");
        foreach (var name in new[] { "Bosch Car Service", "Otopratik", "Genel Servis", "Özel Tamirci" }) Service(Slug(name), name, "transport.repair", PaymentFrequency.Once, Payment, country: "TR", extra: ["transport.maintenance"]);
        Service("tuvturk", "TÜVTÜRK", "transport.inspection", null, Payment, country: "TR", extra: ["transport.emissions"]);
        Service("hgs", "HGS", "transport.hgs", PaymentFrequency.Once, Payment, country: "TR", extra: ["transport.tolls"]);
        foreach (var row in new[] { "Lastikçi|transport.tyres", "Otopark|transport.parking", "Oto Yıkama|transport.wash" }) { var p = row.Split('|'); Service(Slug(p[0]), p[0], p[1], PaymentFrequency.Once, Payment, country: "TR"); }
        foreach (var name in new[] { "Turkcell", "Vodafone", "Türk Telekom", "TurkNet", "Superonline", "Millenicom", "Netspeed" }) Service(Slug(name), name, "telecom", country: "TR", extra: ["telecom.internet", "telecom.phone"]);
        foreach (var row in new[] { "Elektrik Faturası|utilities.electricity", "Su Faturası|utilities.water", "Doğalgaz Faturası|utilities.gas", "İnternet Faturası|telecom.internet", "Telefon Faturası|telecom.phone", "Kira|home.rent", "Aidat|home.dues" }) { var p = row.Split('|'); Service(Slug(p[0]), p[0], p[1], PaymentFrequency.Monthly, Payment, country: "TR"); }
        foreach (var row in new[] { "Konut Sigortası|insurance|Yearly", "Ev Bakımı|home|Once", "Kredi Kartı Ödemesi|finance|Monthly", "İhtiyaç Kredisi|finance|Monthly", "Konut Kredisi|finance|Monthly", "Taşıt Kredisi|transport.loan|Monthly", "Banka Ücreti|finance|Once", "Okul Ücreti|education|Monthly", "Kurs|education|Once", "Özel Ders|education|Once", "Özel Sağlık Sigortası|health|Yearly", "Muayene|health|Once", "İlaç|health|Once", "Vergi Ödemesi|tax|Once", "Harç|tax|Once", "Resmî Ücret|tax|Once", "Spor Salonu|fitness|Monthly", "Kuaför|personal|Once", "Kişisel Bakım|personal|Once", "Trafik Sigortası|transport.traffic-insurance|Yearly", "Kasko|transport.comprehensive-insurance|Yearly", "MTV|transport.vehicle-tax|Once" }) { var p = row.Split('|'); Service(Slug(p[0]), p[0], p[1], Enum.Parse<PaymentFrequency>(p[2]), p[0] == "Spor Salonu" ? CatalogApplicability.Both : Payment, country: "TR"); }
        Expand(categories, services, links);
        return (categories.ToArray(), services.ToArray(), links.ToArray());
    }
    private static string Slug(string value) => CatalogSearch.Normalize(value).Replace("+", "-plus").Replace(' ', '-');
    // Explicit maintenance utility; never called at application startup. Missing shared
    // catalog rows only; existing operator changes and all user records stay untouched.
    public static async Task EnsureMissingAsync(PayAheadDbContext db, CancellationToken ct = default)
    {
        var cats = await db.CatalogCategories.Select(x => x.Id).ToArrayAsync(ct); var services = await db.CatalogServices.Select(x => x.Id).ToArrayAsync(ct); var links = await db.CatalogServiceCategories.AsNoTracking().ToArrayAsync(ct);
        foreach (var c in Categories.Where(x => !cats.Contains(x.Id))) db.CatalogCategories.Add(new() { Id = c.Id, ParentId = c.ParentId, Code = c.Code, NameTr = c.NameTr, NameEn = c.NameEn, IconKey = c.IconKey, SortOrder = c.SortOrder, IsActive = c.IsActive, ApplicableTo = c.ApplicableTo, SearchText = c.SearchText });
        foreach (var s in Services.Where(x => !services.Contains(x.Id))) db.CatalogServices.Add(new() { Id = s.Id, Code = s.Code, DisplayName = s.DisplayName, CategoryId = s.CategoryId, LogoKey = s.LogoKey, SearchKeywords = s.SearchKeywords, SearchText = s.SearchText, SortOrder = s.SortOrder, IsPopular = s.IsPopular, IsActive = s.IsActive, ApplicableTo = s.ApplicableTo, DefaultRecurrenceType = s.DefaultRecurrenceType, CountryCode = s.CountryCode });
        foreach (var l in Links.Where(x => !links.Any(y => y.ServiceId == x.ServiceId && y.CategoryId == x.CategoryId))) db.CatalogServiceCategories.Add(new() { ServiceId = l.ServiceId, CategoryId = l.CategoryId });
        await db.SaveChangesAsync(ct);
    }
}
