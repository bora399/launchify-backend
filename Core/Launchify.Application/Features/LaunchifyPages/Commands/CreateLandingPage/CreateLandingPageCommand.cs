using MediatR;
using System.Collections.Generic;

// İsim uzayını (namespace) artık B2B projene göre daha kurumsal bir hale getirebilirsin, 
// şimdilik var olanı kullanıyorum ki diğer yerler bozulmasın.
namespace Launchify.Application.Features.LaunchifyPages.Commands.CreateLandingPage
{
    public class CreateLandingPageCommand : IRequest<CreateLandingResponse> // Dönüş tipini Handler ile eşitledik
    {
        public string UserId { get; set; }

        // --- Kurumsal B2B İsimlendirmeleri ---
        public string ProductName { get; set; } // Eskiden CoupleNames
        public string ThemeType { get; set; } // "modern" veya "classic"
        public string ContactEmail { get; set; } // Eskiden PhoneNumber
        public string AdminPin { get; set; }
        public string DemoLink { get; set; } // Eskiden SongLink (Ürün demo/video linki)
        public string ProductDescription { get; set; } // Eskiden OriginalPersonalMessage (Brief)

        // Fotoğrafları Frontend Firebase Storage'a yükleyip bize sadece URL'lerini ve notlarını gönderecek
        public List<PhotoDto> Photos { get; set; } = new List<PhotoDto>();
    }

    public class PhotoDto
    {
        public string ImageUrl { get; set; }
        public string Note { get; set; }
        public int OrderIndex { get; set; }
    }

    // Command'in geri döneceği cevap paketi
    public class CreateLandingResponse
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public string GeneratedPageId { get; set; } // Next.js'in yönlendirme (Redirect) yapabilmesi için ID'yi dönüyoruz
    }
}