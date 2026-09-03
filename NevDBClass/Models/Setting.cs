namespace NevDBClass.Models
{
    public class Setting
    {
        public int Id { get; set; }

        public string SettingKey { get; set; } = "";

        public string SettingValue { get; set; } = "";

        public int? IsEnabled { get; set; }

    }
}
