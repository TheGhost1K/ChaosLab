namespace ChaosLab.Tests.Fixtures;

/// <summary>
/// Эталонные значения из литературы для проверок.
/// </summary>
public static class TestData
{
    // ─── Lyapunov spectra ────────────────────────────────────────

    public static class Lorenz
    {
        public const double Lambda1 = 0.9056;
        public const double Lambda2 = 0.0;
        public const double Lambda3 = -14.572;
        public const double Sum = -13.667;       // = −(σ + 1 + β)
        public const double D2 = 2.06;
        public const double Dky = 2.06;
    }

    public static class Henon
    {
        public const double Lambda1 = 0.41922;
        public const double Lambda2 = -1.62319;
        public static readonly double Sum = Math.Log(0.3);
        public const double D2 = 1.26;
    }

    public static class Rossler
    {
        public const double Lambda1 = 0.0714;
        public const double Sum = -5.9;           // = −(a + c)
        public const double D2 = 1.98;
    }

    public static class Logistic
    {
        public const double LambdaAtR4 = 0.693147;   // = log 2
    }

    public static class Tent
    {
        public const double LambdaAtMu2 = 0.693147;  // = log 2
    }

    public static class Chen
    {
        public const double Lambda1 = 2.027;
        public const double Sum = -39.0;             // = −(a + b + 1)
    }

    // ─── Reference D₂ values from literature ─────────────────────

    public static class Dimensions
    {
        public const double LorenzD2 = 2.06;
        public const double HenonD2 = 1.26;
        public const double RosslerD2 = 1.98;
    }
}
