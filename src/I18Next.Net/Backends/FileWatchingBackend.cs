using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using I18Next.Net.TranslationTrees;

namespace I18Next.Net.Backends;

/// <summary>
///     Watches the translation files of a file based backend and notifies about changed namespaces so they are loaded
///     again without restarting the application. Files are expected in the <c>{lng}/{ns}.{extension}</c> layout, changes
///     to other files reload all namespaces.
/// </summary>
public class FileWatchingBackend : INotifyingTranslationBackend, IDisposable
{
    private readonly ITranslationBackend _backend;
    private readonly string _basePath;
    private readonly HashSet<(string Language, string Namespace)> _pendingChanges = [];
    private readonly Timer _timer;
    private readonly FileSystemWatcher _watcher;

    public FileWatchingBackend(ITranslationBackend backend, string basePath, string filter = "*.*")
    {
        _backend = backend ?? throw new ArgumentNullException(nameof(backend));
        _basePath = Path.GetFullPath(basePath ?? throw new ArgumentNullException(nameof(basePath)));
        _timer = new Timer(_ => RaisePendingChanges(), null, Timeout.Infinite, Timeout.Infinite);

        if (_backend is INotifyingTranslationBackend notifyingBackend)
            notifyingBackend.TranslationsChanged += (_, e) => TranslationsChanged?.Invoke(this, e);

        Directory.CreateDirectory(_basePath);

        _watcher = new FileSystemWatcher(_basePath, filter)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite | NotifyFilters.Size
        };
        _watcher.Changed += OnFileChanged;
        _watcher.Created += OnFileChanged;
        _watcher.Deleted += OnFileChanged;
        _watcher.Renamed += OnFileRenamed;
        _watcher.EnableRaisingEvents = true;
    }

    /// <summary>
    ///     The time to wait for further changes before notifying, as editors often write a file several times when saving.
    /// </summary>
    public TimeSpan Delay { get; set; } = TimeSpan.FromMilliseconds(200);

    public ITranslationBackend Backend => _backend;

    public event EventHandler<TranslationsChangedEventArgs> TranslationsChanged;

    public Task<ITranslationTree> LoadNamespaceAsync(string language, string @namespace)
    {
        return _backend.LoadNamespaceAsync(language, @namespace);
    }

    public void Dispose()
    {
        _watcher.Dispose();
        _timer.Dispose();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    ///     Maps a changed file to the language and namespace it contains. Returns <c>(null, null)</c> to reload everything.
    /// </summary>
    protected virtual (string Language, string Namespace) GetChangedNamespace(string path)
    {
        var relativePath = path.Substring(_basePath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var parts = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (parts.Length != 2 || Path.GetExtension(parts[1]).Length == 0)
            return (null, null);

        return (parts[0], Path.GetFileNameWithoutExtension(parts[1]));
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        AddPendingChange(e.FullPath);
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        AddPendingChange(e.OldFullPath);
        AddPendingChange(e.FullPath);
    }

    private void AddPendingChange(string path)
    {
        var change = GetChangedNamespace(path);

        lock (_pendingChanges)
            _pendingChanges.Add(change);

        _timer.Change(Delay, Timeout.InfiniteTimeSpan);
    }

    private void RaisePendingChanges()
    {
        (string Language, string Namespace)[] changes;

        lock (_pendingChanges)
        {
            if (_pendingChanges.Contains((null, null)))
                changes = [(null, null)];
            else
                changes = [.. _pendingChanges];

            _pendingChanges.Clear();
        }

        foreach (var change in changes)
            TranslationsChanged?.Invoke(this, new TranslationsChangedEventArgs(change.Language, change.Namespace));
    }
}
