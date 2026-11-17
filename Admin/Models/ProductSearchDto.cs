using System;
using System.Collections.Generic;

namespace Admin.Models
{
    public class ProductSearchDto
    {

        public int FromPrice { get; set; }
        public int ToPrice { get; set; }
        public string Text { get; set; }
        public Guid CategoryId { get; set; }

        public List<ProductDto> Products { get; set; }
        public List<CategoryDto> Categories { get; set; }
    }
}
