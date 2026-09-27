using Google.Cloud.Firestore;
using Launchify.Application.Interfaces;
using Launchify.Domain.Entities;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Launchify.Infrastructure.Repositories
{
    public class LandingPageRepository : ILandingPageRepository
    {
        private readonly FirestoreDb _firestoreDb;

        public LandingPageRepository(IConfiguration configuration)
        {
            string projectId = configuration["Firebase:ProjectId"];
            _firestoreDb = FirestoreDb.Create(projectId);
        }

        public async Task AddAsync(LandingPage page)
        {
            var docRef = _firestoreDb.Collection("LandingPages").Document(page.Id.ToString());

            var firestoreData = new Dictionary<string, object>
            {
                { "Id", page.Id.ToString() },
                { "Slug", page.Slug ?? "" }, // YENİ: URL ismi Firestore'a kaydediliyor
                { "UserId", page.UserId ?? "" },
                { "ProductName", page.ProductName ?? "" },
                { "ThemeType", page.ThemeType ?? "" },
                { "ContactEmail", page.ContactEmail ?? "" },
                { "AdminPin", page.AdminPin ?? "" },
                { "DemoLink", page.DemoLink ?? "" },
                { "ProductDescription", page.ProductDescription ?? "" },
                { "CreatedAt", page.CreatedAt.ToUniversalTime() },
                { "IsActive", page.IsActive },
                { "AiConfig", new Dictionary<string, object>
                    {
                        { "AiGeneratedHeroTitle", page.AiConfig?.AiGeneratedHeroTitle ?? "" },
                        { "AiGeneratedMarketingCopy", page.AiConfig?.AiGeneratedMarketingCopy ?? "" },
                        { "AccentColor", page.AiConfig?.AccentColor ?? "" }
                    }
                }
            };

            await docRef.SetAsync(firestoreData);
        }

        public async Task<LandingPage> GetByIdAsync(string id)
        {
            var docRef = _firestoreDb.Collection("LandingPages").Document(id);
            var snapshot = await docRef.GetSnapshotAsync();

            if (snapshot.Exists)
            {
                return MapSnapshotToLandingPage(snapshot);
            }
            return null;
        }

        // YENİ: İsme (Slug) göre Firestore'da arama yapan metot
        public async Task<LandingPage> GetBySlugAsync(string slug)
        {
            var query = _firestoreDb.Collection("LandingPages").WhereEqualTo("Slug", slug);
            var snapshot = await query.GetSnapshotAsync();

            var document = snapshot.Documents.FirstOrDefault();

            if (document != null && document.Exists)
            {
                return MapSnapshotToLandingPage(document);
            }
            return null;
        }

        // Kod tekrarını önlemek için Mapping işlemini ortak bir metoda aldık
        private LandingPage MapSnapshotToLandingPage(DocumentSnapshot snapshot)
        {
            var data = snapshot.ToDictionary();
            var aiDict = data.ContainsKey("AiConfig") && data["AiConfig"] is Dictionary<string, object>
                ? (Dictionary<string, object>)data["AiConfig"]
                : new Dictionary<string, object>();

            return new LandingPage
            {
                Id = Guid.Parse(data["Id"].ToString()),
                Slug = data.ContainsKey("Slug") ? data["Slug"].ToString() : null, // YENİ: Eşlemeye dahil edildi
                ProductName = data.ContainsKey("ProductName") ? data["ProductName"].ToString() : null,
                ThemeType = data.ContainsKey("ThemeType") ? data["ThemeType"].ToString() : null,
                ProductDescription = data.ContainsKey("ProductDescription") ? data["ProductDescription"].ToString() : null,
                DemoLink = data.ContainsKey("DemoLink") ? data["DemoLink"].ToString() : null,
                AiConfig = new AiPageConfig
                {
                    AiGeneratedHeroTitle = aiDict.ContainsKey("AiGeneratedHeroTitle") ? aiDict["AiGeneratedHeroTitle"].ToString() : null,
                    AiGeneratedMarketingCopy = aiDict.ContainsKey("AiGeneratedMarketingCopy") ? aiDict["AiGeneratedMarketingCopy"].ToString() : null,
                    AccentColor = aiDict.ContainsKey("AccentColor") ? aiDict["AccentColor"].ToString() : null
                }
            };
        }
    }
}