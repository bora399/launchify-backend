using Google.Cloud.Firestore;
using Launchify.Application.Interfaces;
using Launchify.Domain.Entities;

namespace Launchify.Persistence.Repositories;

public class UserRepository(FirestoreDb firestoreDb) : IUserRepository
{
    private static User MapUser(DocumentSnapshot snapshot) => new()
    {
        Id = snapshot.Id,
        Email = snapshot.GetValue<string>("Email"),
        CreatedAt = snapshot.GetValue<DateTime>("CreatedAt"),
        RemainingCredits = snapshot.GetValue<int>("RemainingCredits")
    };

    public async Task<User> GetByIdAsync(string id)
    {
        var snapshot = await firestoreDb.Collection("Users").Document(id).GetSnapshotAsync();
        return snapshot.Exists ? MapUser(snapshot) : null!;
    }

    public async Task<User> SyncUserAsync(string id, string email)
    {
        var docRef = firestoreDb.Collection("Users").Document(id);
        return await firestoreDb.RunTransactionAsync(async transaction =>
        {
            var snapshot = await transaction.GetSnapshotAsync(docRef);
            if (snapshot.Exists) return MapUser(snapshot);

            var user = new User
            {
                Id = id,
                Email = email,
                CreatedAt = DateTime.UtcNow,
                RemainingCredits = 3
            };
            transaction.Set(docRef, new Dictionary<string, object>
            {
                { "Email", user.Email },
                { "CreatedAt", user.CreatedAt },
                { "RemainingCredits", user.RemainingCredits }
            });
            return user;
        });
    }

    public async Task<bool> DeductCreditAsync(string id)
    {
        var docRef = firestoreDb.Collection("Users").Document(id);
        return await firestoreDb.RunTransactionAsync(async transaction =>
        {
            var snapshot = await transaction.GetSnapshotAsync(docRef);
            if (!snapshot.Exists) return false;

            int credits = snapshot.GetValue<int>("RemainingCredits");
            if (credits <= 0) return false;

            transaction.Update(docRef, "RemainingCredits", credits - 1);
            return true;
        });
    }

    public async Task<bool> RefundCreditAsync(string id)
    {
        var docRef = firestoreDb.Collection("Users").Document(id);
        return await firestoreDb.RunTransactionAsync(async transaction =>
        {
            var snapshot = await transaction.GetSnapshotAsync(docRef);
            if (!snapshot.Exists) return false;

            transaction.Update(docRef, "RemainingCredits",
                Math.Min(3, snapshot.GetValue<int>("RemainingCredits") + 1));
            return true;
        });
    }
}
