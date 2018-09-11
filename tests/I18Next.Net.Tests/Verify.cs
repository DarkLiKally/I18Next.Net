using System;
using System.Diagnostics;
using System.Linq;
using FluentAssertions.Execution;
using NSubstitute.Core;
using NSubstitute.Core.Arguments;

namespace I18Next.Net.Tests
{
    public static class Verify
    {
        private static readonly ArgumentSpecificationQueue Queue;

        static Verify()
        {
            Queue = new ArgumentSpecificationQueue(SubstitutionContext.Current);
        }

        public static T That<T>(Action<T> action)
        {
            return Queue.EnqueueSpecFor<T>(new Matcher<T>(action));
        }

        private class Matcher<T> : IArgumentMatcher
        {
            private readonly Action<T> _assertion;

            public Matcher(Action<T> assertion)
            {
                _assertion = assertion;
            }

            public bool IsSatisfiedBy(object argument)
            {
                using (var scope = new AssertionScope())
                {
                    _assertion((T) argument);

                    var failures = scope.Discard();

                    foreach (var x in failures)
                        Trace.WriteLine(x);

                    var hasFailures = failures.Any();

                    return hasFailures == false;
                }
            }
        }
    }
}
