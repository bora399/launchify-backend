using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Launchify.Domain.Entities
{
    public class User
    {
        public string Id { get; set; } 
        public string Email { get; set; }
        public DateTime CreatedAt { get; set; }
        public int RemainingCredits { get; set; } = 10;
    }
}
