namespace RssBot;

public class Post
{
    public string Description { get; set; } = default!;

    public string Uri { get; set; } = default!;

    public string RssUri { get; set; } = default!;

    public string Title { get; set; } = default!;

    public string RssTitle { get; set; } = default!;

    public string Author { get; set; } = default!;

    public string PublishedAt { get; set; } = default!;
}