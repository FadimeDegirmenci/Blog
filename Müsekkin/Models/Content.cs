namespace Müsekkin.Models
{
    public class Content
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ViewCount { get; set; }
        public string ImagePath { get; set; }
        public string VideoPath { get; set; }
        public string ContentType { get; set; }
        public int CategoryId { get; set; }
    }

}
