using Google.Cloud.Firestore;
using Launchify.Application.Interfaces;
using Launchify.Domain.Entities;

namespace Launchify.Persistence.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly FirestoreDb _firestoreDb;

        public UserRepository(FirestoreDb firestoreDb)
        {
            _firestoreDb = firestoreDb;
        }

        public async Task<User> GetByIdAsync(string id)
        {
            var docRef = _firestoreDb.Collection("Users").Document(id);
            var snapshot = await docRef.GetSnapshotAsync();

            if (!snapshot.Exists) return null;

            return new User
            {
                Id = snapshot.Id,
                Email = snapshot.GetValue<string>("Email"),
                CreatedAt = snapshot.GetValue<DateTime>("CreatedAt"),
                RemainingCredits = snapshot.GetValue<int>("RemainingCredits")
            };
        }

        public async Task<User> SyncUserAsync(string id, string email)
        {
            var docRef = _firestoreDb.Collection("Users").Document(id);
            var snapshot = await docRef.GetSnapshotAsync();

            if (snapshot.Exists)
            {
                return await GetByIdAsync(id);
            }

            var newUser = new User
            {
                Id = id,
                Email = email,
                CreatedAt = DateTime.UtcNow,
                RemainingCredits = 3
            };

            var userData = new Dictionary<string, object>
            {
                { "Email", newUser.Email },
                { "CreatedAt", newUser.CreatedAt.ToUniversalTime() },
                { "RemainingCredits", newUser.RemainingCredits }
            };

            await docRef.SetAsync(userData);
            return newUser;
        }

        public async Task<bool> DeductCreditAsync(string id)
        {
            var docRef = _firestoreDb.Collection("Users").Document(id);
            var snapshot = await docRef.GetSnapshotAsync();

            if (!snapshot.Exists) return false;

            int currentCredits = snapshot.GetValue<int>("RemainingCredits");

            if (currentCredits <= 0) return false;

            await docRef.UpdateAsync("RemainingCredits", currentCredits - 1);
            return true;
        }
    }
}