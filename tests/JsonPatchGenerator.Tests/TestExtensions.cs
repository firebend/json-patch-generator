using System.Linq;
using FluentAssertions;
using Microsoft.AspNetCore.JsonPatch;

namespace Firebend.JsonPatch.Tests;

public static class TestExtensions
{
    public static void ValuesShouldNotContainJson<T>(this JsonPatchDocument<T> diff)
        where T : class => diff.Operations
        .Where(x => x.value?.ToString()?.StartsWith("{") ?? false)
        .Should()
        .BeNullOrEmpty();
}
