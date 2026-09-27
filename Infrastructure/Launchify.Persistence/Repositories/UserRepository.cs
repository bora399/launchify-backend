using Google.Cloud.Firestore;
using Launchify.Application.Interfaces;
using Launchify.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Launchify.Persistence.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly FirestoreDb _firestoreDb;
        private const string CollectionName = "Users";

        // Proje ID'sini dependency injection ile Program.cs'den alacağız
        public UserRepository(string projectId)
        {
            _firestoreDb = FirestoreDb.Create(projectId);
        }

        public async Task<AppUser> GetByIdAsync(string firebaseUid)
        {
            var docRef = _firestoreDb.Collection(CollectionName).Document(firebaseUid);
            var snapshot = await docRef.GetSnapshotAsync();

            if (!snapshot.Exists) return null;

            // Firestore'dan gelen veriyi Domain (AppUser) modelimize çeviriyoruz (Mapping)
            var data = snapshot.ToDictionary();
            return new AppUser
            {
                Id = snapshot.Id,
                Email = data.ContainsKey("Email") ? data["Email"].ToString() : string.Empty,
                FullName = data.ContainsKey("FullName") ? data["FullName"].ToString() : string.Empty,
                RemainingPageCredits = data.ContainsKey("RemainingPageCredits") ? Convert.ToInt32(data["RemainingPageCredits"]) : 0,
                IsActive = data.ContainsKey("IsActive") ? Convert.ToBoolean(data["IsActive"]) : true
            };
        }

        public async Task AddAsync(AppUser user)
        {
            var docRef = _firestoreDb.Collection(CollectionName).Document(user.Id);

            // Domain nesnemizi Firestore'un anlayacağı Dictionary formatına çeviriyoruz
            var userData = new Dictionary<string, object>
            {
                { "Email", user.Email },
                { "FullName", user.FullName },
                { "RemainingPageCredits", user.RemainingPageCredits },
                { "CreatedAt", DateTime.UtcNow }, // Firestore UTC zaman dilimini sever
                { "LastLoginAt", DateTime.UtcNow },
                { "IsActive", user.IsActive }
            };

            await docRef.SetAsync(userData);
        }

        // Şimdilik GetByEmail ve Update metotlarını boş bırakabilirsin, 
        // mantık tamamen Add ve GetById ile aynı şekilde yürüyor.
        public Task<AppUser> GetByEmailAsync(string email) => throw new NotImplementedException();
        public Task UpdateAsync(AppUser user) => throw new NotImplementedException();
    }
}