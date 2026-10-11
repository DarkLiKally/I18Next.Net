using System.Threading.Tasks;

using I18Next.Net.Gettext;

using Shouldly;

using Xunit;

namespace I18Next.Net.Tests.Backends;

public class GettextBackendFixture
{
    [Fact]
    public async Task LoadNamespaceAsync_MissingFile_ShouldReturnNull()
    {
        (await new GettextBackend("TestFiles").LoadNamespaceAsync("fr", "test")).ShouldBeNull();
        (await new GettextBackend().LoadNamespaceAsync("en", "test")).ShouldBeNull();
    }
}
