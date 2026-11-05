using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HealthTrack.Models.Data
{
    // EF value converter that encrypts text columns with ASP.NET Core Data Protection; plain text saved before encryption is still readable.
    public class EncryptedStringConverter : ValueConverter<string, string>
    {
        public EncryptedStringConverter(IDataProtector protector)
            : base(value => protector.Protect(value), value => Unprotect(protector, value))
        {
        }

        private static string Unprotect(IDataProtector protector, string value)
        {
            try
            {
                return protector.Unprotect(value);
            }
            catch (CryptographicException)
            {
                return value;
            }
        }
    }
}
