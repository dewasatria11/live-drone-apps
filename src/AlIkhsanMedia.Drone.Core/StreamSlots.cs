using System.Security.Cryptography;

namespace AlIkhsanMedia.Drone.Core;

public sealed record ProtectedSecret(string Algorithm, string Ciphertext);

public static class SecureStreamKey
{
    public const int EntropyBytes = 16;
    public static string Create()
    {
        Span<byte> bytes = stackalloc byte[EntropyBytes]; RandomNumberGenerator.Fill(bytes);
        return $"drone-{Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_')}";
    }
    public static bool IsValid(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.StartsWith("drone-", StringComparison.Ordinal)) return false;
        var encoded = value[6..]; if (encoded.Any(static c => !(char.IsAsciiLetterOrDigit(c) || c is '-' or '_'))) return false;
        try { var padded = encoded.Replace('-', '+').Replace('_', '/') + new string('=', (4 - encoded.Length % 4) % 4); return Convert.FromBase64String(padded).Length >= EntropyBytes; }
        catch (FormatException) { return false; }
    }
    public static string Redact(string value) => value.Length <= 10 ? "[RAHASIA]" : $"{value[..10]}…[RAHASIA]";
}

public sealed record StreamSlot(StreamSlotId Id, string DisplayName, bool Enabled, ProtectedSecret StreamKey, StreamRuntimeState Runtime);

public static class StreamSlotFactory
{
    public const int SlotCount = 6;
    public static IReadOnlyList<StreamSlot> CreateSix(ISecretProtector protector)
    {
        ArgumentNullException.ThrowIfNull(protector);
        return Enumerable.Range(1, SlotCount).Select(index => new StreamSlot(new StreamSlotId(Guid.NewGuid()), $"Drone {index}", index == 1,
            protector.Protect(SecureStreamKey.Create()), StreamRuntimeState.Disabled)).ToArray();
    }
}
