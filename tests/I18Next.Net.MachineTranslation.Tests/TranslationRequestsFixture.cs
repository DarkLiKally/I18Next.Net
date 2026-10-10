using System.Linq;

using I18Next.Net.MachineTranslation.Internal;

using Shouldly;

using Xunit;

namespace I18Next.Net.MachineTranslation.Tests;

public class TranslationRequestsFixture
{
    [Theory]
    [InlineData(new[] { 1, 2, 3 }, 2, 100, new[] { 2, 1 })]
    [InlineData(new[] { 5, 5, 5, 5 }, 10, 10, new[] { 2, 2 })]
    [InlineData(new[] { 20, 1 }, 10, 10, new[] { 1, 1 })]
    [InlineData(new[] { 1, 20, 1 }, 10, 10, new[] { 1, 1, 1 })]
    [InlineData(new int[0], 10, 10, new int[0])]
    public void Split_ShouldRespectCountAndLength(int[] lengths, int maxCount, int maxLength, int[] expectedCounts)
    {
        var texts = lengths.Select(l => new string('a', l)).ToList();

        var batches = TranslationRequests.Split(texts, maxCount, maxLength).ToList();

        batches.Select(b => b.Count).ShouldBe(expectedCounts);
        batches.Sum(b => b.Count).ShouldBe(texts.Count);

        for (var i = 1; i < batches.Count; i++)
            batches[i].Start.ShouldBe(batches[i - 1].Start + batches[i - 1].Count);
    }
}
