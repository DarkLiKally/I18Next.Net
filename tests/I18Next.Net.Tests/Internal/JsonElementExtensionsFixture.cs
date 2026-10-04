using System.Text.Json;
using I18Next.Net.Internal;
using Shouldly;
using Xunit;

namespace I18Next.Net.Tests.Internal;

public class JsonElementExtensionsFixture
{
    [Fact]
    public void ToObject_LargeNumber_ShouldReturnDouble()
    {
        using var document = JsonDocument.Parse("{ \"value\": 1e300 }");

        document.RootElement.ToDictionary()["value"].ShouldBe(1e300);
    }

    [Fact]
    public void ToObject_Undefined_ShouldReturnNull()
    {
        default(JsonElement).ToObject().ShouldBeNull();
    }
}
