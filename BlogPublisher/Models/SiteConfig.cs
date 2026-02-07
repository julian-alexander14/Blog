namespace BlogPublisher.Models;

public record SiteConfig(
    string SiteTitle,
    string Author,
    string BaseUrl,
    string Description,
    string Footer
);
