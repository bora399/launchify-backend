using Google.Cloud.Firestore;
using Launchify.Application.Interfaces;
using Launchify.Domain.Entities;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Launchify.Infrastructure.Repositories
{
    public class WaitlistRepository : IWaitlistRepository
    {
        private readonly FirestoreDb _firestoreDb;

        public WaitlistRepository(IConfiguration configuration)
        {
            string projectId = configuration["Firebase:ProjectId"];
            _firestoreDb = FirestoreDb.Create(projectId);
        }

        public async Task AddAsync(WaitlistEntry entry)
        {
            var docRef = _firestoreDb.Collection("Waitlists").Document(entry.Id.ToString());

            var firestoreData = new Dictionary<string, object>
            {
                { "Id", entry.Id.ToString() },
                { "PageId", entry.PageId },
                { "Email", entry.Email },
                { "CreatedAt", entry.CreatedAt.ToUniversalTime() }
            };

            await docRef.SetAsync(firestoreData);
            Console.WriteLine($"[BAŞARI] {entry.Email} adresi {entry.PageId} projesi için kaydedildi!");
        }

        public async Task<IEnumerable<WaitlistEntry>> GetByPageIdAsync(string pageId)
        {
            return new List<WaitlistEntry>();
        }
    }
}