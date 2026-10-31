using BudgetCivil.Application.Common.Auditing;
using BudgetCivil.Application.Users.Administration.Repositories;
using BudgetCivil.Application.Users.Administration.UseCases;
using BudgetCivil.Tests.Unit.TestData;
using FluentAssertions;
using Moq;

namespace BudgetCivil.Tests.Unit.Application;

public sealed class EnableUserUseCaseTests
{
    private readonly Mock<IUserAdministrationRepository> _repository = new();
    private readonly Mock<ISecurityAuditService> _securityAuditService = new();

    [Fact]
    public async Task Habilitar_limpia_el_bloqueo_y_registra_el_evento()
    {
        var actor = TestUsers.WithRole(TestUsers.Administrator);
        var target = TestUsers.WithRole(TestUsers.Estimator, accountEnabled: false, accountLocked: true);
        target.FailedLoginAttempts = 5;

        _repository
            .Setup(repository => repository.GetActorByIdAsync(actor.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(actor);
        _repository
            .Setup(repository => repository.GetUserForManagementAsync(target.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(target);
        _repository
            .Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _securityAuditService
            .Setup(service => service.WriteAsync(It.IsAny<SecurityAuditWriteRequest>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var response = await new EnableUserUseCase(_repository.Object, _securityAuditService.Object)
            .ExecuteAsync(actor.Id, target.Id);

        response.AccountEnabled.Should().BeTrue();
        target.AccountEnabled.Should().BeTrue();
        target.AccountLocked.Should().BeFalse();
        target.FailedLoginAttempts.Should().Be(0);

        _securityAuditService.Verify(
            service => service.WriteAsync(
                It.Is<SecurityAuditWriteRequest>(request =>
                    request.PermissionKey == "USERS_DEACTIVATE" &&
                    request.ReasonCode == "USER_ENABLED" &&
                    request.TargetId == target.Id),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
