namespace MiniErp.Domain.Entities;

public class Role
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;

    private Role() { }   // dibutuhkan EF Core

    public Role(int id, string name)
    {
        Id = id;
        Name = name;
    }
}