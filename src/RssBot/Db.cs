using Microsoft.Extensions.Logging;
using System;
using System.Data.SQLite;
using System.Diagnostics;
using System.Security.Policy;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using System.Text.Json;
using static System.Formats.Asn1.AsnWriter;

namespace RssBot
{
    public class Db
    {
        private readonly string connectionString;

        private readonly string dbPath;

        public Db(string dbPath)
        {
            this.dbPath = dbPath;
            this.connectionString = $"Data Source={dbPath};Version=3;";

            CreateDb();
        }

        public SQLiteConnection GetConnection()
        {
            return new SQLiteConnection(connectionString);
        }

        private void CreateDb()
        {
            if (!File.Exists(dbPath))
            {
                // Create the database file
                SQLiteConnection.CreateFile(dbPath);

                // Open a connection to the database
                using (var connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    using (var command = new SQLiteCommand(
                        "CREATE TABLE instances (host string PRIMARY KEY, weight INT)",
                        connection))
                    {
                        command.ExecuteNonQuery();
                    }

                    using (var command = new SQLiteCommand(
                        "CREATE TABLE posts(uri string PRIMARY KEY, rssUri string, jsonObject TEXT, published_at TEXT, status INTEGER)",
                        connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        internal void InsertOrIgnorePost(Post post)
        {
            using(var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                var content = JsonSerializer.Serialize(post, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });

                // FIXME: published_at should not be CURRENT_TIMESTAMP but come from the jsonObject
                using (var command = new SQLiteCommand(
                    "INSERT OR IGNORE INTO posts (uri, rssUri, jsonObject, published_at) VALUES (@uri, @rssUri, @jsonObject, @publishedAt)",
                    connection))
                {
                    command.Parameters.AddWithValue("@uri", post.Uri);
                    command.Parameters.AddWithValue("@rssUri", post.RssUri);
                    command.Parameters.AddWithValue("@jsonObject", content);
                    command.Parameters.AddWithValue("@publishedAt", post.PublishedAt);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void DeleteOldPosts()
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                // TODO: add datetime field to posts table

                using (var command = new SQLiteCommand(
                    "DELETE FROM posts WHERE datetime('now', '-1 day') > datetime(date)",
                    connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

          public IEnumerable<Post> GetPosts(
            string sql = "SELECT * FROM posts LIMIT 1",
            IDictionary<string, object>? parameters = null) {
            IList<Post> posts = new List<Post>();

            using (SQLiteConnection connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                
                using (var command = new SQLiteCommand(sql, connection))
                {
                    foreach (var parameter in parameters ?? new Dictionary<string, object>())
                    {
                        command.Parameters.AddWithValue(parameter.Key, parameter.Value);
                    }

                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string jsonObject = reader["jsonObject"]?.ToString() ?? string.Empty;

                            var post = JsonSerializer.Deserialize<Post>(jsonObject, new JsonSerializerOptions
                            {
                                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                                WriteIndented = true
                            });

                            post!.Uri = reader["uri"]!.ToString()!;
                            
                            posts.Add(post);
                        }
                    }
                }
            }

            return posts;
        }

        public void MarkPostAsPosted(Post post)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                using (var command = new SQLiteCommand(
                    "UPDATE posts SET status = 1 WHERE uri = @uri LIMIT 1",
                    connection))
                {
                    command.Parameters.AddWithValue("@uri", post.Uri!);
                    command.ExecuteNonQuery();
                }
            }
        }

        public void MarkPostAsFail(Post post)
        {
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                using (var command = new SQLiteCommand(
                    "UPDATE posts SET status = 2 WHERE uri = @uri LIMIT 1",
                    connection))
                {
                    command.Parameters.AddWithValue("@uri", post.Uri!);
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}