using System.Text;

namespace VRB.Core
{
    /// <summary>
    /// A host's IPv4 address as it is typed on a keypad of digits and a dot. Keys that could never lead to a valid address
    /// are refused (a dot first or twice in a row, a fourth dot, a number over three digits or above 255), so what is on
    /// screen is always the start of something valid.
    /// </summary>
    public sealed class AddressEntry
    {
        public const int MaxLength = 15;

        readonly StringBuilder m_Text = new StringBuilder();

        public string Text => m_Text.ToString();

        public bool IsComplete => IsValid(Text);

        /// <summary>Adds a key. Returns false, leaving the text as it is, if the key does not fit.</summary>
        public bool Append(char key)
        {
            if (m_Text.Length >= MaxLength) return false;

            if (key == '.')
            {
                if (m_Text.Length == 0 || m_Text[m_Text.Length - 1] == '.' || DotCount(m_Text) >= 3) return false;
                m_Text.Append('.');
                return true;
            }

            if (key < '0' || key > '9') return false;

            var number = CurrentNumber(m_Text);
            if (number.Length >= 3 || int.Parse(number + key) > 255) return false;
            m_Text.Append(key);
            return true;
        }

        public void Backspace()
        {
            if (m_Text.Length > 0) m_Text.Length--;
        }

        public void Clear() => m_Text.Clear();

        /// <summary>Starts from some text, for example the first three numbers of this device's own address.</summary>
        public void Set(string text)
        {
            Clear();
            foreach (var key in text ?? string.Empty)
            {
                if (!Append(key))
                {
                    Clear();
                    return;
                }
            }
        }

        /// <summary>True for four numbers from 0 to 255 separated by dots.</summary>
        public static bool IsValid(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;

            var parts = text.Split('.');
            if (parts.Length != 4) return false;

            foreach (var part in parts)
            {
                if (part.Length == 0 || part.Length > 3) return false;
                foreach (var c in part)
                    if (c < '0' || c > '9') return false;
                if (int.Parse(part) > 255) return false;
            }

            return true;
        }

        /// <summary>
        /// The first three numbers and the dot after them ("192.168.0."), which devices on one network share, so only the
        /// last number has to be typed. Empty when the text is not an address.
        /// </summary>
        public static string SubnetPrefix(string address)
        {
            if (!IsValid(address)) return string.Empty;
            return address.Substring(0, address.LastIndexOf('.') + 1);
        }

        static int DotCount(StringBuilder text)
        {
            var count = 0;
            for (var i = 0; i < text.Length; i++)
                if (text[i] == '.') count++;
            return count;
        }

        // The digits typed since the last dot.
        static string CurrentNumber(StringBuilder text)
        {
            var start = text.Length;
            while (start > 0 && text[start - 1] != '.') start--;
            return text.ToString(start, text.Length - start);
        }
    }
}
