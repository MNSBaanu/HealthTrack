using System.Globalization;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using HealthTrack.Models;
using HealthTrack.Models.Data;
using Microsoft.AspNetCore.Identity;

namespace HealthTrack.Services
{
    public record ImportedUser(string Name, string Email, string Role, string? TemporaryPassword);

    public record RejectedRow(int Line, string Email, string Reason);

    public class UserImportResult
    {
        public List<ImportedUser> Imported { get; } = new();
        public List<RejectedRow> Rejected { get; } = new();
        public string? FileError { get; set; }
    }

    public class UserImportService
    {
        public const int MaxRows = 500;
        public const int MaxBytes = 1_000_000;
        public const string Template =
            "Name,Email,Role,DateOfBirth,HeightCm,FitnessGoal,Password\r\n" +
            "Jane Doe,jane.doe@example.com,Client,1999-05-14,165,Build strength,\r\n" +
            "Sam Coach,sam.coach@example.com,Trainer,1990-02-03,,,\r\n";

        private static readonly string[] RequiredColumns = { "name", "email", "role", "dateofbirth" };

        private readonly UserManager<Person> _users;
        private readonly HealthTrackDbContext _context;
        private readonly TimeProvider _clock;

        public UserImportService(UserManager<Person> users, HealthTrackDbContext context, TimeProvider clock)
        {
            _users = users;
            _context = context;
            _clock = clock;
        }

        public async Task<UserImportResult> ImportAsync(Stream stream)
        {
            var result = new UserImportResult();
            List<List<string>> rows;
            using (var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true))
            {
                rows = ParseCsv(await reader.ReadToEndAsync());
            }

            if (rows.Count == 0)
            {
                result.FileError = "The file is empty.";
                return result;
            }

            var header = rows[0].Select(h => h.Trim().ToLowerInvariant()).ToList();
            var missing = RequiredColumns.Where(c => !header.Contains(c)).ToList();
            if (missing.Count > 0)
            {
                result.FileError = "Missing required column(s): " + string.Join(", ", missing) + ". Download the template to see the expected columns.";
                return result;
            }
            if (rows.Count - 1 > MaxRows)
            {
                result.FileError = $"The file has more than {MaxRows} rows. Split it into smaller files.";
                return result;
            }

            var seenEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 1; index < rows.Count; index++)
            {
                var cells = rows[index];
                string Cell(string column)
                {
                    var position = header.IndexOf(column);
                    return position >= 0 && position < cells.Count ? cells[position].Trim() : string.Empty;
                }

                var email = Cell("email");
                var reason = await ImportRowAsync(Cell, email, seenEmails, result);
                if (reason != null)
                {
                    result.Rejected.Add(new RejectedRow(index + 1, email, reason));
                }
            }
            return result;
        }

        private async Task<string?> ImportRowAsync(Func<string, string> cell, string email, HashSet<string> seenEmails, UserImportResult result)
        {
            var name = cell("name");
            if (name.Length == 0 || name.Length > 100)
            {
                return "Name is required and must be at most 100 characters.";
            }
            if (!IsValidEmail(email))
            {
                return "Email is not valid.";
            }
            if (!seenEmails.Add(email))
            {
                return "Email appears more than once in the file.";
            }

            var role = cell("role");
            var isClient = role.Equals(AppRoles.Client, StringComparison.OrdinalIgnoreCase);
            var isTrainer = role.Equals(AppRoles.Trainer, StringComparison.OrdinalIgnoreCase);
            if (!isClient && !isTrainer)
            {
                return "Role must be Client or Trainer.";
            }

            if (!DateTime.TryParseExact(cell("dateofbirth"), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var born)
                || born >= DateTime.Today || born.Year < 1900)
            {
                return "DateOfBirth must be a past date written as yyyy-MM-dd.";
            }

            var height = 0d;
            var goal = cell("fitnessgoal");
            if (isClient)
            {
                if (!double.TryParse(cell("heightcm"), NumberStyles.Float, CultureInfo.InvariantCulture, out height) || height < 50 || height > 250)
                {
                    return "HeightCm must be a number between 50 and 250 for clients.";
                }
                if (goal.Length > 200)
                {
                    return "FitnessGoal must be at most 200 characters.";
                }
            }

            if (await _users.FindByEmailAsync(email) != null)
            {
                return "An account with this email already exists.";
            }

            var providedPassword = cell("password");
            var password = providedPassword.Length > 0 ? providedPassword : GeneratePassword();
            Person person = isClient
                ? new Client { HeightCm = height, FitnessGoal = goal.Length == 0 ? null : goal }
                : new Trainer();
            person.UserName = email;
            person.Email = email;
            person.EmailConfirmed = true;
            person.Name = name;
            person.DateOfBirth = born;
            person.MustChangePassword = true;
            person.TemporaryPasswordExpiresAtUtc = _clock.GetUtcNow().UtcDateTime.AddHours(48);

            var roleName = isClient ? AppRoles.Client : AppRoles.Trainer;
            var created = await AccountProvisioner.CreateAsync(_context, _users, person, password, roleName);
            if (!created.Succeeded)
            {
                return string.Join(" ", created.Errors.Select(e => e.Description));
            }

            result.Imported.Add(new ImportedUser(name, email, roleName, providedPassword.Length > 0 ? null : password));
            return null;
        }

        private static bool IsValidEmail(string email)
        {
            if (email.Length == 0 || email.Length > 256)
            {
                return false;
            }
            try
            {
                return new MailAddress(email).Address == email;
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static string GeneratePassword()
        {
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghijkmnopqrstuvwxyz";
            const string digits = "23456789";
            const string symbols = "!@#$%*?";
            var all = upper + lower + digits + symbols;
            var chars = new List<char>
            {
                upper[RandomNumberGenerator.GetInt32(upper.Length)],
                lower[RandomNumberGenerator.GetInt32(lower.Length)],
                digits[RandomNumberGenerator.GetInt32(digits.Length)],
                symbols[RandomNumberGenerator.GetInt32(symbols.Length)]
            };
            while (chars.Count < 12)
            {
                chars.Add(all[RandomNumberGenerator.GetInt32(all.Length)]);
            }
            return new string(chars.OrderBy(_ => RandomNumberGenerator.GetInt32(int.MaxValue)).ToArray());
        }

        private static List<List<string>> ParseCsv(string text)
        {
            var rows = new List<List<string>>();
            var row = new List<string>();
            var field = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];
                if (inQuotes)
                {
                    if (c == '"' && i + 1 < text.Length && text[i + 1] == '"')
                    {
                        field.Append('"');
                        i++;
                    }
                    else if (c == '"')
                    {
                        inQuotes = false;
                    }
                    else
                    {
                        field.Append(c);
                    }
                }
                else if (c == '"')
                {
                    inQuotes = true;
                }
                else if (c == ',')
                {
                    row.Add(field.ToString());
                    field.Clear();
                }
                else if (c == '\r' || c == '\n')
                {
                    if (c == '\r' && i + 1 < text.Length && text[i + 1] == '\n')
                    {
                        i++;
                    }
                    row.Add(field.ToString());
                    field.Clear();
                    if (row.Any(f => f.Trim().Length > 0))
                    {
                        rows.Add(row);
                    }
                    row = new List<string>();
                }
                else
                {
                    field.Append(c);
                }
            }

            row.Add(field.ToString());
            if (row.Any(f => f.Trim().Length > 0))
            {
                rows.Add(row);
            }
            return rows;
        }
    }
}
