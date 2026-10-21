using NUnit.Framework;
using VRB.Core;

namespace VRB.Tests
{
    /// <summary>Typing the host's address on a keypad of digits and a dot.</summary>
    public class AddressEntryTests
    {
        static AddressEntry Typed(string keys)
        {
            var entry = new AddressEntry();
            foreach (var key in keys) entry.Append(key);
            return entry;
        }

        [Test]
        public void ItStartsEmpty()
        {
            var entry = new AddressEntry();

            Assert.That(entry.Text, Is.Empty);
            Assert.That(entry.IsComplete, Is.False);
        }

        [Test]
        public void DigitsAndDots_BuildTheAddress()
        {
            var entry = Typed("192.168.0.12");

            Assert.That(entry.Text, Is.EqualTo("192.168.0.12"));
            Assert.That(entry.IsComplete, Is.True);
        }

        [Test]
        public void AnythingButADigitOrADot_IsRefused()
        {
            var entry = new AddressEntry();

            Assert.That(entry.Append('a'), Is.False);
            Assert.That(entry.Append(' '), Is.False);
            Assert.That(entry.Append('-'), Is.False);
            Assert.That(entry.Text, Is.Empty);
        }

        [Test]
        public void ADotCannotComeFirst_OrFollowADot()
        {
            var entry = new AddressEntry();
            Assert.That(entry.Append('.'), Is.False, "Not at the start.");

            entry.Append('1');
            Assert.That(entry.Append('.'), Is.True);
            Assert.That(entry.Append('.'), Is.False, "Not twice in a row.");
            Assert.That(entry.Text, Is.EqualTo("1."));
        }

        [Test]
        public void AnAddressHasThreeDotsAtMost()
        {
            var entry = Typed("1.2.3.4");

            Assert.That(entry.Append('.'), Is.False);
            Assert.That(entry.Text, Is.EqualTo("1.2.3.4"));
        }

        [Test]
        public void ANumberHasThreeDigitsAtMost()
        {
            var entry = Typed("192");

            Assert.That(entry.Append('1'), Is.False);
            Assert.That(entry.Text, Is.EqualTo("192"));
        }

        [Test]
        public void ANumberCannotPass255()
        {
            var entry = Typed("25");

            Assert.That(entry.Append('6'), Is.False);
            Assert.That(entry.Append('5'), Is.True);
            Assert.That(entry.Text, Is.EqualTo("255"));
        }

        [Test]
        public void Backspace_RemovesTheLastKey_AndIsHarmlessWhenEmpty()
        {
            var entry = Typed("10.0");

            entry.Backspace();
            Assert.That(entry.Text, Is.EqualTo("10."));
            entry.Backspace();
            entry.Backspace();
            entry.Backspace();
            Assert.That(entry.Text, Is.Empty);
            Assert.DoesNotThrow(() => entry.Backspace());
        }

        [Test]
        public void Clear_EmptiesIt()
        {
            var entry = Typed("192.168");

            entry.Clear();

            Assert.That(entry.Text, Is.Empty);
        }

        [TestCase("192.168.0.12", true)]
        [TestCase("10.0.0.1", true)]
        [TestCase("0.0.0.0", true)]
        [TestCase("255.255.255.255", true)]
        [TestCase("192.168.0", false)]
        [TestCase("192.168.0.", false)]
        [TestCase("192.168.0.256", false)]
        [TestCase("192.168.0.1.5", false)]
        [TestCase("", false)]
        [TestCase("a.b.c.d", false)]
        [TestCase("192..0.1", false)]
        public void IsValid_RecognisesAnIPv4Address(string text, bool expected)
        {
            Assert.That(AddressEntry.IsValid(text), Is.EqualTo(expected));
        }

        [Test]
        public void SetStartsFromAGivenText_SoOnlyTheEndNeedsTyping()
        {
            var entry = new AddressEntry();

            entry.Set("192.168.0.");
            entry.Append('2');
            entry.Append('3');

            Assert.That(entry.Text, Is.EqualTo("192.168.0.23"));
            Assert.That(entry.IsComplete, Is.True);
        }

        [Test]
        public void Set_IgnoresTextThatIsNotAnAddressPrefix()
        {
            var entry = new AddressEntry();

            entry.Set("hello");

            Assert.That(entry.Text, Is.Empty);
        }

        [TestCase("192.168.0.12", "192.168.0.")]
        [TestCase("10.20.30.40", "10.20.30.")]
        [TestCase("127.0.0.1", "127.0.0.")]
        [TestCase("nonsense", "")]
        [TestCase("", "")]
        public void SubnetPrefix_IsTheFirstThreeNumbers(string address, string expected)
        {
            Assert.That(AddressEntry.SubnetPrefix(address), Is.EqualTo(expected));
        }
    }
}
