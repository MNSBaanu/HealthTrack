using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HealthTrack.Models.Data
{
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
