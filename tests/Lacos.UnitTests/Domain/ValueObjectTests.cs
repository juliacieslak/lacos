using FluentAssertions;
using Lacos.Domain.Enums;
using Lacos.Domain.ValueObjects;

namespace Lacos.UnitTests.Domain;

public class ValueObjectTests
{
    [Theory]
    [InlineData("529.982.247-25", true)]
    [InlineData("52998224725", true)]
    [InlineData("111.111.111-11", false)] // sequência repetida
    [InlineData("529.982.247-24", false)] // dígito verificador errado
    [InlineData("123", false)]
    [InlineData(null, false)]
    public void Cpf_validates_check_digits(string? input, bool expected) => Cpf.IsValid(input).Should().Be(expected);

    [Fact]
    public void Cpf_is_masked_for_display() => Cpf.Create("52998224725").Masked.Should().Be("***.982.247-**");

    [Theory]
    [InlineData("80010-000", true)]
    [InlineData("80010000", true)]
    [InlineData("8001000", false)]
    [InlineData("00000-000", false)]
    [InlineData("ABCDE-123", false)]
    public void Cep_validation(string input, bool expected) => Cep.IsValid(input).Should().Be(expected);

    [Theory]
    [InlineData("I10", true)]
    [InlineData("e11.9", true)]
    [InlineData("F03", true)]
    [InlineData("5A11", true)]
    [InlineData("8A20.0", true)]
    [InlineData("X", false)]
    [InlineData("12345", false)]
    public void Cid_code_validation(string input, bool expected) => CidCode.IsValid(input).Should().Be(expected);

    [Fact]
    public void Unit_conversions_use_official_factors()
    {
        UnitConverter.ToCelsius(98.6m, TemperatureUnit.Fahrenheit).Should().Be(37.0m);
        UnitConverter.ToMgPerDl(5.5m, GlucoseUnit.MmolPerL).Should().Be(99.10m);
        UnitConverter.ToKilograms(150m, WeightUnit.Pound).Should().Be(68.04m);
        UnitConverter.ToCelsius(36.5m, TemperatureUnit.Celsius).Should().Be(36.5m);
    }
}
