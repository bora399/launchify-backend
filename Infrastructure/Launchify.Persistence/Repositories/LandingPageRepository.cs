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
                { "UserId", page.UserId ?? "" },
                { "Slug", page.Slug ?? "" },
                { "ProductName", page.ProductName ?? "" },
                { "TemplateType", page.TemplateType ?? "aurora" },
                { "ContactEmail", page.ContactEmail ?? "" },
                { "DemoLink", page.DemoLink ?? "" },
                { "ProductDescription", page.ProductDescription ?? "" },
                { "CreatedAt", page.CreatedAt.ToUniversalTime() },
                { "IsActive", page.IsActive },
                { "AiConfig", new Dictionary<string, object>
                    {
                        { "AiGeneratedHeroTitle", page.AiConfig?.AiGeneratedHeroTitle ?? "" },
                        { "AiGeneratedMarketingCopy", page.AiConfig?.AiGeneratedMarketingCopy ?? "" },
                        { "AccentColor", page.AiConfig?.AccentColor ?? "" },
                        { "CallToActionText", page.AiConfig?.CallToActionText ?? "Erken Erişime Katıl" },
                        { "Features", page.AiConfig?.Features?.Select(f => new Dictionary<string, object>
                            {
                                { "Title", f.Title ?? "" },
                                { "Description", f.Description ?? "" }
                            }).ToList() ?? new List<Dictionary<string, object>>() }
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

        public async Task<IEnumerable<LandingPage>> GetByUserIdAsync(string userId)
        {
            var query = _firestoreDb.Collection("LandingPages").WhereEqualTo("UserId", userId);
            var snapshot = await query.GetSnapshotAsync();

            var landingPages = new List<LandingPage>();

            foreach (var document in snapshot.Documents)
            {
                if (document.Exists)
                {
                    landingPages.Add(MapSnapshotToLandingPage(document));
                }
            }

            return landingPages;
        }

        public async Task DeleteAsync(string id)
        {
            var docRef = _firestoreDb.Collection("LandingPages").Document(id);
            await docRef.DeleteAsync();
        }

        private LandingPage MapSnapshotToLandingPage(DocumentSnapshot snapshot)
        {
            var data = snapshot.ToDictionary();
            var aiDict = data.ContainsKey("AiConfig") && data["AiConfig"] is Dictionary<string, object>
                ? (Dictionary<string, object>)data["AiConfig"]
                : new Dictionary<string, object>();

            var featuresList = new List<FeatureItem>();
            if (aiDict.ContainsKey("Features") && aiDict["Features"] is IEnumerable<object> rawFeatures)
            {
                foreach (var rawFeature in rawFeatures)
                {
                    if (rawFeature is Dictionary<string, object> fDict)
                    {
                        featuresList.Add(new FeatureItem
                        {
                            Title = fDict.ContainsKey("Title") ? fDict["Title"].ToString() : "",
                            Description = fDict.ContainsKey("Description") ? fDict["Description"].ToString() : ""
                        });
                    }
                }
            }

            return new LandingPage
            {
                Id = Guid.Parse(data["Id"].ToString()),
                UserId = data.ContainsKey("UserId") ? data["UserId"].ToString() : null,
                Slug = data.ContainsKey("Slug") ? data["Slug"].ToString() : null,
                ProductName = data.ContainsKey("ProductName") ? data["ProductName"].ToString() : null,
                ProductDescription = data.ContainsKey("ProductDescription") ? data["ProductDescription"].ToString() : null,
                ContactEmail = data.ContainsKey("ContactEmail") ? data["ContactEmail"].ToString() : null,
                DemoLink = data.ContainsKey("DemoLink") ? data["DemoLink"].ToString() : null,
                TemplateType = data.ContainsKey("TemplateType") ? data["TemplateType"].ToString() :
                              (data.ContainsKey("ThemeType") ? data["ThemeType"].ToString() : "aurora"),
                TotalVisits = data.ContainsKey("TotalVisits") ? Convert.ToInt32(data["TotalVisits"]) : 0,
                AiConfig = new AiPageConfig
                {
                    AiGeneratedHeroTitle = aiDict.ContainsKey("AiGeneratedHeroTitle") ? aiDict["AiGeneratedHeroTitle"].ToString() : null,
                    AiGeneratedMarketingCopy = aiDict.ContainsKey("AiGeneratedMarketingCopy") ? aiDict["AiGeneratedMarketingCopy"].ToString() : null,
                    AccentColor = aiDict.ContainsKey("AccentColor") ? aiDict["AccentColor"].ToString() : null,
                    CallToActionText = aiDict.ContainsKey("CallToActionText") ? aiDict["CallToActionText"].ToString() : null,
                    Features = featuresList
                },
            };
        }

        public async Task IncrementVisitCountAsync(string pageIdOrSlug, int visitCount)
        {
            try
            {
                DocumentReference docRef = null;

                var directDoc = _firestoreDb.Collection("LandingPages").Document(pageIdOrSlug);
                var directSnapshot = await directDoc.GetSnapshotAsync();

                if (directSnapshot.Exists)
                {
                    docRef = directDoc;
                }
                else
                {
                    var query1 = _firestoreDb.Collection("LandingPages").WhereEqualTo("Slug", pageIdOrSlug);
                    var snap1 = await query1.GetSnapshotAsync();

                    if (snap1.Documents.Count > 0)
                    {
                        docRef = snap1.Documents[0].Reference;
                    }
                    else
                    {
                        var query2 = _firestoreDb.Collection("LandingPages").WhereEqualTo("slug", pageIdOrSlug);
                        var snap2 = await query2.GetSnapshotAsync();

                        if (snap2.Documents.Count > 0)
                        {
                            docRef = snap2.Documents[0].Reference;
                        }
                    }
                }

                if (docRef == null)
                {
                    Console.WriteLine($"[UYARI] Firestore'da '{pageIdOrSlug}' bulunamadığı için sayaç artırılamadı!");
                    return;
                }

                var updateData = new Dictionary<string, object>
                {
                    { "TotalVisits", FieldValue.Increment(visitCount) }
                };

                await docRef.SetAsync(updateData, SetOptions.MergeAll);

                Console.WriteLine($"[GERÇEK BAŞARI] '{pageIdOrSlug}' için Firestore'a {visitCount} ziyaret İŞLENDİ.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KRİTİK HATA] Analitik '{pageIdOrSlug}' için yazılırken çöktü: {ex.Message}");
            }
        }
    }
}