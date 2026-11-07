namespace ITR
{
    /// <summary>Live statistics of the current run.</summary>
    public class RunStats
    {
        public float score;
        public float distance;
        public int coins;
        public int nearMisses;
        public int overtakes;
        public int combo;
        public int maxCombo;
        public int nitroUses;
        public float topSpeedKmh;
        public float time;
        public int sideHits;

        public int Score => (int)score;
    }
}
