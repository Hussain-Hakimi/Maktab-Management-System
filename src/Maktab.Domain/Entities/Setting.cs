namespace Maktab.Domain.Entities;

public sealed class Setting
{
    public int SettingId { get; set; }
    public required string Key { get; set; }
    public required string Value { get; set; }
}
