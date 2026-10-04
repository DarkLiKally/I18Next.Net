using System.Text.Json;
using FluentAssertions;
using I18Next.Net.Internal;
using NUnit.Framework;

namespace I18Next.Net.Tests.Internal;

[TestFixture]
public class JsonElementExtensionsFixture
{
    [Test]
    public void ToObject_LargeNumber_ShouldReturnDouble()
    {
        using var document = JsonDocument.Parse("{ \"value\": 1e300 }");

        document.RootElement.ToDictionary()["value"].Should().Be(1e300);
    }

    [Test]
    public void ToObject_Undefined_ShouldReturnNull()
    {
        default(JsonElement).ToObject().Should().BeNull();
    }
}
