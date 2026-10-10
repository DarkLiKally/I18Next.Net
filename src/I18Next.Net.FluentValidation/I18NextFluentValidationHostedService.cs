using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using FluentValidation;
using FluentValidation.Resources;

using Microsoft.Extensions.Hosting;

namespace I18Next.Net.FluentValidation;

internal sealed class I18NextFluentValidationHostedService(I18NextLanguageManager languageManager, I18NextDisplayNameResolver displayNameResolver)
    : IHostedService
{
    private readonly I18NextDisplayNameResolver _displayNameResolver = displayNameResolver;
    private readonly I18NextLanguageManager _languageManager = languageManager;

    private Func<Type, MemberInfo, LambdaExpression, string> _displayNameResolverFunc;
    private Func<Type, MemberInfo, LambdaExpression, string> _previousDisplayNameResolver;
    private ILanguageManager _previousLanguageManager;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _previousLanguageManager = ValidatorOptions.Global.LanguageManager;
        ValidatorOptions.Global.LanguageManager = _languageManager;

        if (_displayNameResolver.Options.TranslateDisplayNames)
        {
            var previousDisplayNameResolver = ValidatorOptions.Global.DisplayNameResolver;

            _previousDisplayNameResolver = previousDisplayNameResolver;
            _displayNameResolverFunc = (type, member, expression) =>
                _displayNameResolver.Resolve(type, member, expression) ?? previousDisplayNameResolver(type, member, expression);

            ValidatorOptions.Global.DisplayNameResolver = _displayNameResolverFunc;
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        if (ValidatorOptions.Global.LanguageManager == _languageManager)
            ValidatorOptions.Global.LanguageManager = _previousLanguageManager;

        if (_displayNameResolverFunc != null && ValidatorOptions.Global.DisplayNameResolver == _displayNameResolverFunc)
            ValidatorOptions.Global.DisplayNameResolver = _previousDisplayNameResolver;

        return Task.CompletedTask;
    }
}
