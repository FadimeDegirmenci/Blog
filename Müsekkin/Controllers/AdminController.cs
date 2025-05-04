using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;
using System.Collections.Generic;
using Müsekkin.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Http;
using System.IO;

namespace Müsekkin.Controllers
{
    public class AdminController : Controller
    {
        private readonly IConfiguration _configuration;
        private const string AdminUsername = "admin";
        private const string AdminPassword = "1234";

        public AdminController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Login Sayfası (GET)
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        // Login İşlemi (POST)
        [HttpPost]
        public IActionResult Login(string username, string password)
        {
            if (username == AdminUsername && password == AdminPassword)
            {
                return RedirectToAction("ContentAdd", "Admin");
            }
            else
            {
                ViewBag.ErrorMessage = "Kullanıcı adı veya şifre hatalı.";
                return View();
            }
        }

        // İçerik Ekleme Sayfası (GET)
        [HttpGet]
        public IActionResult ContentAdd()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            List<SelectListItem> categories = new List<SelectListItem>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT Id, Name FROM Categories";
                SqlCommand command = new SqlCommand(query, connection);
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    categories.Add(new SelectListItem
                    {
                        Value = reader["Id"].ToString(),
                        Text = reader["Name"].ToString()
                    });
                }
            }

            ViewBag.Categories = categories;
            return View();
        }

        // İçerik Ekleme (POST)
        [HttpPost]
        public IActionResult ContentAdd(Content content, IFormFile ImageFile, IFormFile VideoFile)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            // Resim yükleme
            if (ImageFile != null && ImageFile.Length > 0)
            {
                var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", ImageFile.FileName);
                using (var stream = new FileStream(imagePath, FileMode.Create))
                {
                    ImageFile.CopyTo(stream);
                }
                content.ImagePath = "/uploads/" + ImageFile.FileName;
            }

            // Video yükleme
            if (VideoFile != null && VideoFile.Length > 0)
            {
                var videoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/uploads", VideoFile.FileName);
                using (var stream = new FileStream(videoPath, FileMode.Create))
                {
                    VideoFile.CopyTo(stream);
                }
                content.VideoPath = "/uploads/" + VideoFile.FileName;
            }

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"INSERT INTO Contents (Title, Body, CreatedAt, ViewCount, ImagePath, VideoPath, ContentType, CategoryId) 
                                 VALUES (@Title, @Body, GETDATE(), 0, @ImagePath, @VideoPath, @ContentType, @CategoryId)";

                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Title", content.Title);
                command.Parameters.AddWithValue("@Body", (object)content.Body ?? DBNull.Value);
                command.Parameters.AddWithValue("@ImagePath", (object)content.ImagePath ?? DBNull.Value);
                command.Parameters.AddWithValue("@VideoPath", (object)content.VideoPath ?? DBNull.Value);
                command.Parameters.AddWithValue("@ContentType", content.ContentType);
                command.Parameters.AddWithValue("@CategoryId", content.CategoryId);

                command.ExecuteNonQuery();
            }

            return RedirectToAction("ContentAdd");
        }

        // İçerik Düzenle Sayfası (GET)
        [HttpGet]
        public IActionResult Edit(int id)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");
            Content content = null;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM Contents WHERE Id = @Id";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Id", id);
                SqlDataReader reader = command.ExecuteReader();

                if (reader.Read())
                {
                    content = new Content
                    {
                        Id = (int)reader["Id"],
                        Title = reader["Title"].ToString(),
                        Body = reader["Body"] != DBNull.Value ? reader["Body"].ToString() : "",
                        CreatedAt = (DateTime)reader["CreatedAt"],
                        ViewCount = (int)reader["ViewCount"],
                        ImagePath = reader["ImagePath"] != DBNull.Value ? reader["ImagePath"].ToString() : "",
                        VideoPath = reader["VideoPath"] != DBNull.Value ? reader["VideoPath"].ToString() : "",
                        ContentType = reader["ContentType"].ToString(),
                        CategoryId = reader["CategoryId"] != DBNull.Value ? (int)reader["CategoryId"] : 0
                    };
                }
            }

            if (content == null)
            {
                return NotFound();
            }

            List<SelectListItem> categories = new List<SelectListItem>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT Id, Name FROM Categories";
                SqlCommand command = new SqlCommand(query, connection);
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    categories.Add(new SelectListItem
                    {
                        Value = reader["Id"].ToString(),
                        Text = reader["Name"].ToString()
                    });
                }
            }

            ViewBag.Categories = categories;

            return View(content);
        }

        // İçerik Düzenle (POST)
        [HttpPost]
        public IActionResult Edit(Content content)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = @"UPDATE Contents 
                                 SET Title = @Title, Body = @Body, ImagePath = @ImagePath, VideoPath = @VideoPath, ContentType = @ContentType, CategoryId = @CategoryId
                                 WHERE Id = @Id";

                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Title", content.Title);
                command.Parameters.AddWithValue("@Body", (object)content.Body ?? DBNull.Value);
                command.Parameters.AddWithValue("@ImagePath", (object)content.ImagePath ?? DBNull.Value);
                command.Parameters.AddWithValue("@VideoPath", (object)content.VideoPath ?? DBNull.Value);
                command.Parameters.AddWithValue("@ContentType", content.ContentType);
                command.Parameters.AddWithValue("@CategoryId", content.CategoryId);
                command.Parameters.AddWithValue("@Id", content.Id);

                command.ExecuteNonQuery();
            }

            return RedirectToAction("ContentList");
        }

        // İçerik Listesi Sayfası
        [HttpGet]
        public IActionResult ContentList()
        {
            List<Content> contents = new List<Content>();

            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "SELECT * FROM Contents";
                SqlCommand command = new SqlCommand(query, connection);
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    contents.Add(new Content
                    {
                        Id = (int)reader["Id"],
                        Title = reader["Title"].ToString(),
                        Body = reader["Body"] != DBNull.Value ? reader["Body"].ToString() : "",
                        CreatedAt = (DateTime)reader["CreatedAt"],
                        ViewCount = (int)reader["ViewCount"],
                        ImagePath = reader["ImagePath"] != DBNull.Value ? reader["ImagePath"].ToString() : "",
                        VideoPath = reader["VideoPath"] != DBNull.Value ? reader["VideoPath"].ToString() : "",
                        ContentType = reader["ContentType"].ToString(),
                        CategoryId = reader["CategoryId"] != DBNull.Value ? (int)reader["CategoryId"] : 0
                    });
                }
            }

            return View(contents);
        }

        // İçerik Silme
        [HttpGet]
        public IActionResult Delete(int id)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                string query = "DELETE FROM Contents WHERE Id = @Id";
                SqlCommand command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Id", id);
                command.ExecuteNonQuery();
            }

            return RedirectToAction("ContentList");
        }

        // Bülten Kayıtlarını Listeleme
        [HttpGet]
        public IActionResult NewsletterList()
        {
            List<NewsletterSubscriber> subscribers = new List<NewsletterSubscriber>();
            string connectionString = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new SqlConnection(connectionString))
            {
                string query = "SELECT * FROM NewsletterSubscribers ORDER BY SubscribedAt DESC";
                SqlCommand cmd = new SqlCommand(query, connection);
                connection.Open();
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    subscribers.Add(new NewsletterSubscriber
                    {
                        Id = (int)reader["Id"],
                        Email = reader["Email"].ToString(),
                        SubscribedAt = (DateTime)reader["SubscribedAt"]
                    });
                }

                connection.Close();
            }

            return View(subscribers);
        }
    }
}
