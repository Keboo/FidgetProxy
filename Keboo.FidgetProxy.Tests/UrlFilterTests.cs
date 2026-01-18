using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace Keboo.FidgetProxy.Tests;

public class UrlFilterTests
{
    [Test]
    public async Task UrlFilter_ExactMatch()
    {
        var filter = new UrlFilter("https://example.com/api");
        
        await Assert.That(filter.IsMatch("https://example.com/api")).IsTrue();
        await Assert.That(filter.IsMatch("https://example.com/other")).IsFalse();
    }

    [Test]
    public async Task UrlFilter_WildcardStar()
    {
        var filter = new UrlFilter("*.example.com");
        
        await Assert.That(filter.IsMatch("api.example.com")).IsTrue();
        await Assert.That(filter.IsMatch("www.example.com")).IsTrue();
        await Assert.That(filter.IsMatch("sub.api.example.com")).IsTrue();
        await Assert.That(filter.IsMatch("other.com")).IsFalse();
    }

    [Test]
    public async Task UrlFilter_WildcardInPath()
    {
        var filter = new UrlFilter("*/api/*");
        
        await Assert.That(filter.IsMatch("https://example.com/api/users")).IsTrue();
        await Assert.That(filter.IsMatch("https://example.com/api/products")).IsTrue();
        await Assert.That(filter.IsMatch("https://example.com/other/users")).IsFalse();
    }

    [Test]
    public async Task UrlFilter_QuestionMark()
    {
        var filter = new UrlFilter("https://example.com/user?");
        
        await Assert.That(filter.IsMatch("https://example.com/user1")).IsTrue();
        await Assert.That(filter.IsMatch("https://example.com/userA")).IsTrue();
        await Assert.That(filter.IsMatch("https://example.com/user12")).IsFalse();
    }

    [Test]
    public async Task UrlFilter_CaseInsensitive()
    {
        var filter = new UrlFilter("https://EXAMPLE.com/*");
        
        await Assert.That(filter.IsMatch("https://example.com/api")).IsTrue();
        await Assert.That(filter.IsMatch("HTTPS://EXAMPLE.COM/API")).IsTrue();
    }
}

public class UrlFilterManagerTests
{
    [Test]
    public async Task AddFilter_AddsFilter()
    {
        var manager = new UrlFilterManager();
        
        var added = manager.AddFilter("*.example.com");
        
        await Assert.That(added).IsTrue();
        await Assert.That(manager.Count).IsEqualTo(1);
    }

    [Test]
    public async Task AddFilter_DuplicateReturnsFalse()
    {
        var manager = new UrlFilterManager();
        
        manager.AddFilter("*.example.com");
        var added = manager.AddFilter("*.example.com");
        
        await Assert.That(added).IsFalse();
        await Assert.That(manager.Count).IsEqualTo(1);
    }

    [Test]
    public async Task RemoveFilter_RemovesFilter()
    {
        var manager = new UrlFilterManager();
        manager.AddFilter("*.example.com");
        
        var removed = manager.RemoveFilter("*.example.com");
        
        await Assert.That(removed).IsTrue();
        await Assert.That(manager.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ShouldFilter_MatchesPattern()
    {
        var manager = new UrlFilterManager();
        manager.AddFilter("*.example.com");
        manager.AddFilter("*/api/*");
        
        await Assert.That(manager.ShouldFilter("https://api.example.com")).IsTrue();
        await Assert.That(manager.ShouldFilter("https://test.com/api/users")).IsTrue();
        await Assert.That(manager.ShouldFilter("https://other.com/users")).IsFalse();
    }

    [Test]
    public async Task AddFilters_AddsMultiple()
    {
        var manager = new UrlFilterManager();
        var patterns = new[] { "*.example.com", "*/api/*", "https://test.com/*" };
        
        manager.AddFilters(patterns);
        
        await Assert.That(manager.Count).IsEqualTo(3);
        await Assert.That(manager.GetFilters().Count).IsEqualTo(3);
    }

    [Test]
    public async Task ClearFilters_RemovesAll()
    {
        var manager = new UrlFilterManager();
        manager.AddFilter("*.example.com");
        manager.AddFilter("*/api/*");
        
        manager.ClearFilters();
        
        await Assert.That(manager.Count).IsEqualTo(0);
    }

    [Test]
    public async Task GetFilters_ReturnsAllPatterns()
    {
        var manager = new UrlFilterManager();
        manager.AddFilter("*.example.com");
        manager.AddFilter("*/api/*");
        
        var filters = manager.GetFilters();
        
        await Assert.That(filters.Count).IsEqualTo(2);
        await Assert.That(filters).Contains("*.example.com");
        await Assert.That(filters).Contains("*/api/*");
    }
}
