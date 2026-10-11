using System.Buffers.Binary;

namespace HealthTrack.Services;

public static class ProfilePhotoValidator
{
    // Derive the response type from file bytes, never from the submitted MIME type.
    public static string? GetContentType(byte[] bytes, string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        ReadOnlySpan<byte> data = bytes;
        if (extension == ".png" && data.Length >= 45
            && data[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })
            && BinaryPrimitives.ReadUInt32BigEndian(data[8..12]) == 13
            && data[12..16].SequenceEqual("IHDR"u8)
            && data[^12..^8].SequenceEqual(new byte[4])
            && data[^8..^4].SequenceEqual("IEND"u8))
        {
            var width = BinaryPrimitives.ReadUInt32BigEndian(data[16..20]);
            var height = BinaryPrimitives.ReadUInt32BigEndian(data[20..24]);
            return width is > 0 and <= 4096 && height is > 0 and <= 4096 ? "image/png" : null;
        }

        if (extension is not (".jpg" or ".jpeg") || data.Length < 4
            || data[0] != 0xff || data[1] != 0xd8 || data[^2] != 0xff || data[^1] != 0xd9)
            return null;

        // Read JPEG segments until a supported frame provides the dimensions.
        var offset = 2;
        while (offset + 4 <= data.Length)
        {
            if (data[offset++] != 0xff) return null;
            while (offset < data.Length && data[offset] == 0xff) offset++;
            if (offset >= data.Length) return null;
            var marker = data[offset++];
            if (marker is 0xda or 0xd9) return null;
            if (offset + 2 > data.Length) return null;
            var length = BinaryPrimitives.ReadUInt16BigEndian(data[offset..]);
            if (length < 2 || offset + length > data.Length) return null;
            if (marker is 0xc0 or 0xc1 or 0xc2)
            {
                if (length < 8) return null;
                var height = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 3)..]);
                var width = BinaryPrimitives.ReadUInt16BigEndian(data[(offset + 5)..]);
                return width is > 0 and <= 4096 && height is > 0 and <= 4096 ? "image/jpeg" : null;
            }
            offset += length;
        }
        return null;
    }
}
