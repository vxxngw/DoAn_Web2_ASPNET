namespace Recruitment.Api.Services;

internal static class PdfUpload
{
    public static async Task<byte[]> ReadAsync(IFormFile file, string label, CancellationToken ct)
    {
        if (file.Length > 5 * 1024 * 1024) throw new BusinessException(413, $"{label} vượt quá 5 MiB.");
        if (file.Length < 5 || !string.Equals(Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
            throw new BusinessException(400, $"Vui lòng chọn tệp {label} định dạng PDF.");
        using var buffer = new MemoryStream((int)file.Length);
        await file.CopyToAsync(buffer, ct);
        var bytes = buffer.ToArray();
        if (!bytes.AsSpan().StartsWith("%PDF-"u8)) throw new BusinessException(400, "Nội dung tệp thiếu chữ ký PDF.");
        return bytes;
    }
}
