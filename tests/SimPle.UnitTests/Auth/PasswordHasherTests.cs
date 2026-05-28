using FluentAssertions;
using SimPle.Infrastructure.Auth;

namespace SimPle.UnitTests.Auth;

public sealed class PasswordHasherTests
{
    private readonly Argon2PasswordHasher _hasher = new();

    [Fact]
    public void Hash_UsesArgon2idPhcFormatAndUniqueSalt()
    {
        var first = _hasher.Hash("correct horse battery staple");
        var second = _hasher.Hash("correct horse battery staple");

        first.Should().StartWith("$argon2id$v=19$m=65536,t=3,p=1$");
        first.Should().NotBe(second);
    }

    [Fact]
    public void Verify_ReturnsTrueOnlyForMatchingPassword()
    {
        var hash = _hasher.Hash("a long correct password");

        _hasher.Verify("a long correct password", hash).Should().BeTrue();
        _hasher.Verify("an incorrect password", hash).Should().BeFalse();
    }

    [Fact]
    public void NeedsRehash_DetectsOlderParametersAndMalformedHashes()
    {
        var current = _hasher.Hash("a long correct password");
        var weakerParameters = current.Replace("m=65536", "m=32768");

        _hasher.NeedsRehash(current).Should().BeFalse();
        _hasher.NeedsRehash(weakerParameters).Should().BeTrue();
        _hasher.NeedsRehash("not-a-hash").Should().BeTrue();
    }
}
