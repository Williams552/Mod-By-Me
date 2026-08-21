namespace RimwardExiles.Core
{
    public struct LoyaltyFactor
    {
        public string label;
        public float delta;
        public string category;

        public LoyaltyFactor(string label, float delta, string category = "")
        {
            this.label = label;
            this.delta = delta;
            this.category = category;
        }
    }
}
