using System.ComponentModel;
// using System.ComponentModel.DataAnnotations;
using System.Formats.Tar;
using System.Security;
using FluentValidation;
using MiniErp.Application.Common.Exceptions;
using MiniErp.Application.Common.Interface;
using MiniErp.Application.Feature.Users.SetUserRoles;
using MiniErp.Domain.Constants;
using MiniErp.Domain.Entities;
using NSubstitute;

namespace MiniErp.Application.UnitTests.Features.Users;

public class SetUserRolesCommandHandlerTests
{
    private static readonly Role Admin = new(1, RoleNames.Admin);
    private static readonly Role Warehouse = new(3, RoleNames.Warehouse);
    private static readonly Role Sales = new(4, RoleNames.Sales);

    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IRoleRepository _roles = Substitute.For<IRoleRepository>();
    private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly SetUserRolesCommandHandler _sut;

    private readonly User _target = User.Create("staff@b.com", "hash", "Staff");

    public SetUserRolesCommandHandlerTests()
    {
        _roles.GetAllAsync(Arg.Any<CancellationToken>())
              .Returns(new List<Role> { Admin, Warehouse, Sales });
        
        _users.GetByIdForUpdateAsync(_target.Id, Arg.Any<CancellationToken>()).Returns(_target);

        _currentUser.UserId.Returns(Guid.NewGuid());

        _sut = new SetUserRolesCommandHandler(_users, _roles, _currentUser, _unitOfWork);
    }

    private static int[] RoleIdsOf(User user) =>
        user.UserRoles.Select(ur => ur.RoleId).OrderBy(id => id).ToArray();
    
    [Fact]
    public async Task Handle_ValidRoles_ReplacesRolesAndSaves()
    {
        _target.AssignRole(Warehouse.Id);
        await _sut.Handle(
            new SetUserRolesCommand(_target.Id, new[] { "Sales" }),
            CancellationToken.None);
        
        Assert.Equal(new [] { Sales.Id }, RoleIdsOf(_target));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NamesWithDifferentCaseAndDuplicates_AreNormalized()
    {
        await _sut.Handle(
            new SetUserRolesCommand(_target.Id, new[] { "sales", "SALES", " Sales " }),
            CancellationToken.None);
        
        Assert.Equal(new[] { Sales.Id }, RoleIdsOf(_target));
    }

    [Fact]
    public async Task Handle_UserNotFound_ThrowsNotFound()
    {
        var missingId = Guid.NewGuid();
        _users.GetByIdForUpdateAsync(missingId, Arg.Any<CancellationToken>()).Returns((User?)null);

        await Assert.ThrowsAsync<NotFoundException>(
            () => _sut.Handle(new SetUserRolesCommand(missingId, new[] { "Sales" }), CancellationToken.None));

        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownRole_ThrowsValidationAndChangesNothing()
    {
        _target.AssignRole(Warehouse.Id);

        await Assert.ThrowsAsync<ValidationException>(
            () => _sut.Handle(new SetUserRolesCommand(_target.Id, new[] { "Sales", "Bos" }), CancellationToken.None));
        
        Assert.Equal(new[] { Warehouse.Id }, RoleIdsOf(_target));
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AdminRemovesOwnAdminRole_ThrowsConflict()
    {
        _target.AssignRole(Admin.Id);
        _currentUser.UserId.Returns(_target.Id);

        await Assert.ThrowsAsync<ConflictException>(
            () => _sut.Handle(new SetUserRolesCommand(_target.Id, new[] { "Warehouse" }), CancellationToken.None));

        Assert.Equal(new[] { Admin.Id }, RoleIdsOf(_target));
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AdminKeepOwnAdminRole_Succeeds()
    {
        _target.AssignRole(Admin.Id);
        _currentUser.UserId.Returns(_target.Id);

        await _sut.Handle(
            new SetUserRolesCommand(_target.Id, new[] { "Admin", "Sales" }),
            CancellationToken.None);
        
        Assert.Equal(new[] { Admin.Id, Sales.Id }, RoleIdsOf(_target));
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AdminDemotesAnotherAdmin_Succeeds()
    {
        _target.AssignRole(Admin.Id);

        await _sut.Handle(
            new SetUserRolesCommand(_target.Id, new[] { "Warehouse" }),
            CancellationToken.None);
        
        Assert.Equal(new[] { Warehouse.Id }, RoleIdsOf(_target));
    }

}