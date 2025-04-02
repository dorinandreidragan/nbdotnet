using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var books = new ConcurrentDictionary<string, Book>();
var baseUrl = builder.Configuration["BaseUrl"];

app.MapPost("/addBook", (AddBookRequest request) =>
{
    var bookId = Guid.NewGuid().ToString();
    books[bookId] = new Book(request.Title, request.Author, request.ISBN);
    return Results.Ok(new AddBookResponse(bookId));
});

app.MapGet("/books/{id}", (string id) =>
{
    if (books.TryGetValue(id, out var book))
    {
        return Results.Ok(book);
    }
    return Results.NotFound();
});

app.Run();

public record AddBookRequest(string Title, string Author, string ISBN);
public record AddBookResponse(string BookId);
public record Book(string Title, string Author, string ISBN);

// Explicitly define Program as partial for integration tests
public partial class Program { }