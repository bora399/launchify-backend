using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Launchify.Domain.Entities
{
    public class PhotoItem : BaseEntity
    {
        public string ImageUrl { get; set; }
        public string Note { get; set; }
        public int OrderIndex { get; set; }
    }
}
