using System;
using System.Collections;
using FluentAssertions;
using I18Next.Net.Formatters;
using NUnit.Framework;

namespace I18Next.Net.Tests.Formatters
{
    [TestFixture]
    public class MomentJsFormatterFixture
    {
        private MomentJsFormatter _formatter;

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            _formatter = new MomentJsFormatter();
        }

        public static IEnumerable FormatTestData
        {
            get
            {
                // @formatter:off
                yield return new TestCaseData("en-US", new DateTime(636524626794846031), "M Mo MM MMM MMMM", "1 1st 01 Jan January");
                yield return new TestCaseData("en-US", new DateTime(636611368294846031), "M Mo MM MMM MMMM", "5 5th 05 May May");
                yield return new TestCaseData("en-US", new DateTime(636697426794846031), "M Mo MM MMM MMMM", "8 8th 08 Aug August");
                yield return new TestCaseData("en-US", new DateTime(636740968794846031), "M Mo MM MMM MMMM", "10 10th 10 Oct October");
                yield return new TestCaseData("en-US", new DateTime(636740968794846031), "\\M \\Mo \\MM \\MMM \\MMMM", "M Mo MM MMM MMMM");
                
                yield return new TestCaseData("en-US", new DateTime(636524626794846031), "Q Qo", "1 1st");
                yield return new TestCaseData("en-US", new DateTime(636611368294846031), "Q Qo", "2 2nd");
                yield return new TestCaseData("en-US", new DateTime(636697426794846031), "Q Qo", "3 3rd");
                yield return new TestCaseData("en-US", new DateTime(636740968794846031), "Q Qo", "4 4th");
                yield return new TestCaseData("en-US", new DateTime(636740968794846031), "\\Q \\Qo", "Q Qo");
                
                yield return new TestCaseData("en-US", new DateTime(636524626794846031), "D Do DD DDD DDDo DDDD", "25 25th 25 25 25th 025");
                yield return new TestCaseData("en-US", new DateTime(636611368294846031), "D Do DD DDD DDDo DDDD", "5 5th 05 125 125th 125");
                yield return new TestCaseData("en-US", new DateTime(636697426794846031), "D Do DD DDD DDDo DDDD", "13 13th 13 225 225th 225");
                yield return new TestCaseData("en-US", new DateTime(636740968794846031), "D Do DD DDD DDDo DDDD", "2 2nd 02 275 275th 275");
                yield return new TestCaseData("en-US", new DateTime(636740968794846031), "\\D \\Do \\DD \\DDD \\DDDo \\DDDD", "D Do DD DDD DDDo DDDD");
                
                yield return new TestCaseData("en-US", new DateTime(636524626794846031), "d do dd ddd dddd", "4 4th Thu Thursday Thursday");
                yield return new TestCaseData("en-US", new DateTime(636611368294846031), "d do dd ddd dddd", "6 6th Sat Saturday Saturday");
                yield return new TestCaseData("en-US", new DateTime(636697426794846031), "d do dd ddd dddd", "1 1st Mon Monday Monday");
                yield return new TestCaseData("en-US", new DateTime(636740968794846031), "d do dd ddd dddd", "2 2nd Tue Tuesday Tuesday");
                yield return new TestCaseData("en-US", new DateTime(636740968794846031), "\\d \\do \\dd \\ddd \\dddd", "d do dd ddd dddd");
                
                yield return new TestCaseData("en-US", new DateTime(636524626794846031), "e E", "4 5");
                yield return new TestCaseData("en-US", new DateTime(636611368294846031), "e E", "6 7");
                yield return new TestCaseData("en-US", new DateTime(636697426794846031), "e E", "1 2");
                yield return new TestCaseData("en-US", new DateTime(636740968794846031), "e E", "2 3");
                yield return new TestCaseData("en-US", new DateTime(636740968794846031), "\\e \\E", "e E");
                
                yield return new TestCaseData("en-US", new DateTime(636524626794846031), "w wo ww W Wo WW", "4 4th 04 4 4th 04");
                yield return new TestCaseData("en-US", new DateTime(636611368294846031), "w wo ww W Wo WW", "18 18th 18 18 18th 18");
                yield return new TestCaseData("en-US", new DateTime(636697426794846031), "w wo ww W Wo WW", "33 33rd 33 33 33rd 33");
                yield return new TestCaseData("en-US", new DateTime(636740968794846031), "w wo ww W Wo WW", "40 40th 40 40 40th 40");
                yield return new TestCaseData("en-US", new DateTime(636740968794846031), "\\w \\wo \\ww \\W \\Wo \\WW", "w wo ww W Wo WW");
                
                yield return new TestCaseData("en-US", new DateTime(636524626794846031), "YY YYYY Y", "18 2018 Y");
                yield return new TestCaseData("en-US", new DateTime(633585298794846031), "YY YYYY Y", "08 2008 Y");
                yield return new TestCaseData("en-US", new DateTime(633585298794846031), "\\YY \\YYYY \\Y", "YY YYYY Y");
                
                yield return new TestCaseData("en-US", new DateTime(636524626794846031), "gg gggg GG GGGG", "gg GG gg GGGG");
                yield return new TestCaseData("en-US", new DateTime(636740968794846031), "\\gg \\gggg \\GG \\GGGG", "gg gggg GG GGGG");
                
                yield return new TestCaseData("en-US", new DateTime(636524626794846031), "A a", "AM am");
                yield return new TestCaseData("en-US", new DateTime(636611368294846031), "A a", "PM pm");
                yield return new TestCaseData("en-US", new DateTime(636740968794846031), "\\A \\a", "A a");
                
                yield return new TestCaseData("en-US", new DateTime(636524626794846031), "H HH h hh k kk", "7 07 7 07 8 08");
                yield return new TestCaseData("en-US", new DateTime(636611368294846031), "H HH h hh k kk", "17 17 5 05 18 18");
                yield return new TestCaseData("en-US", new DateTime(636740968794846031), "\\H \\HH \\h \\hh \\k \\kk", "H HH h hh k kk");
                
                yield return new TestCaseData("en-US", new DateTime(636524626794846031), "m mm", "37 37");
                yield return new TestCaseData("en-US", new DateTime(636611368294846031), "m mm", "7 07");
                yield return new TestCaseData("en-US", new DateTime(636740968794846031), "\\m \\mm", "m mm");
                
                yield return new TestCaseData("en-US", new DateTime(636524626794846031), "s ss", "59 59");
                yield return new TestCaseData("en-US", new DateTime(636611368294846031), "s ss", "9 09");
                yield return new TestCaseData("en-US", new DateTime(636740968794846031), "\\s \\ss", "s ss");
                
                yield return new TestCaseData("en-US", new DateTime(636524626794846031), "S SS SSS SSSS SSSSS SSSSSS SSSSSSS SSSSSSSS SSSSSSSSS", "4 48 484 4846 48460 484603 4846031 484603100 4846031000");
                yield return new TestCaseData("en-US", new DateTime(636740968794846031), "\\S \\SS \\SSS \\SSSS \\SSSSS \\SSSSSS \\SSSSSSS \\SSSSSSSS \\SSSSSSSSS", "S SS SSS SSSS SSSSS SSSSSS SSSSSSS SSSSSSSS SSSSSSSSS");
                
                yield return new TestCaseData("en-US", new DateTime(636524626794846031), "z zz Z ZZ", "A A +01:00 +0100");
                yield return new TestCaseData("en-US", new DateTime(636740968794846031), "\\z \\zz \\Z \\ZZ", "z zz Z ZZ");
                
                yield return new TestCaseData("en-US", new DateTime(636524626794846031), "X x", "1516862279 1516862279484");
                yield return new TestCaseData("en-US", new DateTime(636740968794846031), "\\X \\x", "X x");
                
                yield return new TestCaseData("en-US", new DateTime(636524626794846031), "[HH:mm:ss] HH:mm:ss \\HH:\\mm:\\ss", "HH:mm:ss 07:37:59 HH:mm:ss");
                yield return new TestCaseData("en-US", new DateTime(636524626794846031), "[HH:\\mm:ss]", "HH:\\mm:ss");
                yield return new TestCaseData("en-US", new DateTime(636524626794846031), "\\[HH:mm:ss]", "[07:37:59]");
                yield return new TestCaseData("en-US", new DateTime(636524626794846031), "%\\hh", "%hh");
                yield return new TestCaseData("en-US", new DateTime(636524626794846031), "%hh", "%07");
                
                yield return new TestCaseData("en-US", new DateTime(636524626794846031), "LT LTS", "7:37 AM 7:37:59 AM");
                yield return new TestCaseData("en-US", new DateTime(636611368294846031), "LT LTS", "5:07 PM 5:07:09 PM");
                yield return new TestCaseData("en-US", new DateTime(636740968794846031), "\\LT \\LTS", "LT LTS");
                yield return new TestCaseData("en-US", new DateTime(636740968794846031), "[LT] \\[LT]", "LT [5:07 PM]");
                yield return new TestCaseData("en-US", new DateTime(636740968794846031), "LTS M MM MMM MMMM HH:mm:ss", "5:07:59 PM 10 10 Oct October 17:07:59");
                yield return new TestCaseData("en-US", new DateTime(636740968794846031), "L LL LLL LLLL", "10/2/2018 Tuesday, October 2, 2018 10/2/2018 5:07:59 PM Tuesday, October 2, 2018 5:07:59 PM");
                yield return new TestCaseData("en-US", new DateTime(636740968794846031), "l ll lll llll", "10/2/2018 Tuesday, October 2, 2018 10/2/2018 5:07 PM Tuesday, October 2, 2018 5:07 PM");
                yield return new TestCaseData("en-US", new DateTime(636740968794846031), "MM/DD/YYYY LLLL [hh:mm:ss] hh:mm A L", "10/02/2018 Tuesday, October 2, 2018 5:07:59 PM hh:mm:ss 05:07 PM 10/2/2018");
                yield return new TestCaseData("en-US", new DateTime(636740968794846031), "\\LLL LLLL [mm:ss] hh:mm [L]", "LLL Tuesday, October 2, 2018 5:07:59 PM mm:ss 05:07 L");
                yield return new TestCaseData("en-US", new DateTime(636524626794846031), "[HH:mm:ss] LLLL \\HH:\\mm:\\ss", "HH:mm:ss Thursday, January 25, 2018 7:37:59 AM HH:mm:ss");
                // @formatter:on
            }
        }

        [Test]
        public void CanFormat_SomeInvalidValues_ShouldReturnFalse()
        {
            _formatter.CanFormat("Test", "dd.MM.yyyy", "de-DE").Should().BeFalse();
            _formatter.CanFormat(123, "dd.MM.yyyy", "de-DE").Should().BeFalse();
            _formatter.CanFormat(123.45, "dd.MM.yyyy", "de-DE").Should().BeFalse();
            _formatter.CanFormat(123L, "dd.MM.yyyy", "de-DE").Should().BeFalse();
            _formatter.CanFormat(123.45m, "dd.MM.yyyy", "de-DE").Should().BeFalse();
            _formatter.CanFormat(false, "dd.MM.yyyy", "de-DE").Should().BeFalse();
            _formatter.CanFormat('a', "dd.MM.yyyy", "de-DE").Should().BeFalse();
            _formatter.CanFormat(new object(), "dd.MM.yyyy", "de-DE").Should().BeFalse();
        }

        [Test]
        public void CanFormat_ValidValues_ShouldReturnTrue()
        {
            _formatter.CanFormat(DateTime.Now, "dd.MM.yyyy", "de-DE").Should().BeTrue();
            _formatter.CanFormat(DateTimeOffset.Now, "dd.MM.yyyy", "de-DE").Should().BeTrue();
        }

        [Test]
        [TestCaseSource(nameof(FormatTestData))]
        public void Format(string language, DateTime value, string format, string expected)
        {
            _formatter.Format(value, format, language).Should().Be(expected);
            _formatter.Format(new DateTimeOffset(value), format, language).Should().Be(expected);
        }
    }
}
