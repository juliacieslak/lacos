using System.Security.Cryptography;
using FluentAssertions;
using Lacos.Application.Validators;
using Lacos.Domain.Exceptions;
using Lacos.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace Lacos.UnitTests.Infrastructure;

public class SecurityTests
{
    private static readonly IOptions<EncryptionOptions> Keys = Options.Create(new EncryptionOptions
    {
        DataKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)),
        BlindIndexKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
    });

    [Fact]
    public void Aes_gcm_round_trip_and_random_nonce()
    {
        var sut = new AesGcmEncryptionService(Keys);
        var a = sut.Encrypt("Diagnóstico: hipertensão");
        var b = sut.Encrypt("Diagnóstico: hipertensão");

        a.Should().StartWith(AesGcmEncryptionService.Prefix).And.NotBe(b); // nonce aleatório
        sut.Decrypt(a).Should().Be("Diagnóstico: hipertensão");
        sut.Encrypt(a).Should().Be(a); // não criptografa duas vezes
    }

    [Fact]
    public void Aes_gcm_detects_tampering()
    {
        var sut = new AesGcmEncryptionService(Keys);
        var bytes = sut.EncryptBytes(new byte[] { 1, 2, 3, 4 });
        bytes[^1] ^= 0xFF;
        var act = () => sut.DecryptBytes(bytes);
        act.Should().Throw<CryptographicException>();
    }

    [Fact]
    public void Encryption_requires_256_bit_key()
    {
        var act = () => new AesGcmEncryptionService(Options.Create(new EncryptionOptions { DataKey = Convert.ToBase64String(new byte[16]) }));
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Blind_index_is_deterministic_and_keyed()
    {
        var sut = new HmacBlindIndexService(Keys);
        sut.Compute("52998224725").Should().Be(sut.Compute("52998224725")).And.HaveLength(64);
        var other = new HmacBlindIndexService(Options.Create(new EncryptionOptions
        {
            BlindIndexKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
        }));
        other.Compute("52998224725").Should().NotBe(sut.Compute("52998224725"));
    }

    [Fact]
    public void File_signature_validation_accepts_real_formats_only()
    {
        var pdf = "%PDF-1.7 conteudo"u8.ToArray();
        var png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0 };
        var exe = new byte[] { 0x4D, 0x5A, 0x90, 0x00 };

        FileSignatureValidator.Validate("exame.pdf", "application/pdf", pdf).Should().Be("application/pdf");
        FileSignatureValidator.Validate("foto.PNG", "image/png", png).Should().Be("image/png");
        ((Action)(() => FileSignatureValidator.Validate("virus.pdf", "application/pdf", exe))).Should().Throw<ValidationFailedException>();
        ((Action)(() => FileSignatureValidator.Validate("script.exe", "application/octet-stream", exe))).Should().Throw<ValidationFailedException>();
        ((Action)(() => FileSignatureValidator.Validate("exame.pdf", "image/png", pdf))).Should().Throw<ValidationFailedException>();
    }

    [Theory]
    [InlineData("../../etc/passwd.pdf", "passwd.pdf")]
    [InlineData("receita\r\nX.pdf", "receita__X.pdf")]
    public void File_names_are_sanitized(string input, string expected) =>
        FileSignatureValidator.SanitizeFileName(input).Should().Be(expected);
}
