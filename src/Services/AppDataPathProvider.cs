using System;
using System.IO;

namespace ReadX.Services;

public sealed class AppDataPathProvider : IAppDataPathProvider
{
    public string ReadXDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ReadX");
}
