using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace KGV.Core.Models;

[Table("app_setting")]
public sealed class AppSettingRecord : BaseModel
{
    [PrimaryKey("setting_key", false)]
    [Column("setting_key")]
    public string SettingKey { get; set; } = string.Empty;

    [Column("bool_value")]
    public bool BoolValue { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
