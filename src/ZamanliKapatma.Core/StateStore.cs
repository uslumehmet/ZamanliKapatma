using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZamanliKapatma.Core;

/// <summary>Plan durumunu JSON olarak saklar (varsayılan: %APPDATA%\ZamanliKapatma\state.json).</summary>
public sealed class StateStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _path;

    public StateStore()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ZamanliKapatma",
            "state.json"))
    {
    }

    public StateStore(string path) => _path = path;

    /// <summary>Kayıtlı durumu okur; dosya yoksa ya da bozuksa null döner.</summary>
    public SchedulerState? Load()
    {
        try
        {
            return File.Exists(_path)
                ? JsonSerializer.Deserialize<SchedulerState>(File.ReadAllText(_path), JsonOptions)
                : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            return null;
        }
    }

    /// <summary>Durumu önce geçici dosyaya yazıp sonra taşır; yarım kalmış dosya oluşmaz.</summary>
    public void Save(SchedulerState state)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            string tempPath = _path + ".tmp";
            File.WriteAllText(tempPath, JsonSerializer.Serialize(state, JsonOptions));
            File.Move(tempPath, _path, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Kaydedilemezse uygulama çalışmaya devam eder; yalnızca plan bir sonraki açılışa taşınmaz.
        }
    }
}
