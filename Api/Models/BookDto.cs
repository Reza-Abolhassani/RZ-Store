using Entities;
using System;
using WebFramework.Api;

namespace API.Models
{
    public class BookDto : BaseDto<BookDto, Book>
    {
        public string Title { get; set; }
        public int Price { get; set; }
        public Guid CategoryBookId { get; set; }
        public string CategoryBookTitle { get; set; }
    }
}
