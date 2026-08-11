namespace FireDiscipline.AimStance
{
    public enum AimStanceMode
    {
        Standard = 0,   // Baseline vanilla (instant free switch)
        Rapid = 1,      // Close-range burst/hipfire, derived weapon warmup, suppression x1.5
        Sharpshot = 2,  // Long-range precision, distance exponent d*0.8, warmup x1.4, suppression reset
        Prone = 3       // Legacy stance ID kept for backward compatibility (Prone is now passive Dug-In)
    }
}
