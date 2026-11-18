using System.Collections.Generic;

namespace Client.Models
{
    public class IndexDto
    {
        public List<CategoryDto> Categories { get; set; } // دسته‌بندی‌های فروشگاه
        public List<ProductDto> NewProducts { get; set; }  // جدیدترین محصولات
        public List<ProductDto> CheapestProducts { get; set; }  // محصولات ارزان‌تر / کم‌قیمت‌ترین محصولات.
        public List<ProductDto> FeaturedProducts { get; set; }// محصولات منتخب و جذاب

        //public List<BannerDto> Banners { get; set; } = new(); // بنرهای تبلیغاتی صفحه اصلی
        //

        public List<HomeBannerDto> HomeBanners { get; set; } = new();
        public List<ProductDto> BestSellingProducts { get; set; } = new();  // محصولات پرفروش       
        public List<ProductDto> DiscountedProducts { get; set; } = new(); // محصولات تخفیف‌دار       
        public List<CategoryDto> PopularCategories { get; set; } = new(); // دسته‌بندی‌های محبوب

        public IndexDto()
        {
            Categories = new List<CategoryDto>();

            NewProducts = new List<ProductDto>();

            CheapestProducts = new List<ProductDto>();

            FeaturedProducts = new List<ProductDto>();

            BestSellingProducts = new List<ProductDto>();

            DiscountedProducts = new List<ProductDto>();

            PopularCategories = new List<CategoryDto>();
        }
    }
}
