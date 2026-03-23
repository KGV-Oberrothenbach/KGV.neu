namespace KGV.Core.Models
{
    public sealed class RfidMediumOption
    {
        public RfidMediumOption(string key, string displayName)
        {
            Key = key;
            DisplayName = displayName;
        }

        public string Key { get; }
        public string DisplayName { get; }
    }
}
