using System.Text.Json;
using BudgetCivil.Application.Users.Auth.Dtos;
using BudgetCivil.Application.Users.Auth.Validators;
using FluentAssertions;

namespace BudgetCivil.Tests.Unit.Application;

public sealed class RegisterUserRequestValidatorTests
{
    private readonly RegisterUserRequestValidator _validator = new();

    [Fact]
    public void Acepta_un_autorregistro_con_identidad_completa()
    {
        var result = _validator.Validate(CreateRequest());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Acepta_que_el_segundo_nombre_y_el_segundo_apellido_no_se_envien()
    {
        // INT-06: son opcionales.
        var result = _validator.Validate(CreateRequest(middleName: null, secondLastName: null));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("sin-arroba")]
    public void Rechaza_un_correo_invalido(string email)
    {
        var result = _validator.Validate(CreateRequest(email: email));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(RegisterUserRequest.Email));
    }

    [Theory]
    [InlineData("corta1!")]
    [InlineData("sinmayusculas1!")]
    [InlineData("SinNumeros!")]
    [InlineData("SinEspeciales1")]
    public void Rechaza_una_contrasena_que_no_cumple_la_politica(string password)
    {
        var result = _validator.Validate(CreateRequest(password: password));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(RegisterUserRequest.Password));
    }

    [Fact]
    public void Exige_la_fecha_de_nacimiento()
    {
        var result = _validator.Validate(CreateRequest(birthDate: DateOnly.MinValue));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(RegisterUserRequest.BirthDate));
    }

    [Theory]
    [InlineData("roleCode")]
    [InlineData("companyId")]
    [InlineData("projectId")]
    [InlineData("ROLE_CODE")]
    public void El_cliente_no_puede_fijar_rol_compania_ni_proyecto(string forbiddenField)
    {
        var extensionData = new Dictionary<string, JsonElement>
        {
            [forbiddenField] = JsonDocument.Parse("\"valor\"").RootElement.Clone()
        };

        var result = _validator.Validate(CreateRequest(extensionData: extensionData));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == nameof(RegisterUserRequest.ExtensionData));
    }

    [Fact]
    public void Acepta_datos_extra_que_no_son_de_identidad()
    {
        var extensionData = new Dictionary<string, JsonElement>
        {
            ["origen"] = JsonDocument.Parse("\"web\"").RootElement.Clone()
        };

        _validator.Validate(CreateRequest(extensionData: extensionData)).IsValid.Should().BeTrue();
    }

    public static RegisterUserRequest CreateRequest(
        string email = "ana.perez@example.com",
        string username = "anaperez",
        string password = "ClaveSegura1!",
        DateOnly? birthDate = null,
        string? middleName = "Maria",
        string? secondLastName = "Gomez",
        IDictionary<string, JsonElement>? extensionData = null) => new()
    {
        FirstName = "Ana",
        MiddleName = middleName,
        LastName = "Perez",
        SecondLastName = secondLastName,
        Email = email,
        Username = username,
        Password = password,
        DocumentType = "CC",
        DocumentNumber = "12345678",
        BirthDate = birthDate ?? new DateOnly(1995, 5, 20),
        CountryId = Guid.NewGuid(),
        DepartmentId = Guid.NewGuid(),
        MunicipalityId = Guid.NewGuid(),
        ExtensionData = extensionData
    };
}
