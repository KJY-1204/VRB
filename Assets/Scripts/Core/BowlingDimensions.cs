namespace VRB.Core
{
    /// <summary>Real-world bowling equipment and lane dimensions in meters and kilograms.</summary>
    public static class BowlingDimensions
    {
        // Pin
        public const float PinHeight = 0.381f;        // 15 in
        public const float PinMaxDiameter = 0.121f;   // 4.766 in
        public const float PinMass = 1.56f;           // ~3 lb 7 oz
        public const float PinCenterOfMassHeight = 0.146f; // 5.75 in above the base: low, which is why pins stand at all
        public const float PinSpacing = 0.3048f;      // 12 in between neighbouring pin centers

        // Ball
        public const float BallDiameter = 0.218f;     // 8.59 in
        public const float BallMass = 6.8f;           // 15 lb, in the usual 14-16 lb range

        // Lane
        public const float LaneLength = 18.288f;      // foul line to head pin, 60 ft
        public const float LaneWidth = 1.0414f;       // 41.5 in
        public const float GutterWidth = 0.235f;      // 9.25 in
        public const float ApproachLength = 4.572f;   // 15 ft behind the foul line
        public const float PitLength = 0.9f;
    }
}
