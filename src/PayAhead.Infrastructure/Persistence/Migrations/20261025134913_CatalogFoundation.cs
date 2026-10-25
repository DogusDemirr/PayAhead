using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace PayAhead.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CatalogFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CatalogCategoryId",
                table: "PaymentPlans",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CatalogServiceId",
                table: "PaymentPlans",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CatalogCategories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Code = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    NameTr = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IconKey = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ApplicableTo = table.Column<int>(type: "int", nullable: false),
                    SearchText = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogCategories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatalogCategories_CatalogCategories_ParentId",
                        column: x => x.ParentId,
                        principalTable: "CatalogCategories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CatalogServices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "varchar(80)", unicode: false, maxLength: 80, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LogoKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    SearchKeywords = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SearchText = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsPopular = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ApplicableTo = table.Column<int>(type: "int", nullable: false),
                    DefaultRecurrenceType = table.Column<int>(type: "int", nullable: true),
                    CountryCode = table.Column<string>(type: "varchar(2)", unicode: false, maxLength: 2, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogServices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CatalogServices_CatalogCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "CatalogCategories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CatalogServiceCategories",
                columns: table => new
                {
                    ServiceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CatalogServiceCategories", x => new { x.ServiceId, x.CategoryId });
                    table.ForeignKey(
                        name: "FK_CatalogServiceCategories_CatalogCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "CatalogCategories",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_CatalogServiceCategories_CatalogServices_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "CatalogServices",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                table: "CatalogCategories",
                columns: new[] { "Id", "ApplicableTo", "Code", "IconKey", "IsActive", "NameEn", "NameTr", "ParentId", "SearchText", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("03854818-24a2-4656-b72c-572da5e1661a"), 0, "groceries", "groceries", true, "Groceries and food", "Market ve Gıda", null, "market ve gida groceries and food groceries", 160 },
                    { new Guid("18acd618-5e34-4f9c-821c-87a7921ad1ad"), 0, "home", "home", true, "Home and rent", "Ev ve Kira", null, "ev ve kira home and rent home", 110 },
                    { new Guid("1e00565d-0e1d-4807-ace9-105b374652fe"), 2, "other", "other", true, "Other", "Diğer", null, "diger other other", 240 },
                    { new Guid("2b33d075-b932-45a9-b02a-3df8e0f0a2ec"), 2, "digital", "digital", true, "Digital subscriptions", "Dijital Abonelikler", null, "dijital abonelikler digital subscriptions digital", 0 },
                    { new Guid("43133d20-4b41-4423-bdb1-6e4c9646956c"), 0, "finance", "finance", true, "Banking and finance", "Banka, Kredi ve Finans", null, "banka, kredi ve finans banking and finance finance", 140 },
                    { new Guid("50dde9cd-647d-4472-b263-ecc52cccff31"), 0, "personal", "personal", true, "Personal care", "Kişisel Bakım", null, "kisisel bakim personal care personal", 210 },
                    { new Guid("7473e800-2311-4ac6-9631-18fab4784f33"), 0, "tax", "tax", true, "Taxes and official payments", "Vergi ve Resmî Ödemeler", null, "vergi ve resmi odemeler taxes and official payments tax", 180 },
                    { new Guid("75330075-0a0e-4edb-ae9a-3b09df6c2515"), 2, "fitness", "fitness", true, "Sport and fitness", "Spor ve Fitness", null, "spor ve fitness sport and fitness fitness", 80 },
                    { new Guid("8b23d004-bf0c-498e-afa9-dc2ca67c1af6"), 0, "transport", "transport", true, "Vehicle and transport", "Araç ve Ulaşım", null, "arac ve ulasim vehicle and transport transport", 120 },
                    { new Guid("8c56a67b-2905-423f-96e9-4d8da4db583c"), 2, "news", "news", true, "News and publishing", "Haber ve Yayıncılık", null, "haber ve yayincilik news and publishing news", 190 },
                    { new Guid("a6f72f7d-c561-4496-865a-ee522dcae63f"), 0, "health", "health", true, "Health", "Sağlık", null, "saglik health health", 170 },
                    { new Guid("aa885dfe-203f-4f4c-b36a-c2e252fab7fb"), 0, "travel", "travel", true, "Travel and accommodation", "Seyahat ve Konaklama", null, "seyahat ve konaklama travel and accommodation travel", 220 },
                    { new Guid("ad374367-3485-44e7-bc59-0daf55262b74"), 2, "telecom", "telecom", true, "Internet and telecom", "İnternet ve Telekom", null, "internet ve telekom internet and telecom telecom", 90 },
                    { new Guid("bf324a2d-f3f4-4e5f-966d-6198e8dc4ac8"), 2, "shopping", "shopping", true, "Shopping and memberships", "Alışveriş ve Üyelikler", null, "alisveris ve uyelikler shopping and memberships shopping", 150 },
                    { new Guid("c0f444ee-12ed-43a2-98df-9f61340ca7bc"), 0, "family", "family", true, "Family and children", "Aile ve Çocuk", null, "aile ve cocuk family and children family", 200 },
                    { new Guid("c8b5b6ee-1204-4e3f-ac08-8d52be7377f1"), 2, "work", "work", true, "Work and professional services", "İş ve Profesyonel Hizmetler", null, "is ve profesyonel hizmetler work and professional services work", 230 },
                    { new Guid("ccf2543c-aa2f-4654-892e-b8720a04bd83"), 2, "education", "education", true, "Education", "Eğitim", null, "egitim education education", 70 },
                    { new Guid("e8b90166-a0f0-4cc4-8833-563025df402c"), 0, "insurance", "insurance", true, "Insurance", "Sigorta", null, "sigorta insurance insurance", 130 },
                    { new Guid("ff490f7e-4349-4185-8333-9eff77417302"), 0, "utilities", "utilities", true, "Utilities", "Elektrik, Su ve Doğalgaz", null, "elektrik, su ve dogalgaz utilities utilities", 100 },
                    { new Guid("03e235a7-8be8-48b0-8dd5-11d87ef057aa"), 0, "transport.public", "transport", true, "Public transport", "Toplu Taşıma", new Guid("8b23d004-bf0c-498e-afa9-dc2ca67c1af6"), "toplu tasima public transport transport.public", 460 },
                    { new Guid("0a4b8fa9-60ab-4b43-ac58-03d04c13e30a"), 0, "transport.roadside", "transport", true, "Roadside assistance", "Yol Yardım", new Guid("8b23d004-bf0c-498e-afa9-dc2ca67c1af6"), "yol yardim roadside assistance transport.roadside", 430 },
                    { new Guid("0ab03d4d-00b4-41eb-aa7c-8bd159f6e316"), 0, "transport.parts", "transport", true, "Spare parts", "Yedek Parça", new Guid("8b23d004-bf0c-498e-afa9-dc2ca67c1af6"), "yedek parca spare parts transport.parts", 320 },
                    { new Guid("0b05b51e-1b39-4252-936c-ad141dc2eca7"), 0, "transport.tolls", "transport", true, "Bridge and motorway", "Köprü ve Otoyol", new Guid("8b23d004-bf0c-498e-afa9-dc2ca67c1af6"), "kopru ve otoyol bridge and motorway transport.tolls", 400 },
                    { new Guid("0cf398fa-dcdc-466a-ac73-10346708ebf2"), 0, "utilities.water", "utilities", true, "Water bill", "Su Faturası", new Guid("ff490f7e-4349-4185-8333-9eff77417302"), "su faturasi water bill utilities.water", 520 },
                    { new Guid("191168e3-9df2-4fdd-81d8-d9d052ae0274"), 0, "utilities.electricity", "utilities", true, "Electricity bill", "Elektrik Faturası", new Guid("ff490f7e-4349-4185-8333-9eff77417302"), "elektrik faturasi electricity bill utilities.electricity", 510 },
                    { new Guid("1af3cc7d-80f0-426f-9603-17b944b6acad"), 0, "transport.vehicle-tax", "transport", true, "Motor vehicle tax", "Motorlu Taşıtlar Vergisi", new Guid("8b23d004-bf0c-498e-afa9-dc2ca67c1af6"), "motorlu tasitlar vergisi motor vehicle tax transport.vehicle-tax", 360 },
                    { new Guid("1dd72c50-47fd-4b26-ae0a-bf60027dc8ee"), 0, "transport.hgs", "transport", true, "HGS", "HGS", new Guid("8b23d004-bf0c-498e-afa9-dc2ca67c1af6"), "hgs hgs transport.hgs", 390 },
                    { new Guid("235d9b7c-07ae-4b74-b1ec-e43e29e66f08"), 0, "transport.maintenance", "transport", true, "Scheduled maintenance", "Periyodik Bakım", new Guid("8b23d004-bf0c-498e-afa9-dc2ca67c1af6"), "periyodik bakim scheduled maintenance transport.maintenance", 300 },
                    { new Guid("2d4d078c-a5fb-404e-99fa-ce63e6bfec92"), 0, "transport.tyres", "transport", true, "Tyres and wheels", "Lastik ve Jant", new Guid("8b23d004-bf0c-498e-afa9-dc2ca67c1af6"), "lastik ve jant tyres and wheels transport.tyres", 330 },
                    { new Guid("2dc8c7ab-920c-4b01-b191-9605039723fb"), 2, "music", "music", true, "Music and podcasts", "Müzik ve Podcast", new Guid("2b33d075-b932-45a9-b02a-3df8e0f0a2ec"), "muzik ve podcast music and podcasts music", 20 },
                    { new Guid("42edd8a7-533d-4b79-ac0f-d47e62dc6ab1"), 0, "home.rent", "home", true, "Rent", "Kira", new Guid("18acd618-5e34-4f9c-821c-87a7921ad1ad"), "kira rent home.rent", 490 },
                    { new Guid("472d4caa-0c65-467c-bee4-ae6d09546f71"), 0, "telecom.phone", "telecom", true, "Phone bill", "Telefon Faturası", new Guid("ad374367-3485-44e7-bc59-0daf55262b74"), "telefon faturasi phone bill telecom.phone", 550 },
                    { new Guid("56e5cd4f-10ee-4e7a-8761-af1cb8183daa"), 0, "transport.repair", "transport", true, "Vehicle repair", "Araç Tamiri", new Guid("8b23d004-bf0c-498e-afa9-dc2ca67c1af6"), "arac tamiri vehicle repair transport.repair", 310 },
                    { new Guid("5f1033c8-fa71-4005-80a6-880482c931ab"), 0, "home.dues", "home", true, "Maintenance dues", "Aidat", new Guid("18acd618-5e34-4f9c-821c-87a7921ad1ad"), "aidat maintenance dues home.dues", 500 },
                    { new Guid("5fe0fad6-83c4-4d71-9da1-6652e804b4f5"), 0, "transport.rental", "transport", true, "Vehicle rental", "Araç Kiralama", new Guid("8b23d004-bf0c-498e-afa9-dc2ca67c1af6"), "arac kiralama vehicle rental transport.rental", 440 },
                    { new Guid("6759dcee-1e8c-4189-b0a7-17bd1f916bd4"), 0, "transport.other", "transport", true, "Other vehicle costs", "Diğer Araç Giderleri", new Guid("8b23d004-bf0c-498e-afa9-dc2ca67c1af6"), "diger arac giderleri other vehicle costs transport.other", 480 },
                    { new Guid("6c29d470-7631-481b-a975-feefcb69688e"), 0, "transport.comprehensive-insurance", "transport", true, "Comprehensive insurance", "Kasko", new Guid("8b23d004-bf0c-498e-afa9-dc2ca67c1af6"), "kasko comprehensive insurance transport.comprehensive-insurance", 350 },
                    { new Guid("7d5de2a8-87e7-498c-926d-a1fe87b86ba0"), 0, "transport.charging", "transport", true, "EV charging", "Elektrikli Araç Şarjı", new Guid("8b23d004-bf0c-498e-afa9-dc2ca67c1af6"), "elektrikli arac sarji ev charging transport.charging", 290 },
                    { new Guid("8b92bbb1-183d-40fe-8512-de7ae23493d8"), 0, "transport.emissions", "transport", true, "Emissions inspection", "Egzoz Muayenesi", new Guid("8b23d004-bf0c-498e-afa9-dc2ca67c1af6"), "egzoz muayenesi emissions inspection transport.emissions", 380 },
                    { new Guid("90a47ca5-93a9-4b97-8d42-377193be7377"), 0, "utilities.gas", "utilities", true, "Gas bill", "Doğalgaz Faturası", new Guid("ff490f7e-4349-4185-8333-9eff77417302"), "dogalgaz faturasi gas bill utilities.gas", 530 },
                    { new Guid("9c945fb0-8444-4ad7-99fc-e15b5e1a5df0"), 2, "ai", "ai", true, "Artificial intelligence", "Yapay Zekâ", new Guid("2b33d075-b932-45a9-b02a-3df8e0f0a2ec"), "yapay zeka artificial intelligence ai", 40 },
                    { new Guid("a5689548-1d21-4b91-a509-5bf60ab54c80"), 2, "software", "software", true, "Software and productivity", "Yazılım ve Üretkenlik", new Guid("2b33d075-b932-45a9-b02a-3df8e0f0a2ec"), "yazilim ve uretkenlik software and productivity software", 50 },
                    { new Guid("a68e8dc7-7af4-4271-a93b-97513311f280"), 0, "transport.loan", "transport", true, "Vehicle loan", "Araç Kredisi", new Guid("8b23d004-bf0c-498e-afa9-dc2ca67c1af6"), "arac kredisi vehicle loan transport.loan", 450 },
                    { new Guid("b88c10bf-5502-4fdc-8ddf-f9b3640e8b2c"), 0, "transport.inspection", "transport", true, "Vehicle inspection", "Araç Muayenesi", new Guid("8b23d004-bf0c-498e-afa9-dc2ca67c1af6"), "arac muayenesi vehicle inspection transport.inspection", 370 },
                    { new Guid("bde9972a-b980-4e27-af68-5f4ee8d76c2e"), 0, "transport.traffic-insurance", "transport", true, "Traffic insurance", "Trafik Sigortası", new Guid("8b23d004-bf0c-498e-afa9-dc2ca67c1af6"), "trafik sigortasi traffic insurance transport.traffic-insurance", 340 },
                    { new Guid("c3f7efbd-2591-4237-9f4f-ff77648ba333"), 2, "gaming", "gaming", true, "Games and entertainment", "Oyun ve Eğlence", new Guid("2b33d075-b932-45a9-b02a-3df8e0f0a2ec"), "oyun ve eglence games and entertainment gaming", 30 },
                    { new Guid("caef747a-7a1f-4407-a714-40c446666d7d"), 0, "transport.fuel", "fuel", true, "Fuel", "Yakıt", new Guid("8b23d004-bf0c-498e-afa9-dc2ca67c1af6"), "yakit fuel transport.fuel", 250 },
                    { new Guid("db3088cf-333f-4107-8f73-095e2e53920f"), 0, "transport.parking", "transport", true, "Parking", "Otopark", new Guid("8b23d004-bf0c-498e-afa9-dc2ca67c1af6"), "otopark parking transport.parking", 410 },
                    { new Guid("db3d1318-93c7-4f1e-8b1c-5599ab0f1d70"), 0, "transport.taxi", "transport", true, "Taxi", "Taksi", new Guid("8b23d004-bf0c-498e-afa9-dc2ca67c1af6"), "taksi taxi transport.taxi", 470 },
                    { new Guid("e64ecf69-c259-42c3-bc3b-4bc80dfa37b9"), 2, "video", "video", true, "Film and TV", "Film ve Dizi", new Guid("2b33d075-b932-45a9-b02a-3df8e0f0a2ec"), "film ve dizi film and tv video", 10 },
                    { new Guid("ee6e4920-cb3d-4005-8651-95c5a3042db5"), 2, "cloud", "cloud", true, "Cloud and storage", "Bulut ve Depolama", new Guid("2b33d075-b932-45a9-b02a-3df8e0f0a2ec"), "bulut ve depolama cloud and storage cloud", 60 },
                    { new Guid("f19d27ea-74cb-4b2e-b9fe-488c2dfdbb99"), 0, "telecom.internet", "telecom", true, "Internet bill", "İnternet Faturası", new Guid("ad374367-3485-44e7-bc59-0daf55262b74"), "internet faturasi internet bill telecom.internet", 540 },
                    { new Guid("f7fa7a0f-5b7a-454a-8c43-3c75fd4999ad"), 0, "transport.wash", "transport", true, "Car wash", "Araç Yıkama", new Guid("8b23d004-bf0c-498e-afa9-dc2ca67c1af6"), "arac yikama car wash transport.wash", 420 }
                });

            migrationBuilder.InsertData(
                table: "CatalogServices",
                columns: new[] { "Id", "ApplicableTo", "CategoryId", "Code", "CountryCode", "DefaultRecurrenceType", "DisplayName", "IsActive", "IsPopular", "LogoKey", "SearchKeywords", "SearchText", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("0c04850b-ac18-4077-8252-fac3c762658a"), 2, new Guid("bf324a2d-f3f4-4e5f-966d-6198e8dc4ac8"), "amazon-prime", null, 3, "Amazon Prime", true, false, null, "", "amazon prime amazon-prime  alisveris ve uyelikler shopping and memberships", 400 },
                    { new Guid("13795c93-773f-4ab5-8a51-fc6cc1be8415"), 0, new Guid("43133d20-4b41-4423-bdb1-6e4c9646956c"), "konut-kredisi", "TR", 3, "Konut Kredisi", true, false, null, "", "konut kredisi konut-kredisi  banka, kredi ve finans banking and finance", 790 },
                    { new Guid("1475e83f-59eb-4eb7-98e8-1c0adf218900"), 0, new Guid("a6f72f7d-c561-4496-865a-ee522dcae63f"), "ozel-saglik-sigortasi", "TR", 4, "Özel Sağlık Sigortası", true, false, null, "", "ozel saglik sigortasi ozel-saglik-sigortasi  saglik health", 850 },
                    { new Guid("285791f2-abc1-4d8c-b87e-461daf90ebab"), 2, new Guid("75330075-0a0e-4edb-ae9a-3b09df6c2515"), "fitbod", null, 3, "Fitbod", true, false, null, "", "fitbod fitbod  spor ve fitness sport and fitness", 390 },
                    { new Guid("3007de3f-25f3-42b3-ae6b-4e6cc46a8ebf"), 2, new Guid("ad374367-3485-44e7-bc59-0daf55262b74"), "netspeed", "TR", 3, "Netspeed", true, false, null, "", "netspeed netspeed  internet ve telekom internet and telecom", 670 },
                    { new Guid("31be357e-60bd-4674-92ff-f1ca9d371ec0"), 0, new Guid("50dde9cd-647d-4472-b263-ecc52cccff31"), "kisisel-bakim", "TR", 0, "Kişisel Bakım", true, false, null, "", "kisisel bakim kisisel-bakim  kisisel bakim personal care", 930 },
                    { new Guid("3d168272-0b2d-44bf-8624-acb90fb804a6"), 0, new Guid("ccf2543c-aa2f-4654-892e-b8720a04bd83"), "okul-ucreti", "TR", 3, "Okul Ücreti", true, false, null, "", "okul ucreti okul-ucreti  egitim education", 820 },
                    { new Guid("46a7611b-4ebf-4800-93d2-767cabb614c2"), 2, new Guid("ad374367-3485-44e7-bc59-0daf55262b74"), "millenicom", "TR", 3, "Millenicom", true, false, null, "", "millenicom millenicom  internet ve telekom internet and telecom", 660 },
                    { new Guid("4c8fbfca-8eb3-4771-9935-168302bdb1d8"), 0, new Guid("7473e800-2311-4ac6-9631-18fab4784f33"), "harc", "TR", 0, "Harç", true, false, null, "", "harc harc  vergi ve resmi odemeler taxes and official payments", 890 },
                    { new Guid("4e8c3109-5509-44fa-b877-ed5452656790"), 2, new Guid("75330075-0a0e-4edb-ae9a-3b09df6c2515"), "spor-salonu", "TR", 3, "Spor Salonu", true, false, null, "", "spor salonu spor-salonu  spor ve fitness sport and fitness", 910 },
                    { new Guid("5b3cfe98-d787-4af2-b69e-7e81b7141d21"), 2, new Guid("ccf2543c-aa2f-4654-892e-b8720a04bd83"), "udemy", null, null, "Udemy", true, false, null, "", "udemy udemy  egitim education", 350 },
                    { new Guid("6313d5f8-bb28-4822-9375-fbb2d2c50a9d"), 2, new Guid("ccf2543c-aa2f-4654-892e-b8720a04bd83"), "duolingo", null, 3, "Duolingo", true, false, null, "", "duolingo duolingo  egitim education", 330 },
                    { new Guid("634a919e-3d90-44d2-97fe-2320c9dd17db"), 0, new Guid("e8b90166-a0f0-4cc4-8833-563025df402c"), "konut-sigortasi", "TR", 4, "Konut Sigortası", true, false, null, "", "konut sigortasi konut-sigortasi  sigorta insurance", 750 },
                    { new Guid("6b15a674-645b-4b8c-a134-a20d618973b4"), 2, new Guid("75330075-0a0e-4edb-ae9a-3b09df6c2515"), "strava", null, 3, "Strava", true, false, null, "", "strava strava  spor ve fitness sport and fitness", 370 },
                    { new Guid("745d52b4-6770-44b3-801e-8ba15b6c1828"), 0, new Guid("43133d20-4b41-4423-bdb1-6e4c9646956c"), "kredi-karti-odemesi", "TR", 3, "Kredi Kartı Ödemesi", true, false, null, "", "kredi karti odemesi kredi-karti-odemesi  banka, kredi ve finans banking and finance", 770 },
                    { new Guid("769082a7-ff15-4116-b6c3-b062aa6b3c6f"), 0, new Guid("18acd618-5e34-4f9c-821c-87a7921ad1ad"), "ev-bakimi", "TR", 0, "Ev Bakımı", true, false, null, "", "ev bakimi ev-bakimi  ev ve kira home and rent", 760 },
                    { new Guid("87a62664-af83-4687-b0bc-53f58635ed20"), 0, new Guid("a6f72f7d-c561-4496-865a-ee522dcae63f"), "muayene", "TR", 0, "Muayene", true, false, null, "", "muayene muayene  saglik health", 860 },
                    { new Guid("8d3a4463-22ab-475b-a426-39a5f0f35cfd"), 2, new Guid("ccf2543c-aa2f-4654-892e-b8720a04bd83"), "skillshare", null, 3, "Skillshare", true, false, null, "", "skillshare skillshare  egitim education", 360 },
                    { new Guid("958da99c-35c5-4461-b486-6893d0d5095f"), 0, new Guid("ccf2543c-aa2f-4654-892e-b8720a04bd83"), "ozel-ders", "TR", 0, "Özel Ders", true, false, null, "", "ozel ders ozel-ders  egitim education", 840 },
                    { new Guid("9ad446db-bea1-454a-adec-62e34c581de3"), 2, new Guid("75330075-0a0e-4edb-ae9a-3b09df6c2515"), "freeletics", null, 3, "Freeletics", true, false, null, "", "freeletics freeletics  spor ve fitness sport and fitness", 380 },
                    { new Guid("9bbb7d25-a481-4a1c-981b-2528ddc5055a"), 2, new Guid("ad374367-3485-44e7-bc59-0daf55262b74"), "superonline", "TR", 3, "Superonline", true, false, null, "", "superonline superonline  internet ve telekom internet and telecom", 650 },
                    { new Guid("9bd05373-5997-46e1-99c2-a7fa48f7a040"), 0, new Guid("50dde9cd-647d-4472-b263-ecc52cccff31"), "kuafor", "TR", 0, "Kuaför", true, false, null, "", "kuafor kuafor  kisisel bakim personal care", 920 },
                    { new Guid("a15f60ac-6bc0-4e6a-87e4-80eafb8587b5"), 0, new Guid("ccf2543c-aa2f-4654-892e-b8720a04bd83"), "kurs", "TR", 0, "Kurs", true, false, null, "", "kurs kurs  egitim education", 830 },
                    { new Guid("a64ad571-dfda-43d9-a866-4d5b99a85ce3"), 2, new Guid("ad374367-3485-44e7-bc59-0daf55262b74"), "turkcell", "TR", 3, "Turkcell", true, false, null, "", "turkcell turkcell  internet ve telekom internet and telecom", 610 },
                    { new Guid("a8593268-529d-43ed-9013-03df091ce03c"), 0, new Guid("a6f72f7d-c561-4496-865a-ee522dcae63f"), "ilac", "TR", 0, "İlaç", true, false, null, "", "ilac ilac  saglik health", 870 },
                    { new Guid("b78be435-cef9-4a6a-a694-1092aa6705a3"), 2, new Guid("bf324a2d-f3f4-4e5f-966d-6198e8dc4ac8"), "trendyol-plus", "TR", 3, "Trendyol Plus", true, false, null, "", "trendyol plus trendyol-plus  alisveris ve uyelikler shopping and memberships", 420 },
                    { new Guid("ba340f4a-0337-4fd0-8af0-63fd75dbe3b1"), 0, new Guid("43133d20-4b41-4423-bdb1-6e4c9646956c"), "ihtiyac-kredisi", "TR", 3, "İhtiyaç Kredisi", true, false, null, "", "ihtiyac kredisi ihtiyac-kredisi  banka, kredi ve finans banking and finance", 780 },
                    { new Guid("ba6716c0-6839-4b95-8940-8e30628cc0c2"), 2, new Guid("ad374367-3485-44e7-bc59-0daf55262b74"), "vodafone", "TR", 3, "Vodafone", true, false, null, "", "vodafone vodafone  internet ve telekom internet and telecom", 620 },
                    { new Guid("bfddbd4d-876f-4fe8-b493-f5231415917b"), 2, new Guid("ccf2543c-aa2f-4654-892e-b8720a04bd83"), "coursera", null, 3, "Coursera", true, false, null, "", "coursera coursera  egitim education", 340 },
                    { new Guid("c336a593-2c19-4ce8-97cd-27935fda4a0c"), 0, new Guid("7473e800-2311-4ac6-9631-18fab4784f33"), "vergi-odemesi", "TR", 0, "Vergi Ödemesi", true, false, null, "", "vergi odemesi vergi-odemesi  vergi ve resmi odemeler taxes and official payments", 880 },
                    { new Guid("ca6548cf-887c-462e-8d47-545663a4af60"), 2, new Guid("ad374367-3485-44e7-bc59-0daf55262b74"), "turknet", "TR", 3, "TurkNet", true, false, null, "", "turknet turknet  internet ve telekom internet and telecom", 640 },
                    { new Guid("d0f065b8-6224-4be9-b0dc-97b5887f597e"), 2, new Guid("ad374367-3485-44e7-bc59-0daf55262b74"), "turk-telekom", "TR", 3, "Türk Telekom", true, false, null, "", "turk telekom turk-telekom  internet ve telekom internet and telecom", 630 },
                    { new Guid("e581f50c-f3e4-4f20-b5da-4c3a83459208"), 0, new Guid("43133d20-4b41-4423-bdb1-6e4c9646956c"), "banka-ucreti", "TR", 0, "Banka Ücreti", true, false, null, "", "banka ucreti banka-ucreti  banka, kredi ve finans banking and finance", 810 },
                    { new Guid("f0073747-08ad-4c31-a5c4-a83c6a3ac620"), 0, new Guid("7473e800-2311-4ac6-9631-18fab4784f33"), "resmi-ucret", "TR", 0, "Resmî Ücret", true, false, null, "", "resmi ucret resmi-ucret  vergi ve resmi odemeler taxes and official payments", 900 },
                    { new Guid("fc4e2e14-586a-4c0f-b0ed-790ebc2afdde"), 2, new Guid("bf324a2d-f3f4-4e5f-966d-6198e8dc4ac8"), "hepsiburada-premium", "TR", 3, "Hepsiburada Premium", true, false, null, "", "hepsiburada premium hepsiburada-premium  alisveris ve uyelikler shopping and memberships", 410 }
                });

            migrationBuilder.InsertData(
                table: "CatalogCategories",
                columns: new[] { "Id", "ApplicableTo", "Code", "IconKey", "IsActive", "NameEn", "NameTr", "ParentId", "SearchText", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("2148fa07-f0db-4a25-b801-2c8bf34e44dc"), 0, "transport.diesel", "fuel", true, "Diesel", "Motorin", new Guid("caef747a-7a1f-4407-a714-40c446666d7d"), "motorin diesel transport.diesel", 270 },
                    { new Guid("a7dde3bb-7008-4359-968d-d62f12e6d819"), 0, "transport.petrol", "fuel", true, "Petrol", "Benzin", new Guid("caef747a-7a1f-4407-a714-40c446666d7d"), "benzin petrol transport.petrol", 260 },
                    { new Guid("c27ab3fb-2b5e-4286-af32-d85587e195af"), 0, "transport.lpg", "fuel", true, "LPG", "LPG", new Guid("caef747a-7a1f-4407-a714-40c446666d7d"), "lpg lpg transport.lpg", 280 }
                });

            migrationBuilder.InsertData(
                table: "CatalogServiceCategories",
                columns: new[] { "CategoryId", "ServiceId" },
                values: new object[,]
                {
                    { new Guid("bf324a2d-f3f4-4e5f-966d-6198e8dc4ac8"), new Guid("0c04850b-ac18-4077-8252-fac3c762658a") },
                    { new Guid("e64ecf69-c259-42c3-bc3b-4bc80dfa37b9"), new Guid("0c04850b-ac18-4077-8252-fac3c762658a") },
                    { new Guid("43133d20-4b41-4423-bdb1-6e4c9646956c"), new Guid("13795c93-773f-4ab5-8a51-fc6cc1be8415") },
                    { new Guid("a6f72f7d-c561-4496-865a-ee522dcae63f"), new Guid("1475e83f-59eb-4eb7-98e8-1c0adf218900") },
                    { new Guid("75330075-0a0e-4edb-ae9a-3b09df6c2515"), new Guid("285791f2-abc1-4d8c-b87e-461daf90ebab") },
                    { new Guid("472d4caa-0c65-467c-bee4-ae6d09546f71"), new Guid("3007de3f-25f3-42b3-ae6b-4e6cc46a8ebf") },
                    { new Guid("ad374367-3485-44e7-bc59-0daf55262b74"), new Guid("3007de3f-25f3-42b3-ae6b-4e6cc46a8ebf") },
                    { new Guid("f19d27ea-74cb-4b2e-b9fe-488c2dfdbb99"), new Guid("3007de3f-25f3-42b3-ae6b-4e6cc46a8ebf") },
                    { new Guid("50dde9cd-647d-4472-b263-ecc52cccff31"), new Guid("31be357e-60bd-4674-92ff-f1ca9d371ec0") },
                    { new Guid("ccf2543c-aa2f-4654-892e-b8720a04bd83"), new Guid("3d168272-0b2d-44bf-8624-acb90fb804a6") },
                    { new Guid("472d4caa-0c65-467c-bee4-ae6d09546f71"), new Guid("46a7611b-4ebf-4800-93d2-767cabb614c2") },
                    { new Guid("ad374367-3485-44e7-bc59-0daf55262b74"), new Guid("46a7611b-4ebf-4800-93d2-767cabb614c2") },
                    { new Guid("f19d27ea-74cb-4b2e-b9fe-488c2dfdbb99"), new Guid("46a7611b-4ebf-4800-93d2-767cabb614c2") },
                    { new Guid("7473e800-2311-4ac6-9631-18fab4784f33"), new Guid("4c8fbfca-8eb3-4771-9935-168302bdb1d8") },
                    { new Guid("75330075-0a0e-4edb-ae9a-3b09df6c2515"), new Guid("4e8c3109-5509-44fa-b877-ed5452656790") },
                    { new Guid("ccf2543c-aa2f-4654-892e-b8720a04bd83"), new Guid("5b3cfe98-d787-4af2-b69e-7e81b7141d21") },
                    { new Guid("ccf2543c-aa2f-4654-892e-b8720a04bd83"), new Guid("6313d5f8-bb28-4822-9375-fbb2d2c50a9d") },
                    { new Guid("e8b90166-a0f0-4cc4-8833-563025df402c"), new Guid("634a919e-3d90-44d2-97fe-2320c9dd17db") },
                    { new Guid("75330075-0a0e-4edb-ae9a-3b09df6c2515"), new Guid("6b15a674-645b-4b8c-a134-a20d618973b4") },
                    { new Guid("43133d20-4b41-4423-bdb1-6e4c9646956c"), new Guid("745d52b4-6770-44b3-801e-8ba15b6c1828") },
                    { new Guid("18acd618-5e34-4f9c-821c-87a7921ad1ad"), new Guid("769082a7-ff15-4116-b6c3-b062aa6b3c6f") },
                    { new Guid("a6f72f7d-c561-4496-865a-ee522dcae63f"), new Guid("87a62664-af83-4687-b0bc-53f58635ed20") },
                    { new Guid("ccf2543c-aa2f-4654-892e-b8720a04bd83"), new Guid("8d3a4463-22ab-475b-a426-39a5f0f35cfd") },
                    { new Guid("ccf2543c-aa2f-4654-892e-b8720a04bd83"), new Guid("958da99c-35c5-4461-b486-6893d0d5095f") },
                    { new Guid("75330075-0a0e-4edb-ae9a-3b09df6c2515"), new Guid("9ad446db-bea1-454a-adec-62e34c581de3") },
                    { new Guid("472d4caa-0c65-467c-bee4-ae6d09546f71"), new Guid("9bbb7d25-a481-4a1c-981b-2528ddc5055a") },
                    { new Guid("ad374367-3485-44e7-bc59-0daf55262b74"), new Guid("9bbb7d25-a481-4a1c-981b-2528ddc5055a") },
                    { new Guid("f19d27ea-74cb-4b2e-b9fe-488c2dfdbb99"), new Guid("9bbb7d25-a481-4a1c-981b-2528ddc5055a") },
                    { new Guid("50dde9cd-647d-4472-b263-ecc52cccff31"), new Guid("9bd05373-5997-46e1-99c2-a7fa48f7a040") },
                    { new Guid("ccf2543c-aa2f-4654-892e-b8720a04bd83"), new Guid("a15f60ac-6bc0-4e6a-87e4-80eafb8587b5") },
                    { new Guid("472d4caa-0c65-467c-bee4-ae6d09546f71"), new Guid("a64ad571-dfda-43d9-a866-4d5b99a85ce3") },
                    { new Guid("ad374367-3485-44e7-bc59-0daf55262b74"), new Guid("a64ad571-dfda-43d9-a866-4d5b99a85ce3") },
                    { new Guid("f19d27ea-74cb-4b2e-b9fe-488c2dfdbb99"), new Guid("a64ad571-dfda-43d9-a866-4d5b99a85ce3") },
                    { new Guid("a6f72f7d-c561-4496-865a-ee522dcae63f"), new Guid("a8593268-529d-43ed-9013-03df091ce03c") },
                    { new Guid("bf324a2d-f3f4-4e5f-966d-6198e8dc4ac8"), new Guid("b78be435-cef9-4a6a-a694-1092aa6705a3") },
                    { new Guid("43133d20-4b41-4423-bdb1-6e4c9646956c"), new Guid("ba340f4a-0337-4fd0-8af0-63fd75dbe3b1") },
                    { new Guid("472d4caa-0c65-467c-bee4-ae6d09546f71"), new Guid("ba6716c0-6839-4b95-8940-8e30628cc0c2") },
                    { new Guid("ad374367-3485-44e7-bc59-0daf55262b74"), new Guid("ba6716c0-6839-4b95-8940-8e30628cc0c2") },
                    { new Guid("f19d27ea-74cb-4b2e-b9fe-488c2dfdbb99"), new Guid("ba6716c0-6839-4b95-8940-8e30628cc0c2") },
                    { new Guid("ccf2543c-aa2f-4654-892e-b8720a04bd83"), new Guid("bfddbd4d-876f-4fe8-b493-f5231415917b") },
                    { new Guid("7473e800-2311-4ac6-9631-18fab4784f33"), new Guid("c336a593-2c19-4ce8-97cd-27935fda4a0c") },
                    { new Guid("472d4caa-0c65-467c-bee4-ae6d09546f71"), new Guid("ca6548cf-887c-462e-8d47-545663a4af60") },
                    { new Guid("ad374367-3485-44e7-bc59-0daf55262b74"), new Guid("ca6548cf-887c-462e-8d47-545663a4af60") },
                    { new Guid("f19d27ea-74cb-4b2e-b9fe-488c2dfdbb99"), new Guid("ca6548cf-887c-462e-8d47-545663a4af60") },
                    { new Guid("472d4caa-0c65-467c-bee4-ae6d09546f71"), new Guid("d0f065b8-6224-4be9-b0dc-97b5887f597e") },
                    { new Guid("ad374367-3485-44e7-bc59-0daf55262b74"), new Guid("d0f065b8-6224-4be9-b0dc-97b5887f597e") },
                    { new Guid("f19d27ea-74cb-4b2e-b9fe-488c2dfdbb99"), new Guid("d0f065b8-6224-4be9-b0dc-97b5887f597e") },
                    { new Guid("43133d20-4b41-4423-bdb1-6e4c9646956c"), new Guid("e581f50c-f3e4-4f20-b5da-4c3a83459208") },
                    { new Guid("7473e800-2311-4ac6-9631-18fab4784f33"), new Guid("f0073747-08ad-4c31-a5c4-a83c6a3ac620") },
                    { new Guid("bf324a2d-f3f4-4e5f-966d-6198e8dc4ac8"), new Guid("fc4e2e14-586a-4c0f-b0ed-790ebc2afdde") }
                });

            migrationBuilder.InsertData(
                table: "CatalogServices",
                columns: new[] { "Id", "ApplicableTo", "CategoryId", "Code", "CountryCode", "DefaultRecurrenceType", "DisplayName", "IsActive", "IsPopular", "LogoKey", "SearchKeywords", "SearchText", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("0121de3c-8fc3-4e08-b551-0570e4ca47b1"), 0, new Guid("56e5cd4f-10ee-4e7a-8761-af1cb8183daa"), "ozel-tamirci", "TR", 0, "Özel Tamirci", true, false, null, "", "ozel tamirci ozel-tamirci  arac tamiri vehicle repair", 550 },
                    { new Guid("1b0da2d1-7da6-4d2e-8d78-66238422739b"), 0, new Guid("56e5cd4f-10ee-4e7a-8761-af1cb8183daa"), "genel-servis", "TR", 0, "Genel Servis", true, false, null, "", "genel servis genel-servis  arac tamiri vehicle repair", 540 },
                    { new Guid("1b5b1522-80fc-45d0-bfb6-a15cacbc763f"), 2, new Guid("a5689548-1d21-4b91-a509-5bf60ab54c80"), "notion", null, 3, "Notion", true, false, null, "", "notion notion  yazilim ve uretkenlik software and productivity", 270 },
                    { new Guid("2215f150-0bc2-43e2-98c5-21794891aa62"), 0, new Guid("472d4caa-0c65-467c-bee4-ae6d09546f71"), "telefon-faturasi", "TR", 3, "Telefon Faturası", true, false, null, "", "telefon faturasi telefon-faturasi  telefon faturasi phone bill", 720 },
                    { new Guid("26f05512-41a8-4e79-92c6-9cf4308f393c"), 0, new Guid("191168e3-9df2-4fdd-81d8-d9d052ae0274"), "elektrik-faturasi", "TR", 3, "Elektrik Faturası", true, false, null, "", "elektrik faturasi elektrik-faturasi  elektrik faturasi electricity bill", 680 },
                    { new Guid("2a21123d-d069-4c6c-91b8-2b60248e5997"), 0, new Guid("f7fa7a0f-5b7a-454a-8c43-3c75fd4999ad"), "oto-yikama", "TR", 0, "Oto Yıkama", true, false, null, "", "oto yikama oto-yikama  arac yikama car wash", 600 },
                    { new Guid("2d78f540-8e07-495c-b5ae-7bb71b480daa"), 0, new Guid("7d5de2a8-87e7-498c-926d-a1fe87b86ba0"), "esarj", "TR", 0, "Eşarj", true, false, null, "", "esarj esarj  elektrikli arac sarji ev charging", 510 },
                    { new Guid("38e10d5a-73d4-4824-82bd-e7cc8f0daa1e"), 2, new Guid("ee6e4920-cb3d-4005-8651-95c5a3042db5"), "onedrive", null, 3, "OneDrive", true, false, null, "", "onedrive onedrive  bulut ve depolama cloud and storage", 320 },
                    { new Guid("3c0d3e89-8090-497b-8765-0fff6e0e0960"), 0, new Guid("5f1033c8-fa71-4005-80a6-880482c931ab"), "aidat", "TR", 3, "Aidat", true, false, null, "", "aidat aidat  aidat maintenance dues", 740 },
                    { new Guid("3df58be1-df63-4df2-826d-537879bfb328"), 2, new Guid("c3f7efbd-2591-4237-9f4f-ff77648ba333"), "nintendo-switch-online", null, 3, "Nintendo Switch Online", true, false, null, "", "nintendo switch online nintendo-switch-online  oyun ve eglence games and entertainment", 160 },
                    { new Guid("3eb0c555-3aff-4e7f-8f8d-f728bca22371"), 2, new Guid("9c945fb0-8444-4ad7-99fc-e15b5e1a5df0"), "midjourney", null, 3, "Midjourney", true, false, null, "", "midjourney midjourney  yapay zeka artificial intelligence", 230 },
                    { new Guid("4ac8b84c-2f79-4cf0-a3ed-48e818507b06"), 0, new Guid("42edd8a7-533d-4b79-ac0f-d47e62dc6ab1"), "kira", "TR", 3, "Kira", true, false, null, "", "kira kira  kira rent", 730 },
                    { new Guid("4be1f486-1b59-46a8-b836-928045dd782c"), 0, new Guid("56e5cd4f-10ee-4e7a-8761-af1cb8183daa"), "otopratik", "TR", 0, "Otopratik", true, false, null, "", "otopratik otopratik  arac tamiri vehicle repair", 530 },
                    { new Guid("4e0a6b80-9847-45fa-9000-643e7ecacb51"), 2, new Guid("a5689548-1d21-4b91-a509-5bf60ab54c80"), "canva", null, 3, "Canva", true, false, null, "", "canva canva  yazilim ve uretkenlik software and productivity", 260 },
                    { new Guid("4f06f017-1530-4506-a976-b2aa4e2200a4"), 2, new Guid("e64ecf69-c259-42c3-bc3b-4bc80dfa37b9"), "amazon-prime-video", null, 3, "Amazon Prime Video", true, false, null, "", "amazon prime video amazon-prime-video  film ve dizi film and tv", 20 },
                    { new Guid("51859b39-47dc-4834-83c8-228722de6e96"), 0, new Guid("f19d27ea-74cb-4b2e-b9fe-488c2dfdbb99"), "internet-faturasi", "TR", 3, "İnternet Faturası", true, false, null, "", "internet faturasi internet-faturasi  internet faturasi internet bill", 710 },
                    { new Guid("5b13814b-47ba-49b5-ad95-1102f70a41d5"), 0, new Guid("b88c10bf-5502-4fdc-8ddf-f9b3640e8b2c"), "tuvturk", "TR", null, "TÜVTÜRK", true, false, null, "", "tuvturk tuvturk  arac muayenesi vehicle inspection", 560 },
                    { new Guid("610f081c-d754-4dd2-9f3c-3bc93d820b30"), 0, new Guid("56e5cd4f-10ee-4e7a-8761-af1cb8183daa"), "bosch-car-service", "TR", 0, "Bosch Car Service", true, false, null, "", "bosch car service bosch-car-service  arac tamiri vehicle repair", 520 },
                    { new Guid("6762f6eb-06fd-49ea-a308-f25159b0fef8"), 2, new Guid("2dc8c7ab-920c-4b01-b191-9605039723fb"), "youtube-music", null, 3, "YouTube Music", true, false, null, "", "youtube music youtube-music  muzik ve podcast music and podcasts", 100 },
                    { new Guid("6a91af7b-a961-4c5d-a78a-aaa94c0d46b6"), 0, new Guid("2d4d078c-a5fb-404e-99fa-ce63e6bfec92"), "lastikci", "TR", 0, "Lastikçi", true, false, null, "", "lastikci lastikci  lastik ve jant tyres and wheels", 580 },
                    { new Guid("6eddd662-95db-4d5d-bfe3-eaf639919693"), 2, new Guid("9c945fb0-8444-4ad7-99fc-e15b5e1a5df0"), "perplexity", null, 3, "Perplexity", true, false, null, "", "perplexity perplexity  yapay zeka artificial intelligence", 220 },
                    { new Guid("7c36c1a0-cd2d-44be-a40e-51e1b0b23443"), 2, new Guid("c3f7efbd-2591-4237-9f4f-ff77648ba333"), "geforce-now", null, 3, "GeForce NOW", true, false, null, "", "geforce now geforce-now  oyun ve eglence games and entertainment", 180 },
                    { new Guid("8f505405-b286-4a61-a2fa-18428bed8134"), 2, new Guid("e64ecf69-c259-42c3-bc3b-4bc80dfa37b9"), "netflix", null, 3, "Netflix", true, true, null, "", "netflix netflix  film ve dizi film and tv", 0 },
                    { new Guid("9014b93e-7b76-46a2-9f1b-c101dfcbb3e1"), 2, new Guid("a5689548-1d21-4b91-a509-5bf60ab54c80"), "adobe-creative-cloud", null, 3, "Adobe Creative Cloud", true, false, null, "", "adobe creative cloud adobe-creative-cloud  yazilim ve uretkenlik software and productivity", 250 },
                    { new Guid("91a26ebb-34f6-42cd-a130-39d2f764b0bc"), 2, new Guid("e64ecf69-c259-42c3-bc3b-4bc80dfa37b9"), "tv-plus", null, 3, "TV+", true, false, null, "", "tv+ tv-plus  film ve dizi film and tv", 60 },
                    { new Guid("93c31242-b92b-49d7-b5b3-2908d3f37c60"), 2, new Guid("ee6e4920-cb3d-4005-8651-95c5a3042db5"), "google-one", null, 3, "Google One", true, false, null, "", "google one google-one  bulut ve depolama cloud and storage", 290 },
                    { new Guid("96e55ea8-00d8-46eb-936e-1b5ce5a6f121"), 2, new Guid("c3f7efbd-2591-4237-9f4f-ff77648ba333"), "xbox-game-pass", null, 3, "Xbox Game Pass", true, false, null, "", "xbox game pass xbox-game-pass  oyun ve eglence games and entertainment", 140 },
                    { new Guid("97776faf-91f4-4836-b95c-8990032b75b8"), 2, new Guid("a5689548-1d21-4b91-a509-5bf60ab54c80"), "github-copilot", null, 3, "GitHub Copilot", true, false, null, "", "github copilot github-copilot  yazilim ve uretkenlik software and productivity", 280 },
                    { new Guid("9bed618e-93bf-4663-bc64-562d90e4bec1"), 0, new Guid("7d5de2a8-87e7-498c-926d-a1fe87b86ba0"), "trugo", "TR", 0, "Trugo", true, false, null, "", "trugo trugo  elektrikli arac sarji ev charging", 490 },
                    { new Guid("9e72cc7d-e299-497f-8d87-a9a745a4adc9"), 2, new Guid("2dc8c7ab-920c-4b01-b191-9605039723fb"), "tidal", null, 3, "TIDAL", true, false, null, "", "tidal tidal  muzik ve podcast music and podcasts", 130 },
                    { new Guid("9f66fecb-5746-4edf-98d8-b6f85ef61307"), 2, new Guid("ee6e4920-cb3d-4005-8651-95c5a3042db5"), "icloud-plus", null, 3, "iCloud+", true, false, null, "Apple TV Apple TV+ Apple Arcade paket Türkiye", "icloud+ icloud-plus apple tv apple tv+ apple arcade paket turkiye bulut ve depolama cloud and storage", 300 },
                    { new Guid("a021683b-b248-4faa-aebd-2bfe3f82e49e"), 0, new Guid("bde9972a-b980-4e27-af68-5f4ee8d76c2e"), "trafik-sigortasi", "TR", 4, "Trafik Sigortası", true, false, null, "", "trafik sigortasi trafik-sigortasi  trafik sigortasi traffic insurance", 940 },
                    { new Guid("a1524a4b-c16a-46db-99b0-95c7325d9df5"), 2, new Guid("9c945fb0-8444-4ad7-99fc-e15b5e1a5df0"), "google-ai-pro", null, 3, "Google AI Pro", true, false, null, "Gemini Google One AI Premium", "google ai pro google-ai-pro gemini google one ai premium yapay zeka artificial intelligence", 210 },
                    { new Guid("b54c516f-b8cd-4723-ae18-84c713eb1c13"), 0, new Guid("90a47ca5-93a9-4b97-8d42-377193be7377"), "dogalgaz-faturasi", "TR", 3, "Doğalgaz Faturası", true, false, null, "", "dogalgaz faturasi dogalgaz-faturasi  dogalgaz faturasi gas bill", 700 },
                    { new Guid("b998f8ab-43c8-4ec5-99ca-039c676422bf"), 0, new Guid("1dd72c50-47fd-4b26-ae0a-bf60027dc8ee"), "hgs", "TR", 0, "HGS", true, false, null, "", "hgs hgs  hgs hgs", 570 },
                    { new Guid("bd9373d2-2179-47fe-8eb8-cbd7a9907aa8"), 2, new Guid("c3f7efbd-2591-4237-9f4f-ff77648ba333"), "ea-play", null, 3, "EA Play", true, false, null, "", "ea play ea-play  oyun ve eglence games and entertainment", 170 },
                    { new Guid("c080f094-a58d-4898-a90a-a0a4e4f16c66"), 0, new Guid("0cf398fa-dcdc-466a-ac73-10346708ebf2"), "su-faturasi", "TR", 3, "Su Faturası", true, false, null, "", "su faturasi su-faturasi  su faturasi water bill", 690 },
                    { new Guid("c2203979-99db-4ccc-84e7-6587bef4ccad"), 2, new Guid("e64ecf69-c259-42c3-bc3b-4bc80dfa37b9"), "mubi", null, 3, "MUBI", true, false, null, "", "mubi mubi  film ve dizi film and tv", 70 },
                    { new Guid("c41ad125-cd33-4e04-bcc9-8d7d28d82ccb"), 2, new Guid("9c945fb0-8444-4ad7-99fc-e15b5e1a5df0"), "chatgpt", null, 3, "ChatGPT", true, true, null, "", "chatgpt chatgpt  yapay zeka artificial intelligence", 190 },
                    { new Guid("c69eb12b-14f7-4b8f-9b88-acae04446d69"), 2, new Guid("2dc8c7ab-920c-4b01-b191-9605039723fb"), "apple-music", null, 3, "Apple Music", true, false, null, "", "apple music apple-music  muzik ve podcast music and podcasts", 110 },
                    { new Guid("c8f4f69e-eb68-4782-a655-616892afc86c"), 2, new Guid("e64ecf69-c259-42c3-bc3b-4bc80dfa37b9"), "apple-tv", null, null, "Apple TV", false, false, null, "Apple TV+ Türkiye iCloud+ kapsamında ayrı abonelik değil", "apple tv apple-tv apple tv+ turkiye icloud+ kapsaminda ayri abonelik degil film ve dizi film and tv", 80 },
                    { new Guid("ca72de55-0b66-4f96-b0f8-db8993f35cb5"), 0, new Guid("db3088cf-333f-4107-8f73-095e2e53920f"), "otopark", "TR", 0, "Otopark", true, false, null, "", "otopark otopark  otopark parking", 590 },
                    { new Guid("cafe8354-b707-4c0d-9756-905887329a60"), 0, new Guid("7d5de2a8-87e7-498c-926d-a1fe87b86ba0"), "zes", "TR", 0, "ZES", true, false, null, "", "zes zes  elektrikli arac sarji ev charging", 500 },
                    { new Guid("ce5d291e-a62e-4165-8cf0-d4f8fadbc817"), 2, new Guid("c3f7efbd-2591-4237-9f4f-ff77648ba333"), "playstation-plus", null, 3, "PlayStation Plus", true, false, null, "", "playstation plus playstation-plus  oyun ve eglence games and entertainment", 150 },
                    { new Guid("d1b623bb-9669-4121-bc2c-c6012adb70d3"), 0, new Guid("1af3cc7d-80f0-426f-9603-17b944b6acad"), "mtv", "TR", 0, "MTV", true, false, null, "", "mtv mtv  motorlu tasitlar vergisi motor vehicle tax", 960 },
                    { new Guid("d887714e-a518-46c0-8ae7-9633f25d031f"), 2, new Guid("ee6e4920-cb3d-4005-8651-95c5a3042db5"), "dropbox", null, 3, "Dropbox", true, false, null, "", "dropbox dropbox  bulut ve depolama cloud and storage", 310 },
                    { new Guid("d9f4d7c8-c234-4cd2-815b-6100896ba386"), 2, new Guid("a5689548-1d21-4b91-a509-5bf60ab54c80"), "microsoft-365", null, 3, "Microsoft 365", true, false, null, "", "microsoft 365 microsoft-365  yazilim ve uretkenlik software and productivity", 240 },
                    { new Guid("dcf84e50-34d8-4856-baf0-6fa5559d6504"), 0, new Guid("6c29d470-7631-481b-a975-feefcb69688e"), "kasko", "TR", 4, "Kasko", true, false, null, "", "kasko kasko  kasko comprehensive insurance", 950 },
                    { new Guid("e589b4bb-584b-42a7-a87a-a83bc143219f"), 2, new Guid("e64ecf69-c259-42c3-bc3b-4bc80dfa37b9"), "hbo-max", null, 3, "HBO Max", true, false, null, "Max BluTV", "hbo max hbo-max max blutv film ve dizi film and tv", 30 },
                    { new Guid("e738bb32-daf9-4551-9f72-f37d2527cb11"), 2, new Guid("9c945fb0-8444-4ad7-99fc-e15b5e1a5df0"), "claude", null, 3, "Claude", true, false, null, "", "claude claude  yapay zeka artificial intelligence", 200 },
                    { new Guid("e7424d3a-e081-43e0-aec2-d80fb31d60ea"), 2, new Guid("2dc8c7ab-920c-4b01-b191-9605039723fb"), "deezer", null, 3, "Deezer", true, false, null, "", "deezer deezer  muzik ve podcast music and podcasts", 120 },
                    { new Guid("e8411f0a-3bae-41cd-ac0c-e6765bd0c4b1"), 2, new Guid("e64ecf69-c259-42c3-bc3b-4bc80dfa37b9"), "exxen", null, 3, "Exxen", true, false, null, "", "exxen exxen  film ve dizi film and tv", 40 },
                    { new Guid("edb4c5e8-34aa-45b7-ae88-a59ddd6dc5e9"), 0, new Guid("a68e8dc7-7af4-4271-a93b-97513311f280"), "tasit-kredisi", "TR", 3, "Taşıt Kredisi", true, false, null, "", "tasit kredisi tasit-kredisi  arac kredisi vehicle loan", 800 },
                    { new Guid("edbade78-a624-4bfc-af03-b433cf3667bc"), 2, new Guid("e64ecf69-c259-42c3-bc3b-4bc80dfa37b9"), "disney-plus", null, 3, "Disney+", true, true, null, "", "disney+ disney-plus  film ve dizi film and tv", 10 },
                    { new Guid("eefcbdc3-457e-48c0-b1c7-e39b1a9579de"), 2, new Guid("e64ecf69-c259-42c3-bc3b-4bc80dfa37b9"), "tod", null, 3, "TOD", true, false, null, "", "tod tod  film ve dizi film and tv", 50 },
                    { new Guid("f3ee3df2-22aa-4492-8097-cb376d5db11b"), 2, new Guid("2dc8c7ab-920c-4b01-b191-9605039723fb"), "spotify", null, 3, "Spotify", true, true, null, "", "spotify spotify  muzik ve podcast music and podcasts", 90 }
                });

            migrationBuilder.InsertData(
                table: "CatalogServiceCategories",
                columns: new[] { "CategoryId", "ServiceId" },
                values: new object[,]
                {
                    { new Guid("235d9b7c-07ae-4b74-b1ec-e43e29e66f08"), new Guid("0121de3c-8fc3-4e08-b551-0570e4ca47b1") },
                    { new Guid("56e5cd4f-10ee-4e7a-8761-af1cb8183daa"), new Guid("0121de3c-8fc3-4e08-b551-0570e4ca47b1") },
                    { new Guid("235d9b7c-07ae-4b74-b1ec-e43e29e66f08"), new Guid("1b0da2d1-7da6-4d2e-8d78-66238422739b") },
                    { new Guid("56e5cd4f-10ee-4e7a-8761-af1cb8183daa"), new Guid("1b0da2d1-7da6-4d2e-8d78-66238422739b") },
                    { new Guid("a5689548-1d21-4b91-a509-5bf60ab54c80"), new Guid("1b5b1522-80fc-45d0-bfb6-a15cacbc763f") },
                    { new Guid("472d4caa-0c65-467c-bee4-ae6d09546f71"), new Guid("2215f150-0bc2-43e2-98c5-21794891aa62") },
                    { new Guid("191168e3-9df2-4fdd-81d8-d9d052ae0274"), new Guid("26f05512-41a8-4e79-92c6-9cf4308f393c") },
                    { new Guid("f7fa7a0f-5b7a-454a-8c43-3c75fd4999ad"), new Guid("2a21123d-d069-4c6c-91b8-2b60248e5997") },
                    { new Guid("7d5de2a8-87e7-498c-926d-a1fe87b86ba0"), new Guid("2d78f540-8e07-495c-b5ae-7bb71b480daa") },
                    { new Guid("ee6e4920-cb3d-4005-8651-95c5a3042db5"), new Guid("38e10d5a-73d4-4824-82bd-e7cc8f0daa1e") },
                    { new Guid("5f1033c8-fa71-4005-80a6-880482c931ab"), new Guid("3c0d3e89-8090-497b-8765-0fff6e0e0960") },
                    { new Guid("c3f7efbd-2591-4237-9f4f-ff77648ba333"), new Guid("3df58be1-df63-4df2-826d-537879bfb328") },
                    { new Guid("9c945fb0-8444-4ad7-99fc-e15b5e1a5df0"), new Guid("3eb0c555-3aff-4e7f-8f8d-f728bca22371") },
                    { new Guid("42edd8a7-533d-4b79-ac0f-d47e62dc6ab1"), new Guid("4ac8b84c-2f79-4cf0-a3ed-48e818507b06") },
                    { new Guid("235d9b7c-07ae-4b74-b1ec-e43e29e66f08"), new Guid("4be1f486-1b59-46a8-b836-928045dd782c") },
                    { new Guid("56e5cd4f-10ee-4e7a-8761-af1cb8183daa"), new Guid("4be1f486-1b59-46a8-b836-928045dd782c") },
                    { new Guid("a5689548-1d21-4b91-a509-5bf60ab54c80"), new Guid("4e0a6b80-9847-45fa-9000-643e7ecacb51") },
                    { new Guid("e64ecf69-c259-42c3-bc3b-4bc80dfa37b9"), new Guid("4f06f017-1530-4506-a976-b2aa4e2200a4") },
                    { new Guid("f19d27ea-74cb-4b2e-b9fe-488c2dfdbb99"), new Guid("51859b39-47dc-4834-83c8-228722de6e96") },
                    { new Guid("8b92bbb1-183d-40fe-8512-de7ae23493d8"), new Guid("5b13814b-47ba-49b5-ad95-1102f70a41d5") },
                    { new Guid("b88c10bf-5502-4fdc-8ddf-f9b3640e8b2c"), new Guid("5b13814b-47ba-49b5-ad95-1102f70a41d5") },
                    { new Guid("235d9b7c-07ae-4b74-b1ec-e43e29e66f08"), new Guid("610f081c-d754-4dd2-9f3c-3bc93d820b30") },
                    { new Guid("56e5cd4f-10ee-4e7a-8761-af1cb8183daa"), new Guid("610f081c-d754-4dd2-9f3c-3bc93d820b30") },
                    { new Guid("2dc8c7ab-920c-4b01-b191-9605039723fb"), new Guid("6762f6eb-06fd-49ea-a308-f25159b0fef8") },
                    { new Guid("2d4d078c-a5fb-404e-99fa-ce63e6bfec92"), new Guid("6a91af7b-a961-4c5d-a78a-aaa94c0d46b6") },
                    { new Guid("9c945fb0-8444-4ad7-99fc-e15b5e1a5df0"), new Guid("6eddd662-95db-4d5d-bfe3-eaf639919693") },
                    { new Guid("c3f7efbd-2591-4237-9f4f-ff77648ba333"), new Guid("7c36c1a0-cd2d-44be-a40e-51e1b0b23443") },
                    { new Guid("e64ecf69-c259-42c3-bc3b-4bc80dfa37b9"), new Guid("8f505405-b286-4a61-a2fa-18428bed8134") },
                    { new Guid("a5689548-1d21-4b91-a509-5bf60ab54c80"), new Guid("9014b93e-7b76-46a2-9f1b-c101dfcbb3e1") },
                    { new Guid("e64ecf69-c259-42c3-bc3b-4bc80dfa37b9"), new Guid("91a26ebb-34f6-42cd-a130-39d2f764b0bc") },
                    { new Guid("ee6e4920-cb3d-4005-8651-95c5a3042db5"), new Guid("93c31242-b92b-49d7-b5b3-2908d3f37c60") },
                    { new Guid("c3f7efbd-2591-4237-9f4f-ff77648ba333"), new Guid("96e55ea8-00d8-46eb-936e-1b5ce5a6f121") },
                    { new Guid("a5689548-1d21-4b91-a509-5bf60ab54c80"), new Guid("97776faf-91f4-4836-b95c-8990032b75b8") },
                    { new Guid("7d5de2a8-87e7-498c-926d-a1fe87b86ba0"), new Guid("9bed618e-93bf-4663-bc64-562d90e4bec1") },
                    { new Guid("2dc8c7ab-920c-4b01-b191-9605039723fb"), new Guid("9e72cc7d-e299-497f-8d87-a9a745a4adc9") },
                    { new Guid("c3f7efbd-2591-4237-9f4f-ff77648ba333"), new Guid("9f66fecb-5746-4edf-98d8-b6f85ef61307") },
                    { new Guid("e64ecf69-c259-42c3-bc3b-4bc80dfa37b9"), new Guid("9f66fecb-5746-4edf-98d8-b6f85ef61307") },
                    { new Guid("ee6e4920-cb3d-4005-8651-95c5a3042db5"), new Guid("9f66fecb-5746-4edf-98d8-b6f85ef61307") },
                    { new Guid("bde9972a-b980-4e27-af68-5f4ee8d76c2e"), new Guid("a021683b-b248-4faa-aebd-2bfe3f82e49e") },
                    { new Guid("9c945fb0-8444-4ad7-99fc-e15b5e1a5df0"), new Guid("a1524a4b-c16a-46db-99b0-95c7325d9df5") },
                    { new Guid("90a47ca5-93a9-4b97-8d42-377193be7377"), new Guid("b54c516f-b8cd-4723-ae18-84c713eb1c13") },
                    { new Guid("0b05b51e-1b39-4252-936c-ad141dc2eca7"), new Guid("b998f8ab-43c8-4ec5-99ca-039c676422bf") },
                    { new Guid("1dd72c50-47fd-4b26-ae0a-bf60027dc8ee"), new Guid("b998f8ab-43c8-4ec5-99ca-039c676422bf") },
                    { new Guid("c3f7efbd-2591-4237-9f4f-ff77648ba333"), new Guid("bd9373d2-2179-47fe-8eb8-cbd7a9907aa8") },
                    { new Guid("0cf398fa-dcdc-466a-ac73-10346708ebf2"), new Guid("c080f094-a58d-4898-a90a-a0a4e4f16c66") },
                    { new Guid("e64ecf69-c259-42c3-bc3b-4bc80dfa37b9"), new Guid("c2203979-99db-4ccc-84e7-6587bef4ccad") },
                    { new Guid("9c945fb0-8444-4ad7-99fc-e15b5e1a5df0"), new Guid("c41ad125-cd33-4e04-bcc9-8d7d28d82ccb") },
                    { new Guid("2dc8c7ab-920c-4b01-b191-9605039723fb"), new Guid("c69eb12b-14f7-4b8f-9b88-acae04446d69") },
                    { new Guid("e64ecf69-c259-42c3-bc3b-4bc80dfa37b9"), new Guid("c8f4f69e-eb68-4782-a655-616892afc86c") },
                    { new Guid("db3088cf-333f-4107-8f73-095e2e53920f"), new Guid("ca72de55-0b66-4f96-b0f8-db8993f35cb5") },
                    { new Guid("7d5de2a8-87e7-498c-926d-a1fe87b86ba0"), new Guid("cafe8354-b707-4c0d-9756-905887329a60") },
                    { new Guid("c3f7efbd-2591-4237-9f4f-ff77648ba333"), new Guid("ce5d291e-a62e-4165-8cf0-d4f8fadbc817") },
                    { new Guid("1af3cc7d-80f0-426f-9603-17b944b6acad"), new Guid("d1b623bb-9669-4121-bc2c-c6012adb70d3") },
                    { new Guid("ee6e4920-cb3d-4005-8651-95c5a3042db5"), new Guid("d887714e-a518-46c0-8ae7-9633f25d031f") },
                    { new Guid("a5689548-1d21-4b91-a509-5bf60ab54c80"), new Guid("d9f4d7c8-c234-4cd2-815b-6100896ba386") },
                    { new Guid("6c29d470-7631-481b-a975-feefcb69688e"), new Guid("dcf84e50-34d8-4856-baf0-6fa5559d6504") },
                    { new Guid("e64ecf69-c259-42c3-bc3b-4bc80dfa37b9"), new Guid("e589b4bb-584b-42a7-a87a-a83bc143219f") },
                    { new Guid("9c945fb0-8444-4ad7-99fc-e15b5e1a5df0"), new Guid("e738bb32-daf9-4551-9f72-f37d2527cb11") },
                    { new Guid("2dc8c7ab-920c-4b01-b191-9605039723fb"), new Guid("e7424d3a-e081-43e0-aec2-d80fb31d60ea") },
                    { new Guid("e64ecf69-c259-42c3-bc3b-4bc80dfa37b9"), new Guid("e8411f0a-3bae-41cd-ac0c-e6765bd0c4b1") },
                    { new Guid("a68e8dc7-7af4-4271-a93b-97513311f280"), new Guid("edb4c5e8-34aa-45b7-ae88-a59ddd6dc5e9") },
                    { new Guid("e64ecf69-c259-42c3-bc3b-4bc80dfa37b9"), new Guid("edbade78-a624-4bfc-af03-b433cf3667bc") },
                    { new Guid("e64ecf69-c259-42c3-bc3b-4bc80dfa37b9"), new Guid("eefcbdc3-457e-48c0-b1c7-e39b1a9579de") },
                    { new Guid("2dc8c7ab-920c-4b01-b191-9605039723fb"), new Guid("f3ee3df2-22aa-4492-8097-cb376d5db11b") }
                });

            migrationBuilder.InsertData(
                table: "CatalogServices",
                columns: new[] { "Id", "ApplicableTo", "CategoryId", "Code", "CountryCode", "DefaultRecurrenceType", "DisplayName", "IsActive", "IsPopular", "LogoKey", "SearchKeywords", "SearchText", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("37e55a59-6067-4afd-98ee-d9f1585fc3c9"), 0, new Guid("a7dde3bb-7008-4359-968d-d62f12e6d819"), "opet", "TR", 0, "Opet", true, false, null, "benzin motorin lpg yakıt", "opet opet benzin motorin lpg yakit benzin petrol", 440 },
                    { new Guid("74ad3446-0890-4fae-bea1-c24681b97100"), 0, new Guid("a7dde3bb-7008-4359-968d-d62f12e6d819"), "aytemiz", "TR", 0, "Aytemiz", true, false, null, "benzin motorin lpg yakıt", "aytemiz aytemiz benzin motorin lpg yakit benzin petrol", 480 },
                    { new Guid("7f36f1ed-8972-4bbf-8085-6f789fac0378"), 0, new Guid("a7dde3bb-7008-4359-968d-d62f12e6d819"), "bp", "TR", 0, "bp", false, false, null, "benzin motorin lpg yakıt", "bp bp benzin motorin lpg yakit benzin petrol", 460 },
                    { new Guid("b3aa473e-0d76-451c-9a14-b32a475714eb"), 0, new Guid("a7dde3bb-7008-4359-968d-d62f12e6d819"), "shell", "TR", 0, "Shell", true, false, null, "benzin motorin lpg yakıt", "shell shell benzin motorin lpg yakit benzin petrol", 430 },
                    { new Guid("d758dc03-6e00-4319-9293-131bf7888a32"), 0, new Guid("a7dde3bb-7008-4359-968d-d62f12e6d819"), "totalenergies", "TR", 0, "TotalEnergies", true, false, null, "benzin motorin lpg yakıt", "totalenergies totalenergies benzin motorin lpg yakit benzin petrol", 470 },
                    { new Guid("e52c79dd-5738-4838-bdbd-6e332092adb2"), 0, new Guid("a7dde3bb-7008-4359-968d-d62f12e6d819"), "petrol-ofisi", "TR", 0, "Petrol Ofisi", true, false, null, "benzin motorin lpg yakıt", "petrol ofisi petrol-ofisi benzin motorin lpg yakit benzin petrol", 450 }
                });

            migrationBuilder.InsertData(
                table: "CatalogServiceCategories",
                columns: new[] { "CategoryId", "ServiceId" },
                values: new object[,]
                {
                    { new Guid("2148fa07-f0db-4a25-b801-2c8bf34e44dc"), new Guid("37e55a59-6067-4afd-98ee-d9f1585fc3c9") },
                    { new Guid("a7dde3bb-7008-4359-968d-d62f12e6d819"), new Guid("37e55a59-6067-4afd-98ee-d9f1585fc3c9") },
                    { new Guid("c27ab3fb-2b5e-4286-af32-d85587e195af"), new Guid("37e55a59-6067-4afd-98ee-d9f1585fc3c9") },
                    { new Guid("2148fa07-f0db-4a25-b801-2c8bf34e44dc"), new Guid("74ad3446-0890-4fae-bea1-c24681b97100") },
                    { new Guid("a7dde3bb-7008-4359-968d-d62f12e6d819"), new Guid("74ad3446-0890-4fae-bea1-c24681b97100") },
                    { new Guid("c27ab3fb-2b5e-4286-af32-d85587e195af"), new Guid("74ad3446-0890-4fae-bea1-c24681b97100") },
                    { new Guid("2148fa07-f0db-4a25-b801-2c8bf34e44dc"), new Guid("7f36f1ed-8972-4bbf-8085-6f789fac0378") },
                    { new Guid("a7dde3bb-7008-4359-968d-d62f12e6d819"), new Guid("7f36f1ed-8972-4bbf-8085-6f789fac0378") },
                    { new Guid("c27ab3fb-2b5e-4286-af32-d85587e195af"), new Guid("7f36f1ed-8972-4bbf-8085-6f789fac0378") },
                    { new Guid("2148fa07-f0db-4a25-b801-2c8bf34e44dc"), new Guid("b3aa473e-0d76-451c-9a14-b32a475714eb") },
                    { new Guid("a7dde3bb-7008-4359-968d-d62f12e6d819"), new Guid("b3aa473e-0d76-451c-9a14-b32a475714eb") },
                    { new Guid("c27ab3fb-2b5e-4286-af32-d85587e195af"), new Guid("b3aa473e-0d76-451c-9a14-b32a475714eb") },
                    { new Guid("2148fa07-f0db-4a25-b801-2c8bf34e44dc"), new Guid("d758dc03-6e00-4319-9293-131bf7888a32") },
                    { new Guid("a7dde3bb-7008-4359-968d-d62f12e6d819"), new Guid("d758dc03-6e00-4319-9293-131bf7888a32") },
                    { new Guid("c27ab3fb-2b5e-4286-af32-d85587e195af"), new Guid("d758dc03-6e00-4319-9293-131bf7888a32") },
                    { new Guid("2148fa07-f0db-4a25-b801-2c8bf34e44dc"), new Guid("e52c79dd-5738-4838-bdbd-6e332092adb2") },
                    { new Guid("a7dde3bb-7008-4359-968d-d62f12e6d819"), new Guid("e52c79dd-5738-4838-bdbd-6e332092adb2") },
                    { new Guid("c27ab3fb-2b5e-4286-af32-d85587e195af"), new Guid("e52c79dd-5738-4838-bdbd-6e332092adb2") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentPlans_CatalogCategoryId",
                table: "PaymentPlans",
                column: "CatalogCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentPlans_CatalogServiceId_CatalogCategoryId",
                table: "PaymentPlans",
                columns: new[] { "CatalogServiceId", "CatalogCategoryId" });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogCategories_Code",
                table: "CatalogCategories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatalogCategories_IsActive_SortOrder",
                table: "CatalogCategories",
                columns: new[] { "IsActive", "SortOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_CatalogCategories_ParentId",
                table: "CatalogCategories",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogServiceCategories_CategoryId",
                table: "CatalogServiceCategories",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogServices_CategoryId",
                table: "CatalogServices",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CatalogServices_Code",
                table: "CatalogServices",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CatalogServices_IsActive_IsPopular_SortOrder",
                table: "CatalogServices",
                columns: new[] { "IsActive", "IsPopular", "SortOrder" });

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentPlans_CatalogCategories_CatalogCategoryId",
                table: "PaymentPlans",
                column: "CatalogCategoryId",
                principalTable: "CatalogCategories",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentPlans_CatalogServiceCategories_CatalogServiceId_CatalogCategoryId",
                table: "PaymentPlans",
                columns: new[] { "CatalogServiceId", "CatalogCategoryId" },
                principalTable: "CatalogServiceCategories",
                principalColumns: new[] { "ServiceId", "CategoryId" });

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentPlans_CatalogServices_CatalogServiceId",
                table: "PaymentPlans",
                column: "CatalogServiceId",
                principalTable: "CatalogServices",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentPlans_CatalogCategories_CatalogCategoryId",
                table: "PaymentPlans");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentPlans_CatalogServiceCategories_CatalogServiceId_CatalogCategoryId",
                table: "PaymentPlans");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentPlans_CatalogServices_CatalogServiceId",
                table: "PaymentPlans");

            migrationBuilder.DropTable(
                name: "CatalogServiceCategories");

            migrationBuilder.DropTable(
                name: "CatalogServices");

            migrationBuilder.DropTable(
                name: "CatalogCategories");

            migrationBuilder.DropIndex(
                name: "IX_PaymentPlans_CatalogCategoryId",
                table: "PaymentPlans");

            migrationBuilder.DropIndex(
                name: "IX_PaymentPlans_CatalogServiceId_CatalogCategoryId",
                table: "PaymentPlans");

            migrationBuilder.DropColumn(
                name: "CatalogCategoryId",
                table: "PaymentPlans");

            migrationBuilder.DropColumn(
                name: "CatalogServiceId",
                table: "PaymentPlans");
        }
    }
}
