using System;

namespace BlogPublisher.Models;

public record BlogPost(
    string Slug,
    string Title,
    DateTime Date,
    string ContentHtml,
    string Author
);