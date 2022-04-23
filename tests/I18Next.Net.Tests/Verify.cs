using System;
using System.Diagnostics;
using System.Linq;
using FluentAssertions.Execution;
using NSubstitute.Core.Arguments;

namespace I18Next.Net.Tests;

public static class Verify
{
    public static T That<T>(Action<T> action)
    {
        return ArgumentMatcher.Enqueue(new Matcher<T>(action));
    }

    private class Matcher<T> : IArgumentMatcher<T>
    {
        private readonly Action<T> _assertion;

        public Matcher(Action<T> assertion)
        {
            _assertion = assertion;
        }

        public bool IsSatisfiedBy(T argument)
        {
            using (var scope = new AssertionScope())
            {
                _assertion(argument);

                var failures = scope.Discard();

                foreach (var x in failures)
                    Trace.WriteLine(x);

                var hasFailures = failures.Any();

                return hasFailures == false;
            }
        }
    }
}