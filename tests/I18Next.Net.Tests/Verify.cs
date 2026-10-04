using System;
using System.Diagnostics;

using NSubstitute.Core.Arguments;

namespace I18Next.Net.Tests;

public static class Verify
{
    public static T That<T>(Action<T> action)
    {
        return ArgumentMatcher.Enqueue(new Matcher<T>(action));
    }

    private class Matcher<T>(Action<T> assertion) : IArgumentMatcher<T>
    {
        private readonly Action<T> _assertion = assertion;

        public bool IsSatisfiedBy(T argument)
        {
            try
            {
                _assertion(argument);

                return true;
            }
            catch (Exception exception)
            {
                Trace.WriteLine(exception.Message);

                return false;
            }
        }
    }
}
