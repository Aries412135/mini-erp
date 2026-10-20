using MiniErp.Domain.Entities;

namespace MiniErp.Domain.UnitTests.Entities;

public class UserTests
{
    private static User NewUser() => User.Create("a@b.com", "hash", "Test User");

    private static int[] RoleIdsOf(User user) =>
        user.UserRoles.Select(ur => ur.RoleId).OrderBy(id => id).ToArray();

    [Fact]
    public void ReplaceRoles_ReplacesTheWholeSet()
    {
        var user = NewUser();
        user.AssignRole(1);
        user.AssignRole(3);

        user.ReplaceRoles(new[] { 3, 4});

        Assert.Equal(new[] { 3, 4 }, RoleIdsOf(user));
    }

    [Fact]
    public void ReplaceRoles_SameSet_ChangeNothing()
    {
        var user = NewUser();
        user.AssignRole(1);
        user.AssignRole(3);

        user.ReplaceRoles(new[] { 3, 1 });

        Assert.Equal(new[] { 1, 3 }, RoleIdsOf(user));
    }

    [Fact]
    public void ReplaceRoles_DuplicateIds_AreCollapse()
    {
        var user = NewUser();
        user.ReplaceRoles(new[] { 4, 4, 4 });
        Assert.Equal(new[] { 4 }, RoleIdsOf(user));
    }

    [Fact]
    public void ReplaceRoles_EmptySet_RemovesAll()
    {
        var user = NewUser();
        user.AssignRole(2);

        user.ReplaceRoles(Array.Empty<int>());

        Assert.Empty(user.UserRoles);
    }
}