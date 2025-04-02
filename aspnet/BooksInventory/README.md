# Testing Minimal Web APIs with ASP.NET: A Developer's Guide

In this article, we’ll explore how to test a minimal web API using a Book Inventory API as an example. We’ll focus on practical techniques, including integration testing, manual testing with REST Client and `.http` files, and ways to avoid code clutter with extension methods.

Let’s dive in!

---

## Setting Up the Stage

First, create the solution and projects:

```bash
dotnet new sln --name BooksInventory

mkdir src tests
dotnet new web -o src/BooksInventory.WebApi
dotnet new xunit -o tests/BooksInventory.WebApi.Tests

dotnet sln add src/BooksInventory.WebApi
dotnet sln add tests/BooksInventory.WebApi.Tests

dotnet add tests/BooksInventory.WebApi.Tests package FluentAssertions
dotnet add tests/BooksInventory.WebApi.Tests package Microsoft.AspNetCore.Mvc.Testing
```

---

## Understanding the Book Inventory API

The API provides two endpoints:

- **POST `/addBook`**: Accepts a JSON payload with `Title`, `Author`, and `ISBN`, stores it, and returns a unique `BookId`.
- **GET `/books/{id}`**: Fetches book details using `BookId`.

Here’s the `Program.cs` file for the API, using an in-memory dictionary instead of a database:

```csharp
using System.Collections.Concurrent;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var books = new ConcurrentDictionary<string, Book>();

app.MapPost("/addBook", (AddBookRequest request) =>
{
    var bookId = Guid.NewGuid().ToString();
    var book = new Book(request.Title, request.Author, request.ISBN);

    if (!books.TryAdd(bookId, book))
    {
        return Results.Problem("Failed to add book due to a concurrency issue.");
    }

    return Results.Ok(new AddBookResponse(bookId));
});

app.MapGet("/books/{id}", (string id) =>
{
    if (books.TryGetValue(id, out var book))
    {
        return Results.Ok(book);
    }
    return Results.NotFound(new { Message = "Book not found", BookId = id });
});

app.Run();

public record AddBookRequest(string Title, string Author, string ISBN);
public record AddBookResponse(string BookId);
public record Book(string Title, string Author, string ISBN);

// Explicitly define Program as partial for integration tests
public partial class Program { }
```

---

## Writing Integration Tests

Integration tests verify that all components work together as expected. We’ll use **xUnit**, **WebApplicationFactory**, and **FluentAssertions**.

### Test File: `BookInventoryTests.cs`

```csharp
using FluentAssertions;

using Microsoft.AspNetCore.Mvc.Testing;

namespace BooksInventory.WebApi.Tests;

public class BookInventoryTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public BookInventoryTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task AddBook_ReturnsBookId()
    {
        var request = new AddBookRequest("AI Engineering", "Chip Huyen", "1098166302");
        var content = request.GetHttpContent();

        var response = await _client.PostAsync("/addBook", content);

        response.EnsureSuccessStatusCode();
        var result = await response.DeserializeAsync<AddBookResponse>();
        result?.Should().NotBeNull();
        result!.BookId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetBook_ReturnsBookDetails()
    {
        var addRequest = new AddBookRequest("AI Engineering", "Chip Huyen", "1234567890");
        var addResponse = await _client.PostAsync("/addBook", addRequest.GetHttpContent());
        var bookId = (await addResponse.DeserializeAsync<AddBookResponse>())?.BookId;

        var getResponse = await _client.GetAsync($"/books/{bookId}");

        getResponse.EnsureSuccessStatusCode();
        var book = await getResponse.DeserializeAsync<Book>();
        book.Should().BeEquivalentTo(
            new Book(
                addRequest.Title,
                addRequest.Author,
                addRequest.ISBN));
    }
}
```

---

## Sweet and Useful: Extension Methods

Avoid clutter in your tests using extension methods for handling HTTP content:

```csharp
using System.Text;
using System.Text.Json;

public static class HttpContentExtensions
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static async Task<T?> DeserializeAsync<T>(this HttpResponseMessage response)
    {
        return JsonSerializer.Deserialize<T>(
            await response.Content.ReadAsStringAsync(),
            SerializerOptions);
    }

    public static HttpContent GetHttpContent<T>(this T obj) where T : class
    {
        return new StringContent(
            JsonSerializer.Serialize(obj),
            Encoding.UTF8, "application/json");
    }
}
```

---

## Manual Testing: Using REST Client and `.http` Files

Visual Studio Code’s **REST Client** extension simplifies manual testing. Define requests in `.http` files like this:

```http
POST {{baseUrl}}/addBook HTTP/1.1
Content-Type: application/json

{
    "Title": "The Pragmatic Programmer",
    "Author": "Andy Hunt and Dave Thomas",
    "ISBN": "9780135957059"
}

###

# Test GET /books/{id} (replace {id} with a valid BookId from the POST response)
GET {{baseUrl}}/books/{id} HTTP/1.1
Accept: application/json
```

---

## Conclusion

We’ve explored how to write and test a minimal web API in ASP.NET using integration tests and manual `.http` files. Key takeaways:

- Use `WebApplicationFactory` to create an in-memory test server.
- Leverage `FluentAssertions` for clean, readable assertions.
- Reduce boilerplate in tests with extension methods.
- Use VS Code’s REST Client for quick manual testing.

If you found this useful, check out the full source code on GitHub [here](https://github.com/dorinandreidragan/nbdotnet/tree/main/aspnet/BooksInventory).

Happy coding! 🚀
