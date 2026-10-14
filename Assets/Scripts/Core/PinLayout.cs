using System;

namespace VRB.Core
{
    /// <summary>
    /// Standard ten-pin triangle. Positions are relative to the head pin:
    /// x = lateral offset (right is positive), z = distance down the lane.
    /// Pin 0 is the head pin, followed by rows of 2, 3 and 4 pins.
    /// </summary>
    public static class PinLayout
    {
        public const int PinCount = 10;
        public const int RowCount = 4;

        /// <summary>Distance between rows: neighbouring pins form equilateral triangles.</summary>
        public static readonly float RowSpacing = BowlingDimensions.PinSpacing * 0.8660254f;

        public static (float x, float z) GetPosition(int pinIndex)
        {
            if (pinIndex < 0 || pinIndex >= PinCount)
                throw new ArgumentOutOfRangeException(nameof(pinIndex), pinIndex, "Pin index must be 0..9.");

            var row = 0;
            var firstInRow = 0;
            while (pinIndex >= firstInRow + row + 1)
            {
                firstInRow += row + 1;
                row++;
            }

            var column = pinIndex - firstInRow;
            var x = (column - row * 0.5f) * BowlingDimensions.PinSpacing;
            var z = row * RowSpacing;
            return (x, z);
        }
    }
}
