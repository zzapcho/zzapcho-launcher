using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Zzapcho.Launcher.Core.Manifest;

namespace Zzapcho.Launcher.Infrastructure.Manifest;

public sealed class ManifestSignatureVerifier
{
    private const string PublicKeyPem = "";

    public SignatureVerificationResult Verify(string manifestJson, LauncherManifest manifest, bool allowDevelopmentPlaceholder)
    {
        if (allowDevelopmentPlaceholder &&
            (string.IsNullOrWhiteSpace(manifest.Signature) ||
             manifest.Signature.Equals("PUT_SIGNATURE_HERE", StringComparison.OrdinalIgnoreCase)))
        {
            return SignatureVerificationResult.DevelopmentPlaceholder();
        }

        if (string.IsNullOrWhiteSpace(PublicKeyPem))
        {
            return SignatureVerificationResult.Invalid("운영용 manifest 공개키가 아직 설정되지 않았습니다.");
        }

        try
        {
            using var rsa = RSA.Create();
            rsa.ImportFromPem(PublicKeyPem);
            var payload = Encoding.UTF8.GetBytes(RemoveSignature(manifestJson));
            var signature = Convert.FromBase64String(manifest.Signature);
            return rsa.VerifyData(payload, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1)
                ? SignatureVerificationResult.Valid()
                : SignatureVerificationResult.Invalid("manifest 서명이 올바르지 않습니다.");
        }
        catch (Exception ex)
        {
            return SignatureVerificationResult.Invalid($"manifest 서명 검증에 실패했습니다: {ex.Message}");
        }
    }

    private static string RemoveSignature(string manifestJson)
    {
        var node = JsonNode.Parse(manifestJson) as JsonObject
            ?? throw new InvalidDataException("manifest JSON 형식이 올바르지 않습니다.");
        node.Remove("signature");
        return node.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }
}
