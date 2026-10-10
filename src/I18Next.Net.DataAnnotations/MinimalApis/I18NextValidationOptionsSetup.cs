using System.Linq;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Validation;

namespace I18Next.Net.DataAnnotations.MinimalApis;

internal sealed class I18NextValidationOptionsSetup(I18NextValidationLocalizer localizer) : IPostConfigureOptions<ValidationOptions>
{
    private readonly I18NextValidationLocalizer _localizer = localizer;

    public void PostConfigure(string name, ValidationOptions options)
    {
        if (options.Resolvers.Any(r => r is I18NextValidatableInfoResolver))
            return;

        options.Resolvers.Insert(0, new I18NextValidatableInfoResolver(options, _localizer));
    }
}
