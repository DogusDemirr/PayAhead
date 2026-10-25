using PayAhead.Application.Catalog;
using PayAhead.Domain;
namespace PayAhead.Infrastructure;

public static partial class CatalogSeed
{
    // Additive catalogue revision: existing IDs, codes, parents and private finance stay intact.
    private static void Expand(List<CatalogCategory> categories, List<CatalogService> services, List<CatalogServiceCategory> links)
    {
        string[] additions = [
            "video.streaming|Dijital Yayın Platformları|Streaming platforms|video", "video.cinema|Sinema ve Film Kulüpleri|Cinema and film clubs|video",
            "music.streaming|Müzik Platformları|Music platforms|music", "music.podcasts|Podcast ve Sesli Yayınlar|Podcasts and audio|music",
            "gaming.memberships|Oyun Üyelikleri|Game memberships|gaming", "gaming.cloud|Bulut Oyun|Cloud gaming|gaming",
            "ai.assistants|Yapay Zekâ Asistanları|AI assistants|ai", "ai.creative|Görsel ve Yaratıcı Araçlar|Creative AI tools|ai",
            "software.office|Ofis ve Üretkenlik|Office and productivity|software", "software.design|Tasarım Araçları|Design tools|software", "software.development|Geliştirici Araçları|Developer tools|software",
            "cloud.storage|Bulut Depolama|Cloud storage|cloud", "cloud.backup|Yedekleme|Backup|cloud",
            "education.courses|Kurs ve Online Eğitim|Courses and online learning|education", "education.language|Dil Öğrenimi|Language learning|education", "education.school|Okul ve Özel Ders|School and tutoring|education",
            "fitness.gym|Spor Salonu|Gym memberships|fitness", "fitness.training|Antrenman Uygulamaları|Training apps|fitness",
            "telecom.mobile|Mobil Hat ve Paketler|Mobile plans|telecom", "telecom.broadband|Ev İnterneti|Home broadband|telecom", "home.maintenance|Ev Bakımı ve Onarım|Home maintenance|home",
            "transport.service|Bakım ve Tamir|Maintenance and repair|transport",
            "insurance.vehicle|Araç Sigortaları|Vehicle insurance|insurance", "insurance.home|Konut Sigortaları|Home insurance|insurance", "insurance.health|Sağlık Sigortaları|Health insurance|insurance", "insurance.other|Diğer Sigortalar|Other insurance|insurance",
            "finance.cards|Kredi Kartı|Credit card payments|finance", "finance.loans|Kredi Taksitleri|Loan instalments|finance", "finance.fees|Banka Ücretleri|Bank fees|finance",
            "shopping.memberships|Alışveriş Üyelikleri|Shopping memberships|shopping", "shopping.orders|Alışveriş ve Siparişler|Shopping and orders|shopping",
            "groceries.market|Market Alışverişi|Grocery shopping|groceries", "groceries.delivery|Yemek ve Gıda Siparişi|Food delivery|groceries",
            "health.appointments|Muayene ve Tedavi|Appointments and treatment|health", "health.medicine|İlaç ve Eczane|Medicine and pharmacy|health", "health.cover|Sağlık Güvencesi|Health coverage|health",
            "tax.general|Vergi Ödemeleri|Tax payments|tax", "tax.fees|Harç ve Resmî Ücretler|Official fees|tax",
            "news.digital-newspaper|Dijital Gazete|Digital newspapers|news", "news.print-newspaper|Basılı Gazete|Print newspapers|news", "news.digital-magazine|Dijital Dergi|Digital magazines|news", "news.print-magazine|Basılı Dergi|Print magazines|news",
            "news.finance|Ekonomi ve Finans Haberleri|Business and finance news|news", "news.technology|Teknoloji Haberleri|Technology news|news", "news.sports|Spor Haberleri|Sports news|news", "news.international|Uluslararası Haberler|International news|news", "news.newsletters|Haber Bültenleri|Newsletters|news", "news.other|Diğer Yayınlar|Other publications|news",
            "family.childcare|Çocuk Bakımı|Childcare|family", "family.activities|Çocuk Etkinlikleri|Children activities|family",
            "personal.hair|Kuaför ve Berber|Hairdressing|personal", "personal.care|Bakım ve Kozmetik|Care and cosmetics|personal",
            "travel.accommodation|Konaklama|Accommodation|travel", "travel.tickets|Ulaşım Biletleri|Travel tickets|travel", "travel.services|Seyahat Hizmetleri|Travel services|travel",
            "work.consulting|Danışmanlık ve Destek|Consulting and support|work", "work.tools|Profesyonel Araçlar|Professional tools|work", "work.memberships|Mesleki Üyelikler|Professional memberships|work",
            "other.payment|Diğer Ödemeler|Other payments|other", "other.subscription|Diğer Abonelikler|Other subscriptions|other"
        ];
        foreach (var row in additions)
        {
            var p = row.Split('|'); var parent = categories.Single(x => x.Code == p[3]);
            categories.Add(new() { Id = Id(p[0]), Code = p[0], NameTr = p[1], NameEn = p[2], ParentId = parent.Id, IconKey = parent.IconKey, SortOrder = 1000 + categories.Count * 10,
                ApplicableTo = p[0] == "other.payment" ? CatalogApplicability.Payment : p[0] == "other.subscription" ? CatalogApplicability.Subscription : parent.ApplicableTo,
                SearchText = CatalogSearch.Normalize(p[1] + " " + p[2] + " " + p[0] + " " + parent.NameTr) });
        }
        void Link(string serviceCode, params string[] targets)
        {
            var service = services.Single(x => x.Code == serviceCode);
            foreach (var code in targets)
            {
                var category = categories.Single(x => x.Code == code);
                if (!links.Any(x => x.ServiceId == service.Id && x.CategoryId == category.Id)) links.Add(new() { ServiceId = service.Id, CategoryId = category.Id });
            }
        }
        foreach (var s in services.ToArray())
        {
            var code = categories.Single(x => x.Id == s.CategoryId).Code;
            string? target = code switch {
                "video" => "video.streaming", "music" => "music.streaming", "gaming" => s.Code == "geforce-now" ? "gaming.cloud" : "gaming.memberships",
                "ai" => s.Code == "midjourney" ? "ai.creative" : "ai.assistants", "software" => s.Code is "adobe-creative-cloud" or "canva" ? "software.design" : s.Code == "github-copilot" ? "software.development" : "software.office",
                "cloud" => "cloud.storage", "education" => s.Code == "duolingo" ? "education.language" : s.Code is "okul-ucreti" or "ozel-ders" ? "education.school" : "education.courses",
                "fitness" => s.Code == "spor-salonu" ? "fitness.gym" : "fitness.training", "shopping" => "shopping.memberships", "telecom" => s.Code is "turkcell" or "vodafone" or "turk-telekom" ? "telecom.mobile" : "telecom.broadband",
                "insurance" => "insurance.home", "finance" => s.Code == "kredi-karti-odemesi" ? "finance.cards" : s.Code == "banka-ucreti" ? "finance.fees" : "finance.loans",
                "health" => s.Code == "ilac" ? "health.medicine" : s.Code == "ozel-saglik-sigortasi" ? "health.cover" : "health.appointments",
                "tax" => s.Code == "vergi-odemesi" ? "tax.general" : "tax.fees", "home" => "home.maintenance", "personal" => s.Code == "kuafor" ? "personal.hair" : "personal.care", _ => null };
            if (target is not null) Link(s.Code, target);
            if (code == "transport.petrol") Link(s.Code, "transport.fuel");
            if (code == "transport.repair") Link(s.Code, "transport.service");
        }
        foreach (var code in new[] { "turkcell", "vodafone", "turk-telekom" }) Link(code, "telecom.broadband");
        Link("trafik-sigortasi", "insurance.vehicle"); Link("kasko", "insurance.vehicle"); Link("ozel-saglik-sigortasi", "insurance.health");
        Link("icloud-plus", "video.streaming", "gaming.memberships"); Link("amazon-prime", "video.streaming");
        void Publisher(string code, string name, string primary, string keywords, params string[] extra)
        {
            var category = categories.Single(x => x.Code == primary);
            var s = new CatalogService { Id = Id("service." + code), Code = code, DisplayName = name, CategoryId = category.Id, SortOrder = 2000 + services.Count * 10, ApplicableTo = CatalogApplicability.Both,
                DefaultRecurrenceType = null, SearchKeywords = keywords, SearchText = CatalogSearch.Normalize(name + " " + code + " " + keywords + " " + category.NameTr) };
            services.Add(s); Link(code, new[] { primary, "news" }.Concat(extra).ToArray());
        }
        Publisher("new-york-times", "The New York Times", "news.digital-newspaper", "NYT gazete news", "news.international");
        Publisher("wall-street-journal", "The Wall Street Journal", "news.digital-newspaper", "WSJ ekonomi finance", "news.finance", "news.international");
        Publisher("financial-times", "Financial Times", "news.digital-newspaper", "FT ekonomi finance", "news.finance", "news.international");
        Publisher("bloomberg", "Bloomberg", "news.finance", "ekonomi finans digital access", "news.digital-newspaper", "news.international");
        Publisher("the-economist", "The Economist", "news.digital-magazine", "ekonomi dergi digital print", "news.print-magazine", "news.finance", "news.international");
        Publisher("medium", "Medium", "news.digital-magazine", "membership teknoloji yazı", "news.technology", "news.other");
        Publisher("substack", "Substack", "news.newsletters", "ücretli yayıncı bülten paid newsletter");
    }
}
