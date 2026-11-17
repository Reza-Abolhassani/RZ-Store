using System;
using System.Collections.Generic;

namespace API.Models
{
    public class ProductSearchDto
    {
        //vorodi haye karbar
        public string Text { get; set; }
        public long FromPrice { get; set; }//az gheymat
        public long ToPrice { get; set; }//ta gheymat
        public int FromCount { get; set; }
        public int ToCount { get; set; }
        public Guid CategoryId { get; set; }


        //khoroji
        public List<ProductDto> Products { get; set; }
    }
}
