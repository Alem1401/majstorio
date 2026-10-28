namespace Majstorio.Services.Database;


public class City : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
