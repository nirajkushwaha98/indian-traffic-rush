namespace ITR
{
    /// <summary>Global constants shared by every system.</summary>
    public static class GameConstants
    {
        public const string GameTitle = "INDIAN TRAFFIC RUSH";
        public const string MenuScene = "MainMenu";
        public const string GameScene = "Game";

        public const int LaneCount = 3;
        public const float LaneWidth = 3.6f;
        public const float RoadHalfWidth = LaneWidth * LaneCount * 0.5f;   // 5.4 m
        public const float SegmentLength = 40f;

        public const float MsToKmh = 3.6f;
        public const float KmhToMs = 1f / 3.6f;

        /// <summary>World X of the centre of a lane (0 = left, 2 = right).</summary>
        public static float LaneX(int lane) => (lane - (LaneCount - 1) * 0.5f) * LaneWidth;

        public static int ClampLane(int lane) => lane < 0 ? 0 : (lane >= LaneCount ? LaneCount - 1 : lane);

        /// <summary>Nearest lane index for a world X position.</summary>
        public static int NearestLane(float x)
        {
            int lane = UnityEngine.Mathf.RoundToInt(x / LaneWidth + (LaneCount - 1) * 0.5f);
            return ClampLane(lane);
        }
    }
}
