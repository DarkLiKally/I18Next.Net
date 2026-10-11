using System;

using I18Next.Net.Logging;

using Shouldly;

using UnityEngine;

using Xunit;

using LogLevel = I18Next.Net.Logging.LogLevel;

namespace I18Next.Net.Unity.Tests
{
    [Collection(nameof(I18NextUnity))]
    public class UnityLoggerFixture : IDisposable
    {
        public UnityLoggerFixture()
        {
            UnityTestEnvironment.Reset();
        }

        public void Dispose()
        {
            UnityTestEnvironment.Reset();
        }

        [Theory]
        [InlineData("Missing {key} in {language}", "Missing title in de")]
        [InlineData("Missing {key}", "Missing title")]
        [InlineData("Escaped {{key}} {key}", "Escaped {key} title")]
        [InlineData("Unclosed {key", "Unclosed {key")]
        [InlineData("Too many {key} {language} {ns}", "Too many title de {ns}")]
        public void Format_ShouldReplacePlaceholdersInOrder(string message, string expected)
        {
            UnityLogger.Format(message, "title", "de").ShouldBe(expected);
        }

        [Theory]
        [InlineData(LogLevel.Information, "Log")]
        [InlineData(LogLevel.Warning, "Warning")]
        [InlineData(LogLevel.Error, "Error")]
        [InlineData(LogLevel.Critical, "Error")]
        public void Log_ShouldWriteToUnityConsole(LogLevel logLevel, string expected)
        {
            var logger = new UnityLogger { LogLevel = LogLevel.Trace };

            logger.Log(logLevel, "Missing {key}", "title");

            Debug.Messages.ShouldHaveSingleItem().ShouldBe((expected, (object)"Missing title"));
        }

        [Fact]
        public void Log_BelowLogLevel_ShouldNotWrite()
        {
            var logger = new UnityLogger();

            logger.LogInformation("Missing {key}", "title");

            Debug.Messages.ShouldBeEmpty();
        }

        [Fact]
        public void Log_Exception_ShouldLogException()
        {
            var logger = new UnityLogger();
            var exception = new InvalidOperationException();

            logger.Log(LogLevel.Error, exception, "Failed");

            Debug.Messages.Count.ShouldBe(2);
            Debug.Messages[1].ShouldBe(("Exception", (object)exception));
        }
    }
}
