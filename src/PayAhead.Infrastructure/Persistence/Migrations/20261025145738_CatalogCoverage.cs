using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PayAhead.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CatalogCoverage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "CatalogCategories",
                columns: new[] { "Id", "ApplicableTo", "Code", "IconKey", "IsActive", "NameEn", "NameTr", "ParentId", "SearchText", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("05a956ce-0c6b-42fc-bc75-ca1f28e55120"), 0, "health.cover", "health", true, "Health coverage", "Sağlık Güvencesi", new Guid("a6f72f7d-c561-4496-865a-ee522dcae63f"), "saglik guvencesi health coverage health.cover saglik", 1910 },
                    { new Guid("05d5e9ec-afff-495e-9541-52416b37f28e"), 2, "ai.creative", "ai", true, "Creative AI tools", "Görsel ve Yaratıcı Araçlar", new Guid("9c945fb0-8444-4ad7-99fc-e15b5e1a5df0"), "gorsel ve yaratici araclar creative ai tools ai.creative yapay zeka", 1630 },
                    { new Guid("0c712d88-d2dc-4b8c-bea7-8b0d0c83f64e"), 2, "work.memberships", "work", true, "Professional memberships", "Mesleki Üyelikler", new Guid("c8b5b6ee-1204-4e3f-ac08-8d52be7377f1"), "mesleki uyelikler professional memberships work.memberships is ve profesyonel hizmetler", 2130 },
                    { new Guid("103d9e4d-bb6d-4d34-89d6-7b73f8bfdd82"), 2, "news.technology", "news", true, "Technology news", "Teknoloji Haberleri", new Guid("8c56a67b-2905-423f-96e9-4d8da4db583c"), "teknoloji haberleri technology news news.technology haber ve yayincilik", 1990 },
                    { new Guid("105b7a80-58f6-4202-adf3-b0ec7df0853b"), 0, "finance.loans", "finance", true, "Loan instalments", "Kredi Taksitleri", new Guid("43133d20-4b41-4423-bdb1-6e4c9646956c"), "kredi taksitleri loan instalments finance.loans banka, kredi ve finans", 1830 },
                    { new Guid("120d4e70-5475-4b00-85ff-c735ac5dac7c"), 2, "fitness.training", "fitness", true, "Training apps", "Antrenman Uygulamaları", new Guid("75330075-0a0e-4edb-ae9a-3b09df6c2515"), "antrenman uygulamalari training apps fitness.training spor ve fitness", 1730 },
                    { new Guid("1443b332-65ae-4dee-8516-163611721787"), 0, "groceries.delivery", "groceries", true, "Food delivery", "Yemek ve Gıda Siparişi", new Guid("03854818-24a2-4656-b72c-572da5e1661a"), "yemek ve gida siparisi food delivery groceries.delivery market ve gida", 1880 },
                    { new Guid("14cff2e1-ef04-4193-939a-59463107d17d"), 2, "music.streaming", "music", true, "Music platforms", "Müzik Platformları", new Guid("2dc8c7ab-920c-4b01-b191-9605039723fb"), "muzik platformlari music platforms music.streaming muzik ve podcast", 1580 },
                    { new Guid("1956109e-3bc0-4d72-8190-e858c32b7055"), 2, "software.office", "software", true, "Office and productivity", "Ofis ve Üretkenlik", new Guid("a5689548-1d21-4b91-a509-5bf60ab54c80"), "ofis ve uretkenlik office and productivity software.office yazilim ve uretkenlik", 1640 },
                    { new Guid("29020bd2-2e26-42f2-ad8a-853726892fd6"), 2, "shopping.orders", "shopping", true, "Shopping and orders", "Alışveriş ve Siparişler", new Guid("bf324a2d-f3f4-4e5f-966d-6198e8dc4ac8"), "alisveris ve siparisler shopping and orders shopping.orders alisveris ve uyelikler", 1860 },
                    { new Guid("2b0e36a4-2718-4746-abe6-735ef1e9d140"), 2, "news.print-magazine", "news", true, "Print magazines", "Basılı Dergi", new Guid("8c56a67b-2905-423f-96e9-4d8da4db583c"), "basili dergi print magazines news.print-magazine haber ve yayincilik", 1970 },
                    { new Guid("2e45a6c8-363e-4d61-a10c-29763cbea936"), 0, "insurance.home", "insurance", true, "Home insurance", "Konut Sigortaları", new Guid("e8b90166-a0f0-4cc4-8833-563025df402c"), "konut sigortalari home insurance insurance.home sigorta", 1790 },
                    { new Guid("2ecb6935-3a3a-41e1-9a4c-64a357ec94ac"), 2, "education.courses", "education", true, "Courses and online learning", "Kurs ve Online Eğitim", new Guid("ccf2543c-aa2f-4654-892e-b8720a04bd83"), "kurs ve online egitim courses and online learning education.courses egitim", 1690 },
                    { new Guid("2f9d9b00-84e9-46cd-ad24-b11a1c368889"), 2, "cloud.storage", "cloud", true, "Cloud storage", "Bulut Depolama", new Guid("ee6e4920-cb3d-4005-8651-95c5a3042db5"), "bulut depolama cloud storage cloud.storage bulut ve depolama", 1670 },
                    { new Guid("3b06cb49-6beb-4d76-9615-5bc8895b9825"), 0, "tax.general", "tax", true, "Tax payments", "Vergi Ödemeleri", new Guid("7473e800-2311-4ac6-9631-18fab4784f33"), "vergi odemeleri tax payments tax.general vergi ve resmi odemeler", 1920 },
                    { new Guid("3c960efc-061d-4fa1-90e3-91e2469f86f7"), 2, "work.consulting", "work", true, "Consulting and support", "Danışmanlık ve Destek", new Guid("c8b5b6ee-1204-4e3f-ac08-8d52be7377f1"), "danismanlik ve destek consulting and support work.consulting is ve profesyonel hizmetler", 2110 },
                    { new Guid("3dcf71b4-1022-4b6c-83f5-dfbeeb78cd5a"), 0, "travel.services", "travel", true, "Travel services", "Seyahat Hizmetleri", new Guid("aa885dfe-203f-4f4c-b36a-c2e252fab7fb"), "seyahat hizmetleri travel services travel.services seyahat ve konaklama", 2100 },
                    { new Guid("40405271-2f01-40bf-8d24-dce932f99dd1"), 0, "groceries.market", "groceries", true, "Grocery shopping", "Market Alışverişi", new Guid("03854818-24a2-4656-b72c-572da5e1661a"), "market alisverisi grocery shopping groceries.market market ve gida", 1870 },
                    { new Guid("406de719-32b2-4100-b2f4-ae74d635b03f"), 0, "personal.care", "personal", true, "Care and cosmetics", "Bakım ve Kozmetik", new Guid("50dde9cd-647d-4472-b263-ecc52cccff31"), "bakim ve kozmetik care and cosmetics personal.care kisisel bakim", 2070 },
                    { new Guid("4b284088-f9d6-43a0-bdd9-4f488bb6d67a"), 2, "telecom.broadband", "telecom", true, "Home broadband", "Ev İnterneti", new Guid("ad374367-3485-44e7-bc59-0daf55262b74"), "ev interneti home broadband telecom.broadband internet ve telekom", 1750 },
                    { new Guid("4c48e94d-7207-49b3-8d6f-e3f194847062"), 0, "health.medicine", "health", true, "Medicine and pharmacy", "İlaç ve Eczane", new Guid("a6f72f7d-c561-4496-865a-ee522dcae63f"), "ilac ve eczane medicine and pharmacy health.medicine saglik", 1900 },
                    { new Guid("4d596701-c9e7-4f05-9a41-c900f82092bc"), 2, "shopping.memberships", "shopping", true, "Shopping memberships", "Alışveriş Üyelikleri", new Guid("bf324a2d-f3f4-4e5f-966d-6198e8dc4ac8"), "alisveris uyelikleri shopping memberships shopping.memberships alisveris ve uyelikler", 1850 },
                    { new Guid("5f390075-6df9-4944-88e5-6d2c20a1a7a4"), 2, "news.sports", "news", true, "Sports news", "Spor Haberleri", new Guid("8c56a67b-2905-423f-96e9-4d8da4db583c"), "spor haberleri sports news news.sports haber ve yayincilik", 2000 },
                    { new Guid("6714ec3b-360c-4094-b046-7c1f2ae443cd"), 2, "video.streaming", "video", true, "Streaming platforms", "Dijital Yayın Platformları", new Guid("e64ecf69-c259-42c3-bc3b-4bc80dfa37b9"), "dijital yayin platformlari streaming platforms video.streaming film ve dizi", 1560 },
                    { new Guid("6bc3778d-d803-4d6f-9fd7-6707989d411b"), 1, "other.subscription", "other", true, "Other subscriptions", "Diğer Abonelikler", new Guid("1e00565d-0e1d-4807-ace9-105b374652fe"), "diger abonelikler other subscriptions other.subscription diger", 2150 },
                    { new Guid("6cc82a5b-5f94-4532-b9f6-7b0bc44045fd"), 2, "news.newsletters", "news", true, "Newsletters", "Haber Bültenleri", new Guid("8c56a67b-2905-423f-96e9-4d8da4db583c"), "haber bultenleri newsletters news.newsletters haber ve yayincilik", 2020 },
                    { new Guid("6f478f47-93b5-41ac-90ca-354e0d4314e4"), 0, "insurance.health", "insurance", true, "Health insurance", "Sağlık Sigortaları", new Guid("e8b90166-a0f0-4cc4-8833-563025df402c"), "saglik sigortalari health insurance insurance.health sigorta", 1800 },
                    { new Guid("761f831e-450d-420c-bcd9-53224fff8669"), 2, "ai.assistants", "ai", true, "AI assistants", "Yapay Zekâ Asistanları", new Guid("9c945fb0-8444-4ad7-99fc-e15b5e1a5df0"), "yapay zeka asistanlari ai assistants ai.assistants yapay zeka", 1620 },
                    { new Guid("775ec693-90f2-4b84-8ad7-f82976278ddf"), 2, "news.other", "news", true, "Other publications", "Diğer Yayınlar", new Guid("8c56a67b-2905-423f-96e9-4d8da4db583c"), "diger yayinlar other publications news.other haber ve yayincilik", 2030 },
                    { new Guid("797fd1da-9053-43b5-8236-ecc698081821"), 0, "travel.tickets", "travel", true, "Travel tickets", "Ulaşım Biletleri", new Guid("aa885dfe-203f-4f4c-b36a-c2e252fab7fb"), "ulasim biletleri travel tickets travel.tickets seyahat ve konaklama", 2090 },
                    { new Guid("7b31924b-95f0-4d4f-86b2-0882ee74f037"), 2, "news.print-newspaper", "news", true, "Print newspapers", "Basılı Gazete", new Guid("8c56a67b-2905-423f-96e9-4d8da4db583c"), "basili gazete print newspapers news.print-newspaper haber ve yayincilik", 1950 },
                    { new Guid("7bc49d86-6863-4849-bf5e-97352d9bdbfa"), 0, "family.childcare", "family", true, "Childcare", "Çocuk Bakımı", new Guid("c0f444ee-12ed-43a2-98df-9f61340ca7bc"), "cocuk bakimi childcare family.childcare aile ve cocuk", 2040 },
                    { new Guid("892b2f2b-d6de-4475-b869-1b101f320534"), 2, "gaming.memberships", "gaming", true, "Game memberships", "Oyun Üyelikleri", new Guid("c3f7efbd-2591-4237-9f4f-ff77648ba333"), "oyun uyelikleri game memberships gaming.memberships oyun ve eglence", 1600 },
                    { new Guid("8d1bdf4a-4a4f-41ac-8ca4-3243c39ee9c2"), 2, "news.international", "news", true, "International news", "Uluslararası Haberler", new Guid("8c56a67b-2905-423f-96e9-4d8da4db583c"), "uluslararasi haberler international news news.international haber ve yayincilik", 2010 },
                    { new Guid("8e8ffbdb-be95-43df-bf2d-11a4637699ee"), 0, "personal.hair", "personal", true, "Hairdressing", "Kuaför ve Berber", new Guid("50dde9cd-647d-4472-b263-ecc52cccff31"), "kuafor ve berber hairdressing personal.hair kisisel bakim", 2060 },
                    { new Guid("99ae4b81-6937-4777-a603-e1d89126aab9"), 0, "finance.fees", "finance", true, "Bank fees", "Banka Ücretleri", new Guid("43133d20-4b41-4423-bdb1-6e4c9646956c"), "banka ucretleri bank fees finance.fees banka, kredi ve finans", 1840 },
                    { new Guid("99af86dc-d903-45b3-832a-ebfcf434b2a4"), 2, "work.tools", "work", true, "Professional tools", "Profesyonel Araçlar", new Guid("c8b5b6ee-1204-4e3f-ac08-8d52be7377f1"), "profesyonel araclar professional tools work.tools is ve profesyonel hizmetler", 2120 },
                    { new Guid("a1a46632-73f9-4863-943e-b0c393419c21"), 2, "telecom.mobile", "telecom", true, "Mobile plans", "Mobil Hat ve Paketler", new Guid("ad374367-3485-44e7-bc59-0daf55262b74"), "mobil hat ve paketler mobile plans telecom.mobile internet ve telekom", 1740 },
                    { new Guid("a5219530-b0b2-4b70-a117-1723f629e801"), 0, "family.activities", "family", true, "Children activities", "Çocuk Etkinlikleri", new Guid("c0f444ee-12ed-43a2-98df-9f61340ca7bc"), "cocuk etkinlikleri children activities family.activities aile ve cocuk", 2050 },
                    { new Guid("a6f3a99d-9120-4dd9-9d15-9340f4f08a22"), 2, "news.digital-newspaper", "news", true, "Digital newspapers", "Dijital Gazete", new Guid("8c56a67b-2905-423f-96e9-4d8da4db583c"), "dijital gazete digital newspapers news.digital-newspaper haber ve yayincilik", 1940 },
                    { new Guid("a7600074-8ee1-45e2-94e1-ee712b1f5626"), 0, "insurance.vehicle", "insurance", true, "Vehicle insurance", "Araç Sigortaları", new Guid("e8b90166-a0f0-4cc4-8833-563025df402c"), "arac sigortalari vehicle insurance insurance.vehicle sigorta", 1780 },
                    { new Guid("a8901ac8-2d92-42da-afea-8cf63d1d6dd5"), 2, "news.digital-magazine", "news", true, "Digital magazines", "Dijital Dergi", new Guid("8c56a67b-2905-423f-96e9-4d8da4db583c"), "dijital dergi digital magazines news.digital-magazine haber ve yayincilik", 1960 },
                    { new Guid("a9285b52-c39f-4c12-a41a-367f3e89475f"), 2, "software.development", "software", true, "Developer tools", "Geliştirici Araçları", new Guid("a5689548-1d21-4b91-a509-5bf60ab54c80"), "gelistirici araclari developer tools software.development yazilim ve uretkenlik", 1660 },
                    { new Guid("ae96245e-612e-48eb-8cf9-a19571bc907c"), 2, "music.podcasts", "music", true, "Podcasts and audio", "Podcast ve Sesli Yayınlar", new Guid("2dc8c7ab-920c-4b01-b191-9605039723fb"), "podcast ve sesli yayinlar podcasts and audio music.podcasts muzik ve podcast", 1590 },
                    { new Guid("af5f00bc-030b-4804-a833-14eb77800e06"), 0, "home.maintenance", "home", true, "Home maintenance", "Ev Bakımı ve Onarım", new Guid("18acd618-5e34-4f9c-821c-87a7921ad1ad"), "ev bakimi ve onarim home maintenance home.maintenance ev ve kira", 1760 },
                    { new Guid("b2b60aed-aa7d-4018-b594-dbe81fae50dd"), 2, "video.cinema", "video", true, "Cinema and film clubs", "Sinema ve Film Kulüpleri", new Guid("e64ecf69-c259-42c3-bc3b-4bc80dfa37b9"), "sinema ve film kulupleri cinema and film clubs video.cinema film ve dizi", 1570 },
                    { new Guid("be2f4901-0638-411d-bb10-d566005aec61"), 0, "transport.service", "transport", true, "Maintenance and repair", "Bakım ve Tamir", new Guid("8b23d004-bf0c-498e-afa9-dc2ca67c1af6"), "bakim ve tamir maintenance and repair transport.service arac ve ulasim", 1770 },
                    { new Guid("c2790a5d-538c-4f1e-9e0b-62b7581c0fcf"), 2, "software.design", "software", true, "Design tools", "Tasarım Araçları", new Guid("a5689548-1d21-4b91-a509-5bf60ab54c80"), "tasarim araclari design tools software.design yazilim ve uretkenlik", 1650 },
                    { new Guid("d45db776-7db7-405b-9988-26166dbb95df"), 0, "tax.fees", "tax", true, "Official fees", "Harç ve Resmî Ücretler", new Guid("7473e800-2311-4ac6-9631-18fab4784f33"), "harc ve resmi ucretler official fees tax.fees vergi ve resmi odemeler", 1930 },
                    { new Guid("d6355b47-1900-4cd1-9a40-d3158167f3d5"), 0, "health.appointments", "health", true, "Appointments and treatment", "Muayene ve Tedavi", new Guid("a6f72f7d-c561-4496-865a-ee522dcae63f"), "muayene ve tedavi appointments and treatment health.appointments saglik", 1890 },
                    { new Guid("d8f14d9b-788e-4f8f-881f-f30e46aebd38"), 0, "other.payment", "other", true, "Other payments", "Diğer Ödemeler", new Guid("1e00565d-0e1d-4807-ace9-105b374652fe"), "diger odemeler other payments other.payment diger", 2140 },
                    { new Guid("e17b2055-d350-47ff-93be-a8e1e7fdf7ac"), 0, "insurance.other", "insurance", true, "Other insurance", "Diğer Sigortalar", new Guid("e8b90166-a0f0-4cc4-8833-563025df402c"), "diger sigortalar other insurance insurance.other sigorta", 1810 },
                    { new Guid("e29a3935-2b05-497a-8203-0eb978e5a7fe"), 2, "gaming.cloud", "gaming", true, "Cloud gaming", "Bulut Oyun", new Guid("c3f7efbd-2591-4237-9f4f-ff77648ba333"), "bulut oyun cloud gaming gaming.cloud oyun ve eglence", 1610 },
                    { new Guid("e64a71f1-4344-44a5-b510-ad89dc899dfc"), 2, "cloud.backup", "cloud", true, "Backup", "Yedekleme", new Guid("ee6e4920-cb3d-4005-8651-95c5a3042db5"), "yedekleme backup cloud.backup bulut ve depolama", 1680 },
                    { new Guid("e8db84b2-4ece-4dbe-abed-70e6c8e0b7af"), 2, "fitness.gym", "fitness", true, "Gym memberships", "Spor Salonu", new Guid("75330075-0a0e-4edb-ae9a-3b09df6c2515"), "spor salonu gym memberships fitness.gym spor ve fitness", 1720 },
                    { new Guid("ecfa12c2-b8c6-4508-ba5a-6e94dc4e9ffc"), 0, "travel.accommodation", "travel", true, "Accommodation", "Konaklama", new Guid("aa885dfe-203f-4f4c-b36a-c2e252fab7fb"), "konaklama accommodation travel.accommodation seyahat ve konaklama", 2080 },
                    { new Guid("f3c938b2-555f-4029-994d-564745f3e9ad"), 2, "news.finance", "news", true, "Business and finance news", "Ekonomi ve Finans Haberleri", new Guid("8c56a67b-2905-423f-96e9-4d8da4db583c"), "ekonomi ve finans haberleri business and finance news news.finance haber ve yayincilik", 1980 },
                    { new Guid("f7719fac-b111-42fe-b044-4179db523637"), 2, "education.language", "education", true, "Language learning", "Dil Öğrenimi", new Guid("ccf2543c-aa2f-4654-892e-b8720a04bd83"), "dil ogrenimi language learning education.language egitim", 1700 },
                    { new Guid("f84e1762-a4a1-447b-a0ea-c7fb0193219c"), 2, "education.school", "education", true, "School and tutoring", "Okul ve Özel Ders", new Guid("ccf2543c-aa2f-4654-892e-b8720a04bd83"), "okul ve ozel ders school and tutoring education.school egitim", 1710 },
                    { new Guid("fa6624eb-3539-45d1-8308-56ac06b9ba4e"), 0, "finance.cards", "finance", true, "Credit card payments", "Kredi Kartı", new Guid("43133d20-4b41-4423-bdb1-6e4c9646956c"), "kredi karti credit card payments finance.cards banka, kredi ve finans", 1820 }
                });

            migrationBuilder.InsertData(
                table: "CatalogServiceCategories",
                columns: new[] { "CategoryId", "ServiceId" },
                values: new object[,]
                {
                    { new Guid("caef747a-7a1f-4407-a714-40c446666d7d"), new Guid("37e55a59-6067-4afd-98ee-d9f1585fc3c9") },
                    { new Guid("caef747a-7a1f-4407-a714-40c446666d7d"), new Guid("74ad3446-0890-4fae-bea1-c24681b97100") },
                    { new Guid("caef747a-7a1f-4407-a714-40c446666d7d"), new Guid("7f36f1ed-8972-4bbf-8085-6f789fac0378") },
                    { new Guid("caef747a-7a1f-4407-a714-40c446666d7d"), new Guid("b3aa473e-0d76-451c-9a14-b32a475714eb") },
                    { new Guid("caef747a-7a1f-4407-a714-40c446666d7d"), new Guid("d758dc03-6e00-4319-9293-131bf7888a32") },
                    { new Guid("caef747a-7a1f-4407-a714-40c446666d7d"), new Guid("e52c79dd-5738-4838-bdbd-6e332092adb2") },
                    { new Guid("be2f4901-0638-411d-bb10-d566005aec61"), new Guid("0121de3c-8fc3-4e08-b551-0570e4ca47b1") },
                    { new Guid("4d596701-c9e7-4f05-9a41-c900f82092bc"), new Guid("0c04850b-ac18-4077-8252-fac3c762658a") },
                    { new Guid("6714ec3b-360c-4094-b046-7c1f2ae443cd"), new Guid("0c04850b-ac18-4077-8252-fac3c762658a") },
                    { new Guid("105b7a80-58f6-4202-adf3-b0ec7df0853b"), new Guid("13795c93-773f-4ab5-8a51-fc6cc1be8415") },
                    { new Guid("05a956ce-0c6b-42fc-bc75-ca1f28e55120"), new Guid("1475e83f-59eb-4eb7-98e8-1c0adf218900") },
                    { new Guid("6f478f47-93b5-41ac-90ca-354e0d4314e4"), new Guid("1475e83f-59eb-4eb7-98e8-1c0adf218900") },
                    { new Guid("be2f4901-0638-411d-bb10-d566005aec61"), new Guid("1b0da2d1-7da6-4d2e-8d78-66238422739b") },
                    { new Guid("1956109e-3bc0-4d72-8190-e858c32b7055"), new Guid("1b5b1522-80fc-45d0-bfb6-a15cacbc763f") },
                    { new Guid("120d4e70-5475-4b00-85ff-c735ac5dac7c"), new Guid("285791f2-abc1-4d8c-b87e-461daf90ebab") },
                    { new Guid("4b284088-f9d6-43a0-bdd9-4f488bb6d67a"), new Guid("3007de3f-25f3-42b3-ae6b-4e6cc46a8ebf") },
                    { new Guid("406de719-32b2-4100-b2f4-ae74d635b03f"), new Guid("31be357e-60bd-4674-92ff-f1ca9d371ec0") },
                    { new Guid("2f9d9b00-84e9-46cd-ad24-b11a1c368889"), new Guid("38e10d5a-73d4-4824-82bd-e7cc8f0daa1e") },
                    { new Guid("f84e1762-a4a1-447b-a0ea-c7fb0193219c"), new Guid("3d168272-0b2d-44bf-8624-acb90fb804a6") },
                    { new Guid("892b2f2b-d6de-4475-b869-1b101f320534"), new Guid("3df58be1-df63-4df2-826d-537879bfb328") },
                    { new Guid("05d5e9ec-afff-495e-9541-52416b37f28e"), new Guid("3eb0c555-3aff-4e7f-8f8d-f728bca22371") },
                    { new Guid("4b284088-f9d6-43a0-bdd9-4f488bb6d67a"), new Guid("46a7611b-4ebf-4800-93d2-767cabb614c2") },
                    { new Guid("be2f4901-0638-411d-bb10-d566005aec61"), new Guid("4be1f486-1b59-46a8-b836-928045dd782c") },
                    { new Guid("d45db776-7db7-405b-9988-26166dbb95df"), new Guid("4c8fbfca-8eb3-4771-9935-168302bdb1d8") },
                    { new Guid("c2790a5d-538c-4f1e-9e0b-62b7581c0fcf"), new Guid("4e0a6b80-9847-45fa-9000-643e7ecacb51") },
                    { new Guid("e8db84b2-4ece-4dbe-abed-70e6c8e0b7af"), new Guid("4e8c3109-5509-44fa-b877-ed5452656790") },
                    { new Guid("6714ec3b-360c-4094-b046-7c1f2ae443cd"), new Guid("4f06f017-1530-4506-a976-b2aa4e2200a4") },
                    { new Guid("2ecb6935-3a3a-41e1-9a4c-64a357ec94ac"), new Guid("5b3cfe98-d787-4af2-b69e-7e81b7141d21") },
                    { new Guid("be2f4901-0638-411d-bb10-d566005aec61"), new Guid("610f081c-d754-4dd2-9f3c-3bc93d820b30") },
                    { new Guid("f7719fac-b111-42fe-b044-4179db523637"), new Guid("6313d5f8-bb28-4822-9375-fbb2d2c50a9d") },
                    { new Guid("2e45a6c8-363e-4d61-a10c-29763cbea936"), new Guid("634a919e-3d90-44d2-97fe-2320c9dd17db") },
                    { new Guid("14cff2e1-ef04-4193-939a-59463107d17d"), new Guid("6762f6eb-06fd-49ea-a308-f25159b0fef8") },
                    { new Guid("120d4e70-5475-4b00-85ff-c735ac5dac7c"), new Guid("6b15a674-645b-4b8c-a134-a20d618973b4") },
                    { new Guid("761f831e-450d-420c-bcd9-53224fff8669"), new Guid("6eddd662-95db-4d5d-bfe3-eaf639919693") },
                    { new Guid("fa6624eb-3539-45d1-8308-56ac06b9ba4e"), new Guid("745d52b4-6770-44b3-801e-8ba15b6c1828") },
                    { new Guid("af5f00bc-030b-4804-a833-14eb77800e06"), new Guid("769082a7-ff15-4116-b6c3-b062aa6b3c6f") },
                    { new Guid("e29a3935-2b05-497a-8203-0eb978e5a7fe"), new Guid("7c36c1a0-cd2d-44be-a40e-51e1b0b23443") },
                    { new Guid("d6355b47-1900-4cd1-9a40-d3158167f3d5"), new Guid("87a62664-af83-4687-b0bc-53f58635ed20") },
                    { new Guid("2ecb6935-3a3a-41e1-9a4c-64a357ec94ac"), new Guid("8d3a4463-22ab-475b-a426-39a5f0f35cfd") },
                    { new Guid("6714ec3b-360c-4094-b046-7c1f2ae443cd"), new Guid("8f505405-b286-4a61-a2fa-18428bed8134") },
                    { new Guid("c2790a5d-538c-4f1e-9e0b-62b7581c0fcf"), new Guid("9014b93e-7b76-46a2-9f1b-c101dfcbb3e1") },
                    { new Guid("6714ec3b-360c-4094-b046-7c1f2ae443cd"), new Guid("91a26ebb-34f6-42cd-a130-39d2f764b0bc") },
                    { new Guid("2f9d9b00-84e9-46cd-ad24-b11a1c368889"), new Guid("93c31242-b92b-49d7-b5b3-2908d3f37c60") },
                    { new Guid("f84e1762-a4a1-447b-a0ea-c7fb0193219c"), new Guid("958da99c-35c5-4461-b486-6893d0d5095f") },
                    { new Guid("892b2f2b-d6de-4475-b869-1b101f320534"), new Guid("96e55ea8-00d8-46eb-936e-1b5ce5a6f121") },
                    { new Guid("a9285b52-c39f-4c12-a41a-367f3e89475f"), new Guid("97776faf-91f4-4836-b95c-8990032b75b8") },
                    { new Guid("120d4e70-5475-4b00-85ff-c735ac5dac7c"), new Guid("9ad446db-bea1-454a-adec-62e34c581de3") },
                    { new Guid("4b284088-f9d6-43a0-bdd9-4f488bb6d67a"), new Guid("9bbb7d25-a481-4a1c-981b-2528ddc5055a") },
                    { new Guid("8e8ffbdb-be95-43df-bf2d-11a4637699ee"), new Guid("9bd05373-5997-46e1-99c2-a7fa48f7a040") },
                    { new Guid("14cff2e1-ef04-4193-939a-59463107d17d"), new Guid("9e72cc7d-e299-497f-8d87-a9a745a4adc9") },
                    { new Guid("2f9d9b00-84e9-46cd-ad24-b11a1c368889"), new Guid("9f66fecb-5746-4edf-98d8-b6f85ef61307") },
                    { new Guid("6714ec3b-360c-4094-b046-7c1f2ae443cd"), new Guid("9f66fecb-5746-4edf-98d8-b6f85ef61307") },
                    { new Guid("892b2f2b-d6de-4475-b869-1b101f320534"), new Guid("9f66fecb-5746-4edf-98d8-b6f85ef61307") },
                    { new Guid("a7600074-8ee1-45e2-94e1-ee712b1f5626"), new Guid("a021683b-b248-4faa-aebd-2bfe3f82e49e") },
                    { new Guid("761f831e-450d-420c-bcd9-53224fff8669"), new Guid("a1524a4b-c16a-46db-99b0-95c7325d9df5") },
                    { new Guid("2ecb6935-3a3a-41e1-9a4c-64a357ec94ac"), new Guid("a15f60ac-6bc0-4e6a-87e4-80eafb8587b5") },
                    { new Guid("4b284088-f9d6-43a0-bdd9-4f488bb6d67a"), new Guid("a64ad571-dfda-43d9-a866-4d5b99a85ce3") },
                    { new Guid("a1a46632-73f9-4863-943e-b0c393419c21"), new Guid("a64ad571-dfda-43d9-a866-4d5b99a85ce3") },
                    { new Guid("4c48e94d-7207-49b3-8d6f-e3f194847062"), new Guid("a8593268-529d-43ed-9013-03df091ce03c") },
                    { new Guid("4d596701-c9e7-4f05-9a41-c900f82092bc"), new Guid("b78be435-cef9-4a6a-a694-1092aa6705a3") },
                    { new Guid("105b7a80-58f6-4202-adf3-b0ec7df0853b"), new Guid("ba340f4a-0337-4fd0-8af0-63fd75dbe3b1") },
                    { new Guid("4b284088-f9d6-43a0-bdd9-4f488bb6d67a"), new Guid("ba6716c0-6839-4b95-8940-8e30628cc0c2") },
                    { new Guid("a1a46632-73f9-4863-943e-b0c393419c21"), new Guid("ba6716c0-6839-4b95-8940-8e30628cc0c2") },
                    { new Guid("892b2f2b-d6de-4475-b869-1b101f320534"), new Guid("bd9373d2-2179-47fe-8eb8-cbd7a9907aa8") },
                    { new Guid("2ecb6935-3a3a-41e1-9a4c-64a357ec94ac"), new Guid("bfddbd4d-876f-4fe8-b493-f5231415917b") },
                    { new Guid("6714ec3b-360c-4094-b046-7c1f2ae443cd"), new Guid("c2203979-99db-4ccc-84e7-6587bef4ccad") },
                    { new Guid("3b06cb49-6beb-4d76-9615-5bc8895b9825"), new Guid("c336a593-2c19-4ce8-97cd-27935fda4a0c") },
                    { new Guid("761f831e-450d-420c-bcd9-53224fff8669"), new Guid("c41ad125-cd33-4e04-bcc9-8d7d28d82ccb") },
                    { new Guid("14cff2e1-ef04-4193-939a-59463107d17d"), new Guid("c69eb12b-14f7-4b8f-9b88-acae04446d69") },
                    { new Guid("6714ec3b-360c-4094-b046-7c1f2ae443cd"), new Guid("c8f4f69e-eb68-4782-a655-616892afc86c") },
                    { new Guid("4b284088-f9d6-43a0-bdd9-4f488bb6d67a"), new Guid("ca6548cf-887c-462e-8d47-545663a4af60") },
                    { new Guid("892b2f2b-d6de-4475-b869-1b101f320534"), new Guid("ce5d291e-a62e-4165-8cf0-d4f8fadbc817") },
                    { new Guid("4b284088-f9d6-43a0-bdd9-4f488bb6d67a"), new Guid("d0f065b8-6224-4be9-b0dc-97b5887f597e") },
                    { new Guid("a1a46632-73f9-4863-943e-b0c393419c21"), new Guid("d0f065b8-6224-4be9-b0dc-97b5887f597e") },
                    { new Guid("2f9d9b00-84e9-46cd-ad24-b11a1c368889"), new Guid("d887714e-a518-46c0-8ae7-9633f25d031f") },
                    { new Guid("1956109e-3bc0-4d72-8190-e858c32b7055"), new Guid("d9f4d7c8-c234-4cd2-815b-6100896ba386") },
                    { new Guid("a7600074-8ee1-45e2-94e1-ee712b1f5626"), new Guid("dcf84e50-34d8-4856-baf0-6fa5559d6504") },
                    { new Guid("99ae4b81-6937-4777-a603-e1d89126aab9"), new Guid("e581f50c-f3e4-4f20-b5da-4c3a83459208") },
                    { new Guid("6714ec3b-360c-4094-b046-7c1f2ae443cd"), new Guid("e589b4bb-584b-42a7-a87a-a83bc143219f") },
                    { new Guid("761f831e-450d-420c-bcd9-53224fff8669"), new Guid("e738bb32-daf9-4551-9f72-f37d2527cb11") },
                    { new Guid("14cff2e1-ef04-4193-939a-59463107d17d"), new Guid("e7424d3a-e081-43e0-aec2-d80fb31d60ea") },
                    { new Guid("6714ec3b-360c-4094-b046-7c1f2ae443cd"), new Guid("e8411f0a-3bae-41cd-ac0c-e6765bd0c4b1") },
                    { new Guid("6714ec3b-360c-4094-b046-7c1f2ae443cd"), new Guid("edbade78-a624-4bfc-af03-b433cf3667bc") },
                    { new Guid("6714ec3b-360c-4094-b046-7c1f2ae443cd"), new Guid("eefcbdc3-457e-48c0-b1c7-e39b1a9579de") },
                    { new Guid("d45db776-7db7-405b-9988-26166dbb95df"), new Guid("f0073747-08ad-4c31-a5c4-a83c6a3ac620") },
                    { new Guid("14cff2e1-ef04-4193-939a-59463107d17d"), new Guid("f3ee3df2-22aa-4492-8097-cb376d5db11b") },
                    { new Guid("4d596701-c9e7-4f05-9a41-c900f82092bc"), new Guid("fc4e2e14-586a-4c0f-b0ed-790ebc2afdde") }
                });

            migrationBuilder.InsertData(
                table: "CatalogServices",
                columns: new[] { "Id", "ApplicableTo", "CategoryId", "Code", "CountryCode", "DefaultRecurrenceType", "DisplayName", "IsActive", "IsPopular", "LogoKey", "SearchKeywords", "SearchText", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("144ebaba-c62e-41cc-8ea4-2ecf8a809e2c"), 2, new Guid("a8901ac8-2d92-42da-afea-8cf63d1d6dd5"), "the-economist", null, null, "The Economist", true, false, null, "ekonomi dergi digital print", "the economist the-economist ekonomi dergi digital print dijital dergi", 3010 },
                    { new Guid("367bed1e-604e-4a0b-a647-efa82c7b6011"), 2, new Guid("a8901ac8-2d92-42da-afea-8cf63d1d6dd5"), "medium", null, null, "Medium", true, false, null, "membership teknoloji yazı", "medium medium membership teknoloji yazi dijital dergi", 3020 },
                    { new Guid("4a07d96e-2111-4ad3-81dd-415e8e56a6bb"), 2, new Guid("a6f3a99d-9120-4dd9-9d15-9340f4f08a22"), "new-york-times", null, null, "The New York Times", true, false, null, "NYT gazete news", "the new york times new-york-times nyt gazete news dijital gazete", 2970 },
                    { new Guid("58ad7b33-96ef-4618-9633-44f5a9a68c89"), 2, new Guid("a6f3a99d-9120-4dd9-9d15-9340f4f08a22"), "financial-times", null, null, "Financial Times", true, false, null, "FT ekonomi finance", "financial times financial-times ft ekonomi finance dijital gazete", 2990 },
                    { new Guid("a8dc347c-66f1-4353-ae73-a3b426a83242"), 2, new Guid("6cc82a5b-5f94-4532-b9f6-7b0bc44045fd"), "substack", null, null, "Substack", true, false, null, "ücretli yayıncı bülten paid newsletter", "substack substack ucretli yayinci bulten paid newsletter haber bultenleri", 3030 },
                    { new Guid("b9c2df68-630b-4ece-a402-bf922bd4d66c"), 2, new Guid("f3c938b2-555f-4029-994d-564745f3e9ad"), "bloomberg", null, null, "Bloomberg", true, false, null, "ekonomi finans digital access", "bloomberg bloomberg ekonomi finans digital access ekonomi ve finans haberleri", 3000 },
                    { new Guid("bfe1c7e5-2571-4898-9387-59e38436d1dc"), 2, new Guid("a6f3a99d-9120-4dd9-9d15-9340f4f08a22"), "wall-street-journal", null, null, "The Wall Street Journal", true, false, null, "WSJ ekonomi finance", "the wall street journal wall-street-journal wsj ekonomi finance dijital gazete", 2980 }
                });

            migrationBuilder.InsertData(
                table: "CatalogServiceCategories",
                columns: new[] { "CategoryId", "ServiceId" },
                values: new object[,]
                {
                    { new Guid("2b0e36a4-2718-4746-abe6-735ef1e9d140"), new Guid("144ebaba-c62e-41cc-8ea4-2ecf8a809e2c") },
                    { new Guid("8c56a67b-2905-423f-96e9-4d8da4db583c"), new Guid("144ebaba-c62e-41cc-8ea4-2ecf8a809e2c") },
                    { new Guid("8d1bdf4a-4a4f-41ac-8ca4-3243c39ee9c2"), new Guid("144ebaba-c62e-41cc-8ea4-2ecf8a809e2c") },
                    { new Guid("a8901ac8-2d92-42da-afea-8cf63d1d6dd5"), new Guid("144ebaba-c62e-41cc-8ea4-2ecf8a809e2c") },
                    { new Guid("f3c938b2-555f-4029-994d-564745f3e9ad"), new Guid("144ebaba-c62e-41cc-8ea4-2ecf8a809e2c") },
                    { new Guid("103d9e4d-bb6d-4d34-89d6-7b73f8bfdd82"), new Guid("367bed1e-604e-4a0b-a647-efa82c7b6011") },
                    { new Guid("775ec693-90f2-4b84-8ad7-f82976278ddf"), new Guid("367bed1e-604e-4a0b-a647-efa82c7b6011") },
                    { new Guid("8c56a67b-2905-423f-96e9-4d8da4db583c"), new Guid("367bed1e-604e-4a0b-a647-efa82c7b6011") },
                    { new Guid("a8901ac8-2d92-42da-afea-8cf63d1d6dd5"), new Guid("367bed1e-604e-4a0b-a647-efa82c7b6011") },
                    { new Guid("8c56a67b-2905-423f-96e9-4d8da4db583c"), new Guid("4a07d96e-2111-4ad3-81dd-415e8e56a6bb") },
                    { new Guid("8d1bdf4a-4a4f-41ac-8ca4-3243c39ee9c2"), new Guid("4a07d96e-2111-4ad3-81dd-415e8e56a6bb") },
                    { new Guid("a6f3a99d-9120-4dd9-9d15-9340f4f08a22"), new Guid("4a07d96e-2111-4ad3-81dd-415e8e56a6bb") },
                    { new Guid("8c56a67b-2905-423f-96e9-4d8da4db583c"), new Guid("58ad7b33-96ef-4618-9633-44f5a9a68c89") },
                    { new Guid("8d1bdf4a-4a4f-41ac-8ca4-3243c39ee9c2"), new Guid("58ad7b33-96ef-4618-9633-44f5a9a68c89") },
                    { new Guid("a6f3a99d-9120-4dd9-9d15-9340f4f08a22"), new Guid("58ad7b33-96ef-4618-9633-44f5a9a68c89") },
                    { new Guid("f3c938b2-555f-4029-994d-564745f3e9ad"), new Guid("58ad7b33-96ef-4618-9633-44f5a9a68c89") },
                    { new Guid("6cc82a5b-5f94-4532-b9f6-7b0bc44045fd"), new Guid("a8dc347c-66f1-4353-ae73-a3b426a83242") },
                    { new Guid("8c56a67b-2905-423f-96e9-4d8da4db583c"), new Guid("a8dc347c-66f1-4353-ae73-a3b426a83242") },
                    { new Guid("8c56a67b-2905-423f-96e9-4d8da4db583c"), new Guid("b9c2df68-630b-4ece-a402-bf922bd4d66c") },
                    { new Guid("8d1bdf4a-4a4f-41ac-8ca4-3243c39ee9c2"), new Guid("b9c2df68-630b-4ece-a402-bf922bd4d66c") },
                    { new Guid("a6f3a99d-9120-4dd9-9d15-9340f4f08a22"), new Guid("b9c2df68-630b-4ece-a402-bf922bd4d66c") },
                    { new Guid("f3c938b2-555f-4029-994d-564745f3e9ad"), new Guid("b9c2df68-630b-4ece-a402-bf922bd4d66c") },
                    { new Guid("8c56a67b-2905-423f-96e9-4d8da4db583c"), new Guid("bfe1c7e5-2571-4898-9387-59e38436d1dc") },
                    { new Guid("8d1bdf4a-4a4f-41ac-8ca4-3243c39ee9c2"), new Guid("bfe1c7e5-2571-4898-9387-59e38436d1dc") },
                    { new Guid("a6f3a99d-9120-4dd9-9d15-9340f4f08a22"), new Guid("bfe1c7e5-2571-4898-9387-59e38436d1dc") },
                    { new Guid("f3c938b2-555f-4029-994d-564745f3e9ad"), new Guid("bfe1c7e5-2571-4898-9387-59e38436d1dc") }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("0c712d88-d2dc-4b8c-bea7-8b0d0c83f64e"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("1443b332-65ae-4dee-8516-163611721787"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("29020bd2-2e26-42f2-ad8a-853726892fd6"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("3c960efc-061d-4fa1-90e3-91e2469f86f7"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("3dcf71b4-1022-4b6c-83f5-dfbeeb78cd5a"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("40405271-2f01-40bf-8d24-dce932f99dd1"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("5f390075-6df9-4944-88e5-6d2c20a1a7a4"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("6bc3778d-d803-4d6f-9fd7-6707989d411b"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("797fd1da-9053-43b5-8236-ecc698081821"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("7b31924b-95f0-4d4f-86b2-0882ee74f037"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("7bc49d86-6863-4849-bf5e-97352d9bdbfa"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("99af86dc-d903-45b3-832a-ebfcf434b2a4"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("a5219530-b0b2-4b70-a117-1723f629e801"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("ae96245e-612e-48eb-8cf9-a19571bc907c"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("b2b60aed-aa7d-4018-b594-dbe81fae50dd"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("d8f14d9b-788e-4f8f-881f-f30e46aebd38"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("e17b2055-d350-47ff-93be-a8e1e7fdf7ac"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("e64a71f1-4344-44a5-b510-ad89dc899dfc"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("ecfa12c2-b8c6-4508-ba5a-6e94dc4e9ffc"));

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("be2f4901-0638-411d-bb10-d566005aec61"), new Guid("0121de3c-8fc3-4e08-b551-0570e4ca47b1") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("4d596701-c9e7-4f05-9a41-c900f82092bc"), new Guid("0c04850b-ac18-4077-8252-fac3c762658a") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("6714ec3b-360c-4094-b046-7c1f2ae443cd"), new Guid("0c04850b-ac18-4077-8252-fac3c762658a") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("105b7a80-58f6-4202-adf3-b0ec7df0853b"), new Guid("13795c93-773f-4ab5-8a51-fc6cc1be8415") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("2b0e36a4-2718-4746-abe6-735ef1e9d140"), new Guid("144ebaba-c62e-41cc-8ea4-2ecf8a809e2c") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("8c56a67b-2905-423f-96e9-4d8da4db583c"), new Guid("144ebaba-c62e-41cc-8ea4-2ecf8a809e2c") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("8d1bdf4a-4a4f-41ac-8ca4-3243c39ee9c2"), new Guid("144ebaba-c62e-41cc-8ea4-2ecf8a809e2c") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("a8901ac8-2d92-42da-afea-8cf63d1d6dd5"), new Guid("144ebaba-c62e-41cc-8ea4-2ecf8a809e2c") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("f3c938b2-555f-4029-994d-564745f3e9ad"), new Guid("144ebaba-c62e-41cc-8ea4-2ecf8a809e2c") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("05a956ce-0c6b-42fc-bc75-ca1f28e55120"), new Guid("1475e83f-59eb-4eb7-98e8-1c0adf218900") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("6f478f47-93b5-41ac-90ca-354e0d4314e4"), new Guid("1475e83f-59eb-4eb7-98e8-1c0adf218900") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("be2f4901-0638-411d-bb10-d566005aec61"), new Guid("1b0da2d1-7da6-4d2e-8d78-66238422739b") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("1956109e-3bc0-4d72-8190-e858c32b7055"), new Guid("1b5b1522-80fc-45d0-bfb6-a15cacbc763f") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("120d4e70-5475-4b00-85ff-c735ac5dac7c"), new Guid("285791f2-abc1-4d8c-b87e-461daf90ebab") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("4b284088-f9d6-43a0-bdd9-4f488bb6d67a"), new Guid("3007de3f-25f3-42b3-ae6b-4e6cc46a8ebf") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("406de719-32b2-4100-b2f4-ae74d635b03f"), new Guid("31be357e-60bd-4674-92ff-f1ca9d371ec0") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("103d9e4d-bb6d-4d34-89d6-7b73f8bfdd82"), new Guid("367bed1e-604e-4a0b-a647-efa82c7b6011") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("775ec693-90f2-4b84-8ad7-f82976278ddf"), new Guid("367bed1e-604e-4a0b-a647-efa82c7b6011") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("8c56a67b-2905-423f-96e9-4d8da4db583c"), new Guid("367bed1e-604e-4a0b-a647-efa82c7b6011") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("a8901ac8-2d92-42da-afea-8cf63d1d6dd5"), new Guid("367bed1e-604e-4a0b-a647-efa82c7b6011") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("caef747a-7a1f-4407-a714-40c446666d7d"), new Guid("37e55a59-6067-4afd-98ee-d9f1585fc3c9") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("2f9d9b00-84e9-46cd-ad24-b11a1c368889"), new Guid("38e10d5a-73d4-4824-82bd-e7cc8f0daa1e") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("f84e1762-a4a1-447b-a0ea-c7fb0193219c"), new Guid("3d168272-0b2d-44bf-8624-acb90fb804a6") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("892b2f2b-d6de-4475-b869-1b101f320534"), new Guid("3df58be1-df63-4df2-826d-537879bfb328") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("05d5e9ec-afff-495e-9541-52416b37f28e"), new Guid("3eb0c555-3aff-4e7f-8f8d-f728bca22371") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("4b284088-f9d6-43a0-bdd9-4f488bb6d67a"), new Guid("46a7611b-4ebf-4800-93d2-767cabb614c2") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("8c56a67b-2905-423f-96e9-4d8da4db583c"), new Guid("4a07d96e-2111-4ad3-81dd-415e8e56a6bb") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("8d1bdf4a-4a4f-41ac-8ca4-3243c39ee9c2"), new Guid("4a07d96e-2111-4ad3-81dd-415e8e56a6bb") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("a6f3a99d-9120-4dd9-9d15-9340f4f08a22"), new Guid("4a07d96e-2111-4ad3-81dd-415e8e56a6bb") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("be2f4901-0638-411d-bb10-d566005aec61"), new Guid("4be1f486-1b59-46a8-b836-928045dd782c") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("d45db776-7db7-405b-9988-26166dbb95df"), new Guid("4c8fbfca-8eb3-4771-9935-168302bdb1d8") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("c2790a5d-538c-4f1e-9e0b-62b7581c0fcf"), new Guid("4e0a6b80-9847-45fa-9000-643e7ecacb51") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("e8db84b2-4ece-4dbe-abed-70e6c8e0b7af"), new Guid("4e8c3109-5509-44fa-b877-ed5452656790") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("6714ec3b-360c-4094-b046-7c1f2ae443cd"), new Guid("4f06f017-1530-4506-a976-b2aa4e2200a4") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("8c56a67b-2905-423f-96e9-4d8da4db583c"), new Guid("58ad7b33-96ef-4618-9633-44f5a9a68c89") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("8d1bdf4a-4a4f-41ac-8ca4-3243c39ee9c2"), new Guid("58ad7b33-96ef-4618-9633-44f5a9a68c89") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("a6f3a99d-9120-4dd9-9d15-9340f4f08a22"), new Guid("58ad7b33-96ef-4618-9633-44f5a9a68c89") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("f3c938b2-555f-4029-994d-564745f3e9ad"), new Guid("58ad7b33-96ef-4618-9633-44f5a9a68c89") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("2ecb6935-3a3a-41e1-9a4c-64a357ec94ac"), new Guid("5b3cfe98-d787-4af2-b69e-7e81b7141d21") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("be2f4901-0638-411d-bb10-d566005aec61"), new Guid("610f081c-d754-4dd2-9f3c-3bc93d820b30") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("f7719fac-b111-42fe-b044-4179db523637"), new Guid("6313d5f8-bb28-4822-9375-fbb2d2c50a9d") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("2e45a6c8-363e-4d61-a10c-29763cbea936"), new Guid("634a919e-3d90-44d2-97fe-2320c9dd17db") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("14cff2e1-ef04-4193-939a-59463107d17d"), new Guid("6762f6eb-06fd-49ea-a308-f25159b0fef8") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("120d4e70-5475-4b00-85ff-c735ac5dac7c"), new Guid("6b15a674-645b-4b8c-a134-a20d618973b4") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("761f831e-450d-420c-bcd9-53224fff8669"), new Guid("6eddd662-95db-4d5d-bfe3-eaf639919693") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("fa6624eb-3539-45d1-8308-56ac06b9ba4e"), new Guid("745d52b4-6770-44b3-801e-8ba15b6c1828") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("caef747a-7a1f-4407-a714-40c446666d7d"), new Guid("74ad3446-0890-4fae-bea1-c24681b97100") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("af5f00bc-030b-4804-a833-14eb77800e06"), new Guid("769082a7-ff15-4116-b6c3-b062aa6b3c6f") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("e29a3935-2b05-497a-8203-0eb978e5a7fe"), new Guid("7c36c1a0-cd2d-44be-a40e-51e1b0b23443") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("caef747a-7a1f-4407-a714-40c446666d7d"), new Guid("7f36f1ed-8972-4bbf-8085-6f789fac0378") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("d6355b47-1900-4cd1-9a40-d3158167f3d5"), new Guid("87a62664-af83-4687-b0bc-53f58635ed20") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("2ecb6935-3a3a-41e1-9a4c-64a357ec94ac"), new Guid("8d3a4463-22ab-475b-a426-39a5f0f35cfd") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("6714ec3b-360c-4094-b046-7c1f2ae443cd"), new Guid("8f505405-b286-4a61-a2fa-18428bed8134") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("c2790a5d-538c-4f1e-9e0b-62b7581c0fcf"), new Guid("9014b93e-7b76-46a2-9f1b-c101dfcbb3e1") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("6714ec3b-360c-4094-b046-7c1f2ae443cd"), new Guid("91a26ebb-34f6-42cd-a130-39d2f764b0bc") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("2f9d9b00-84e9-46cd-ad24-b11a1c368889"), new Guid("93c31242-b92b-49d7-b5b3-2908d3f37c60") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("f84e1762-a4a1-447b-a0ea-c7fb0193219c"), new Guid("958da99c-35c5-4461-b486-6893d0d5095f") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("892b2f2b-d6de-4475-b869-1b101f320534"), new Guid("96e55ea8-00d8-46eb-936e-1b5ce5a6f121") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("a9285b52-c39f-4c12-a41a-367f3e89475f"), new Guid("97776faf-91f4-4836-b95c-8990032b75b8") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("120d4e70-5475-4b00-85ff-c735ac5dac7c"), new Guid("9ad446db-bea1-454a-adec-62e34c581de3") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("4b284088-f9d6-43a0-bdd9-4f488bb6d67a"), new Guid("9bbb7d25-a481-4a1c-981b-2528ddc5055a") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("8e8ffbdb-be95-43df-bf2d-11a4637699ee"), new Guid("9bd05373-5997-46e1-99c2-a7fa48f7a040") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("14cff2e1-ef04-4193-939a-59463107d17d"), new Guid("9e72cc7d-e299-497f-8d87-a9a745a4adc9") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("2f9d9b00-84e9-46cd-ad24-b11a1c368889"), new Guid("9f66fecb-5746-4edf-98d8-b6f85ef61307") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("6714ec3b-360c-4094-b046-7c1f2ae443cd"), new Guid("9f66fecb-5746-4edf-98d8-b6f85ef61307") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("892b2f2b-d6de-4475-b869-1b101f320534"), new Guid("9f66fecb-5746-4edf-98d8-b6f85ef61307") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("a7600074-8ee1-45e2-94e1-ee712b1f5626"), new Guid("a021683b-b248-4faa-aebd-2bfe3f82e49e") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("761f831e-450d-420c-bcd9-53224fff8669"), new Guid("a1524a4b-c16a-46db-99b0-95c7325d9df5") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("2ecb6935-3a3a-41e1-9a4c-64a357ec94ac"), new Guid("a15f60ac-6bc0-4e6a-87e4-80eafb8587b5") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("4b284088-f9d6-43a0-bdd9-4f488bb6d67a"), new Guid("a64ad571-dfda-43d9-a866-4d5b99a85ce3") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("a1a46632-73f9-4863-943e-b0c393419c21"), new Guid("a64ad571-dfda-43d9-a866-4d5b99a85ce3") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("4c48e94d-7207-49b3-8d6f-e3f194847062"), new Guid("a8593268-529d-43ed-9013-03df091ce03c") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("6cc82a5b-5f94-4532-b9f6-7b0bc44045fd"), new Guid("a8dc347c-66f1-4353-ae73-a3b426a83242") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("8c56a67b-2905-423f-96e9-4d8da4db583c"), new Guid("a8dc347c-66f1-4353-ae73-a3b426a83242") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("caef747a-7a1f-4407-a714-40c446666d7d"), new Guid("b3aa473e-0d76-451c-9a14-b32a475714eb") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("4d596701-c9e7-4f05-9a41-c900f82092bc"), new Guid("b78be435-cef9-4a6a-a694-1092aa6705a3") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("8c56a67b-2905-423f-96e9-4d8da4db583c"), new Guid("b9c2df68-630b-4ece-a402-bf922bd4d66c") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("8d1bdf4a-4a4f-41ac-8ca4-3243c39ee9c2"), new Guid("b9c2df68-630b-4ece-a402-bf922bd4d66c") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("a6f3a99d-9120-4dd9-9d15-9340f4f08a22"), new Guid("b9c2df68-630b-4ece-a402-bf922bd4d66c") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("f3c938b2-555f-4029-994d-564745f3e9ad"), new Guid("b9c2df68-630b-4ece-a402-bf922bd4d66c") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("105b7a80-58f6-4202-adf3-b0ec7df0853b"), new Guid("ba340f4a-0337-4fd0-8af0-63fd75dbe3b1") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("4b284088-f9d6-43a0-bdd9-4f488bb6d67a"), new Guid("ba6716c0-6839-4b95-8940-8e30628cc0c2") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("a1a46632-73f9-4863-943e-b0c393419c21"), new Guid("ba6716c0-6839-4b95-8940-8e30628cc0c2") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("892b2f2b-d6de-4475-b869-1b101f320534"), new Guid("bd9373d2-2179-47fe-8eb8-cbd7a9907aa8") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("2ecb6935-3a3a-41e1-9a4c-64a357ec94ac"), new Guid("bfddbd4d-876f-4fe8-b493-f5231415917b") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("8c56a67b-2905-423f-96e9-4d8da4db583c"), new Guid("bfe1c7e5-2571-4898-9387-59e38436d1dc") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("8d1bdf4a-4a4f-41ac-8ca4-3243c39ee9c2"), new Guid("bfe1c7e5-2571-4898-9387-59e38436d1dc") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("a6f3a99d-9120-4dd9-9d15-9340f4f08a22"), new Guid("bfe1c7e5-2571-4898-9387-59e38436d1dc") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("f3c938b2-555f-4029-994d-564745f3e9ad"), new Guid("bfe1c7e5-2571-4898-9387-59e38436d1dc") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("6714ec3b-360c-4094-b046-7c1f2ae443cd"), new Guid("c2203979-99db-4ccc-84e7-6587bef4ccad") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("3b06cb49-6beb-4d76-9615-5bc8895b9825"), new Guid("c336a593-2c19-4ce8-97cd-27935fda4a0c") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("761f831e-450d-420c-bcd9-53224fff8669"), new Guid("c41ad125-cd33-4e04-bcc9-8d7d28d82ccb") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("14cff2e1-ef04-4193-939a-59463107d17d"), new Guid("c69eb12b-14f7-4b8f-9b88-acae04446d69") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("6714ec3b-360c-4094-b046-7c1f2ae443cd"), new Guid("c8f4f69e-eb68-4782-a655-616892afc86c") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("4b284088-f9d6-43a0-bdd9-4f488bb6d67a"), new Guid("ca6548cf-887c-462e-8d47-545663a4af60") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("892b2f2b-d6de-4475-b869-1b101f320534"), new Guid("ce5d291e-a62e-4165-8cf0-d4f8fadbc817") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("4b284088-f9d6-43a0-bdd9-4f488bb6d67a"), new Guid("d0f065b8-6224-4be9-b0dc-97b5887f597e") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("a1a46632-73f9-4863-943e-b0c393419c21"), new Guid("d0f065b8-6224-4be9-b0dc-97b5887f597e") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("caef747a-7a1f-4407-a714-40c446666d7d"), new Guid("d758dc03-6e00-4319-9293-131bf7888a32") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("2f9d9b00-84e9-46cd-ad24-b11a1c368889"), new Guid("d887714e-a518-46c0-8ae7-9633f25d031f") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("1956109e-3bc0-4d72-8190-e858c32b7055"), new Guid("d9f4d7c8-c234-4cd2-815b-6100896ba386") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("a7600074-8ee1-45e2-94e1-ee712b1f5626"), new Guid("dcf84e50-34d8-4856-baf0-6fa5559d6504") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("caef747a-7a1f-4407-a714-40c446666d7d"), new Guid("e52c79dd-5738-4838-bdbd-6e332092adb2") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("99ae4b81-6937-4777-a603-e1d89126aab9"), new Guid("e581f50c-f3e4-4f20-b5da-4c3a83459208") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("6714ec3b-360c-4094-b046-7c1f2ae443cd"), new Guid("e589b4bb-584b-42a7-a87a-a83bc143219f") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("761f831e-450d-420c-bcd9-53224fff8669"), new Guid("e738bb32-daf9-4551-9f72-f37d2527cb11") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("14cff2e1-ef04-4193-939a-59463107d17d"), new Guid("e7424d3a-e081-43e0-aec2-d80fb31d60ea") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("6714ec3b-360c-4094-b046-7c1f2ae443cd"), new Guid("e8411f0a-3bae-41cd-ac0c-e6765bd0c4b1") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("6714ec3b-360c-4094-b046-7c1f2ae443cd"), new Guid("edbade78-a624-4bfc-af03-b433cf3667bc") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("6714ec3b-360c-4094-b046-7c1f2ae443cd"), new Guid("eefcbdc3-457e-48c0-b1c7-e39b1a9579de") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("d45db776-7db7-405b-9988-26166dbb95df"), new Guid("f0073747-08ad-4c31-a5c4-a83c6a3ac620") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("14cff2e1-ef04-4193-939a-59463107d17d"), new Guid("f3ee3df2-22aa-4492-8097-cb376d5db11b") });

            migrationBuilder.DeleteData(
                table: "CatalogServiceCategories",
                keyColumns: new[] { "CategoryId", "ServiceId" },
                keyValues: new object[] { new Guid("4d596701-c9e7-4f05-9a41-c900f82092bc"), new Guid("fc4e2e14-586a-4c0f-b0ed-790ebc2afdde") });

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("05a956ce-0c6b-42fc-bc75-ca1f28e55120"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("05d5e9ec-afff-495e-9541-52416b37f28e"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("103d9e4d-bb6d-4d34-89d6-7b73f8bfdd82"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("105b7a80-58f6-4202-adf3-b0ec7df0853b"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("120d4e70-5475-4b00-85ff-c735ac5dac7c"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("14cff2e1-ef04-4193-939a-59463107d17d"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("1956109e-3bc0-4d72-8190-e858c32b7055"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("2b0e36a4-2718-4746-abe6-735ef1e9d140"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("2e45a6c8-363e-4d61-a10c-29763cbea936"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("2ecb6935-3a3a-41e1-9a4c-64a357ec94ac"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("2f9d9b00-84e9-46cd-ad24-b11a1c368889"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("3b06cb49-6beb-4d76-9615-5bc8895b9825"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("406de719-32b2-4100-b2f4-ae74d635b03f"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("4b284088-f9d6-43a0-bdd9-4f488bb6d67a"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("4c48e94d-7207-49b3-8d6f-e3f194847062"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("4d596701-c9e7-4f05-9a41-c900f82092bc"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("6714ec3b-360c-4094-b046-7c1f2ae443cd"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("6f478f47-93b5-41ac-90ca-354e0d4314e4"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("761f831e-450d-420c-bcd9-53224fff8669"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("775ec693-90f2-4b84-8ad7-f82976278ddf"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("892b2f2b-d6de-4475-b869-1b101f320534"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("8d1bdf4a-4a4f-41ac-8ca4-3243c39ee9c2"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("8e8ffbdb-be95-43df-bf2d-11a4637699ee"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("99ae4b81-6937-4777-a603-e1d89126aab9"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("a1a46632-73f9-4863-943e-b0c393419c21"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("a7600074-8ee1-45e2-94e1-ee712b1f5626"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("a9285b52-c39f-4c12-a41a-367f3e89475f"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("af5f00bc-030b-4804-a833-14eb77800e06"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("be2f4901-0638-411d-bb10-d566005aec61"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("c2790a5d-538c-4f1e-9e0b-62b7581c0fcf"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("d45db776-7db7-405b-9988-26166dbb95df"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("d6355b47-1900-4cd1-9a40-d3158167f3d5"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("e29a3935-2b05-497a-8203-0eb978e5a7fe"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("e8db84b2-4ece-4dbe-abed-70e6c8e0b7af"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("f7719fac-b111-42fe-b044-4179db523637"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("f84e1762-a4a1-447b-a0ea-c7fb0193219c"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("fa6624eb-3539-45d1-8308-56ac06b9ba4e"));

            migrationBuilder.DeleteData(
                table: "CatalogServices",
                keyColumn: "Id",
                keyValue: new Guid("144ebaba-c62e-41cc-8ea4-2ecf8a809e2c"));

            migrationBuilder.DeleteData(
                table: "CatalogServices",
                keyColumn: "Id",
                keyValue: new Guid("367bed1e-604e-4a0b-a647-efa82c7b6011"));

            migrationBuilder.DeleteData(
                table: "CatalogServices",
                keyColumn: "Id",
                keyValue: new Guid("4a07d96e-2111-4ad3-81dd-415e8e56a6bb"));

            migrationBuilder.DeleteData(
                table: "CatalogServices",
                keyColumn: "Id",
                keyValue: new Guid("58ad7b33-96ef-4618-9633-44f5a9a68c89"));

            migrationBuilder.DeleteData(
                table: "CatalogServices",
                keyColumn: "Id",
                keyValue: new Guid("a8dc347c-66f1-4353-ae73-a3b426a83242"));

            migrationBuilder.DeleteData(
                table: "CatalogServices",
                keyColumn: "Id",
                keyValue: new Guid("b9c2df68-630b-4ece-a402-bf922bd4d66c"));

            migrationBuilder.DeleteData(
                table: "CatalogServices",
                keyColumn: "Id",
                keyValue: new Guid("bfe1c7e5-2571-4898-9387-59e38436d1dc"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("6cc82a5b-5f94-4532-b9f6-7b0bc44045fd"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("a6f3a99d-9120-4dd9-9d15-9340f4f08a22"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("a8901ac8-2d92-42da-afea-8cf63d1d6dd5"));

            migrationBuilder.DeleteData(
                table: "CatalogCategories",
                keyColumn: "Id",
                keyValue: new Guid("f3c938b2-555f-4029-994d-564745f3e9ad"));
        }
    }
}
