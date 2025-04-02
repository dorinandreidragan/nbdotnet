# Testing Minimal Web APIs with ASP.NET: A Developer's Guide

In this article, we’ll explore how to test a minimal web API using a Book Inventory API as an example. We’ll also touch on practical techniques, including the use of extension methods and manual testing with REST Client and `.http` files, to simplify the process and make it more enjoyable.

Let's dive right in!

---

## Setting Up The Stage

First we need to set up the stage, creating the solution, and the projects.

```bash
dotnet sln new --name BooksInventory

mkdir src tests
dotnet new web -o src/BooksInventory.WebApi
dotnet new xunit -o tests/BooksInventory.WebApi.Tests

dotnet sln add src/BooksInventory.WebApi
dotnet sln add tests/BooksInventory.WebApi.Tests

dotnet add tests/BooksInventory.WebApi.Tests package FluentAssertions
dotnet add tests/BooksInventory.WebApi.Tests package Microsoft.AspNetCore.Mvc.Testing
```

## Understanding the Book Inventory API

The Book Inventory API provides two endpoints:

- **POST `/addBook`**: Accepts a JSON payload with `Title`, `Author`, and `ISBN`, stores it, and returns a unique `BookId`.
- **GET `/books/{id}`**: Fetches the details of the book using the `BookId`.

Here’s how the **Program.cs** file for the Book Inventory API might look. Note that instead of a database, we use an in memory dictionary. This is just to avoid distractions and keep focus on the integration testing.

```csharp
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
```

---

## Writing Integration Tests

Integration tests ensure the API works as expected when all components (like endpoints, middleware, and dependencies) are combined. For our Book Inventory API, testing the POST and GET endpoints verifies that:

Using **xUnit**, **WebApplicationFactory**, and **FluentAssertions**, here’s how you can test the Book Inventory API:

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
        result.Should().NotBeNull();
        result!.BookId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetBook_ReturnsBookDetails()
    {
        var addRequest = new AddBookRequest("AI Engineering", "Chip Huyen", "1098166302");
        var addResponse = await _client.PostAsync("/addBook", addRequest.GetHttpContent());
        var bookId = (await addResponse.DeserializeAsync<AddBookResponse>())?.BookId;

        var getResponse = await _client.GetAsync($"/books/{bookId}");

        getResponse.EnsureSuccessStatusCode();
        var book = await getResponse.DeserializeAsync<Book>();
        book.Should().NotBeNull();
        book!.Title.Should().Be(addRequest.Title);
        book.Author.Should().Be(addRequest.Author);
        book.ISBN.Should().Be(addRequest.ISBN);
    }
}
```

---

## Sweet and Useful: Extension Methods

Simplify repetitive tasks in your tests using extension methods for handling HTTP content. Here’s an example:

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

I use these methods in my tests for streamlined serialization and deserialization of HTTP content, as you can see in the lines below:

```csharp
var request = new AddBookRequest("AI Engineering", "Chip Huyen", "1098166302");
var content = request.GetHttpContent();
```

or here:

```csharp
var book = await getResponse.DeserializeAsync<Book>();
```

This helps me to avoid a lot of clutter.

---

## Manual Testing: Using REST Client and `.http` Files

Visual Studio Code’s **REST Client** extension simplifies manual testing. Define requests in `.http` files like this:

### `.http` File Example:

```http
# Base URL
@baseUrl = http://localhost:5000

# Test POST /addBook
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

Execute these requests directly in VS Code using the REST Client extension, with responses displayed right in the editor.

## Conclusion

TODO:
