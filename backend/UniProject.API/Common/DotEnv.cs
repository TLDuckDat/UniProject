namespace UniProject.API.Common
{
    /// <summary>
    /// Nạp file .env vào biến môi trường. ASP.NET Core đọc biến môi trường vào IConfiguration
    /// với quy ước "__" thay cho ":" (Jwt__Key → config["Jwt:Key"]).
    /// </summary>
    public static class DotEnv
    {
        /// <summary>
        /// Biến môi trường đã có sẵn được ưu tiên, không bị .env ghi đè.
        /// </summary>
        public static void Load(string fileName = ".env")
        {
            var path = new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory }
                .Select(dir => Path.Combine(dir, fileName))
                .FirstOrDefault(File.Exists);
            if (path == null) return;

            foreach (var rawLine in File.ReadAllLines(path))
            {
                var line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith('#')) continue;

                var separator = line.IndexOf('=');
                if (separator <= 0) continue;

                var key = line[..separator].Trim();
                var value = line[(separator + 1)..].Trim();

                if (value.Length >= 2 && (value[0] == '"' || value[0] == '\'') && value[^1] == value[0])
                    value = value[1..^1];

                if (Environment.GetEnvironmentVariable(key) == null)
                    Environment.SetEnvironmentVariable(key, value);
            }
        }
    }
}
