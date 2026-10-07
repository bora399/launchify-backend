using Moq;
using FluentAssertions;
using Xunit;
using Launchify.Application.Interfaces;
using Launchify.Domain.Entities;
using System.Threading.Tasks;

namespace Launchify.Tests.Application
{
    public class LandingPageQueryTests
    {
        [Fact]
        public async Task GetBySlugAsync_Sistemde_Olmayan_Bir_Slug_Arandiginda_Null_Donmeli()
        {
            var mockRepository = new Mock<ILandingPageRepository>();
            string olmayanSlug = "hayalet-proje";

            mockRepository
                .Setup(repo => repo.GetBySlugAsync(olmayanSlug))
                .ReturnsAsync((LandingPage)null);

            var sonuc = await mockRepository.Object.GetBySlugAsync(olmayanSlug);

            sonuc.Should().BeNull("çünkü veritabanında böyle bir slug yok");
        }

        [Fact]
        public async Task GetBySlugAsync_Gecerli_Bir_Slug_Geldiginde_Dogru_Projeyi_Donmeli()
        {
            var mockRepository = new Mock<ILandingPageRepository>();
            string gecerliSlug = "vios-coffee";
            var beklenenProje = new LandingPage { Slug = "vios-coffee", ProductName = "Vios Coffee & Bakery" };

            mockRepository
                .Setup(repo => repo.GetBySlugAsync(gecerliSlug))
                .ReturnsAsync(beklenenProje);

            var sonuc = await mockRepository.Object.GetBySlugAsync(gecerliSlug);

            sonuc.Should().NotBeNull();
            sonuc.Slug.Should().Be("vios-coffee");
            sonuc.ProductName.Should().Contain("Vios");
        }
    }
}