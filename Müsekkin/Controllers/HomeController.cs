using Microsoft.AspNetCore.Mvc;
using Müsekkin.Models;
using Microsoft.Extensions.Configuration;
using System.Data.SqlClient;
using System.Collections.Generic;
using System;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Müsekkin.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration _configuration;

        public HomeController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        // Ana Sayfa (Tüm içerikler veya Arama)
        public IActionResult Index(string searchQuery = null)
        {
            var contents = GetContents(searchQuery: searchQuery);
            return View(contents);
        }

        // Bültene Kaydolma (POST)
        [HttpPost]
        public IActionResult SubscribeNewsletter(string email)
        {
            if (!string.IsNullOrWhiteSpace(email))
            {
                string connectionString = _configuration.GetConnectionString("DefaultConnection");

                using (var connection = new SqlConnection(connectionString))
                {
                    string query = "INSERT INTO NewsletterSubscribers (Email) VALUES (@Email)";
                    SqlCommand cmd = new SqlCommand(query, connection);
                    cmd.Parameters.AddWithValue("@Email", email); // E-posta parametresi ekleniyor

                    connection.Open();
                    cmd.ExecuteNonQuery();
                    connection.Close();
                }

                TempData["SubscriptionMessage"] = "Bültene başarıyla kaydoldunuz!";
            }

            return RedirectToAction("Index");
        }

        // İçerik Detay Sayfası
        [HttpGet]
        public IActionResult Details(int id)
        {
            Content content = null;
            string connStr = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new SqlConnection(connStr))
            {
                connection.Open();

                var updateQuery = "UPDATE Contents SET ViewCount = ViewCount + 1 WHERE Id = @Id";
                using (var updateCmd = new SqlCommand(updateQuery, connection))
                {
                    updateCmd.Parameters.AddWithValue("@Id", id);
                    updateCmd.ExecuteNonQuery();
                }

                var selectQuery = "SELECT * FROM Contents WHERE Id = @Id";
                using (var selectCmd = new SqlCommand(selectQuery, connection))
                {
                    selectCmd.Parameters.AddWithValue("@Id", id);
                    using (var reader = selectCmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            content = new Content
                            {
                                Id = (int)reader["Id"],
                                Title = reader["Title"].ToString(),
                                Body = reader["Body"]?.ToString(),
                                CreatedAt = (DateTime)reader["CreatedAt"],
                                ViewCount = (int)reader["ViewCount"],
                                ImagePath = reader["ImagePath"]?.ToString(),
                                VideoPath = reader["VideoPath"]?.ToString(),
                                ContentType = reader["ContentType"].ToString(),
                                CategoryId = reader["CategoryId"] != DBNull.Value ? (int)reader["CategoryId"] : 0
                            };
                        }
                    }
                }
            }

            if (content == null) return NotFound();
            return View(content);
        }

        // Kategoriye Göre İçerik Filtreleme
        [HttpGet]
        public IActionResult CategoryFilter(int id)
        {
            var contents = GetContents(categoryId: id);
            return View("Index", contents);
        }

        // Sabit Sayfalar
        [HttpGet] public IActionResult Categories() => View("Index", GetContents());
        [HttpGet] public IActionResult FadimeDefter() => View("Index", GetContents(contentType: "Yazi"));
        [HttpGet] public IActionResult FadimeKadraj() => View("Index", GetContents(contentType: "Resim"));
        [HttpGet] public IActionResult FadimeTV() => View("Index", GetContents(contentType: "Video"));

        // İçerik Düzenle (GET)
        [HttpGet]
        public IActionResult Edit(int id)
        {
            Content content = null;
            string connStr = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new SqlConnection(connStr))
            {
                connection.Open();
                var query = "SELECT * FROM Contents WHERE Id = @Id";
                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            content = new Content
                            {
                                Id = (int)reader["Id"],
                                Title = reader["Title"].ToString(),
                                Body = reader["Body"]?.ToString(),
                                CreatedAt = (DateTime)reader["CreatedAt"],
                                ViewCount = (int)reader["ViewCount"],
                                ImagePath = reader["ImagePath"]?.ToString(),
                                VideoPath = reader["VideoPath"]?.ToString(),
                                ContentType = reader["ContentType"].ToString(),
                                CategoryId = reader["CategoryId"] != DBNull.Value ? (int)reader["CategoryId"] : 0
                            };
                        }
                    }
                }
            }

            if (content == null) return NotFound();

            ViewBag.Categories = GetCategories();
            return View(content);
        }

        // İçerik Düzenle (POST)
        [HttpPost]
        public IActionResult Edit(Content model)
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new SqlConnection(connStr))
            {
                connection.Open();
                var query = @"UPDATE Contents SET 
                                Title = @Title, 
                                Body = @Body, 
                                ImagePath = @ImagePath, 
                                VideoPath = @VideoPath, 
                                ContentType = @ContentType, 
                                CategoryId = @CategoryId 
                              WHERE Id = @Id";

                using (var cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@Title", model.Title);
                    cmd.Parameters.AddWithValue("@Body", (object?)model.Body ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ImagePath", (object?)model.ImagePath ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@VideoPath", (object?)model.VideoPath ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ContentType", model.ContentType);
                    cmd.Parameters.AddWithValue("@CategoryId", model.CategoryId);
                    cmd.Parameters.AddWithValue("@Id", model.Id);

                    cmd.ExecuteNonQuery();
                }
            }

            return RedirectToAction("Index");
        }

        // Yardımcı Fonksiyonlar
        private List<Content> GetContents(string contentType = null, int? categoryId = null, string searchQuery = null)
        {
            List<Content> contents = new();
            string connStr = _configuration.GetConnectionString("DefaultConnection");

            using (var connection = new SqlConnection(connStr))
            {
                connection.Open();
                string query = "SELECT * FROM Contents WHERE 1=1";

                if (!string.IsNullOrEmpty(contentType))
                    query += " AND ContentType = @ContentType";
                if (categoryId.HasValue)
                    query += " AND CategoryId = @CategoryId";
                if (!string.IsNullOrEmpty(searchQuery))
                    query += " AND Title LIKE @SearchQuery";

                using (var cmd = new SqlCommand(query, connection))
                {
                    if (!string.IsNullOrEmpty(contentType))
                        cmd.Parameters.AddWithValue("@ContentType", contentType);
                    if (categoryId.HasValue)
                        cmd.Parameters.AddWithValue("@CategoryId", categoryId.Value);
                    if (!string.IsNullOrEmpty(searchQuery))
                        cmd.Parameters.AddWithValue("@SearchQuery", $"%{searchQuery}%");

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            contents.Add(new Content
                            {
                                Id = (int)reader["Id"],
                                Title = reader["Title"].ToString(),
                                Body = reader["Body"]?.ToString(),
                                CreatedAt = (DateTime)reader["CreatedAt"],
                                ViewCount = (int)reader["ViewCount"],
                                ImagePath = reader["ImagePath"]?.ToString(),
                                VideoPath = reader["VideoPath"]?.ToString(),
                                ContentType = reader["ContentType"].ToString(),
                                CategoryId = reader["CategoryId"] != DBNull.Value ? (int)reader["CategoryId"] : 0
                            });
                        }
                    }
                }
            }

            return contents;
        }

        private List<SelectListItem> GetCategories()
        {
            return new List<SelectListItem>
            {
                new SelectListItem { Value = "1", Text = "Psikoloji" },
                new SelectListItem { Value = "2", Text = "Sosyoloji" },
                new SelectListItem { Value = "3", Text = "Edebiyat" },
                new SelectListItem { Value = "4", Text = "Gezi" }
            };
        }
    }
}
