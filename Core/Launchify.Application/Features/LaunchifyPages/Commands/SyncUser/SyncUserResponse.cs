namespace Launchify.Application.Features.Users.Commands.SyncUser
{
    public class SyncUserResponse
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public int RemainingCredits { get; set; }
    }
}