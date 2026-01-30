using System.IO;
using System.Linq;
using UnityEditor;

namespace SimpleToolkits.Editor
{
    /// <summary>
    /// 实时监听 ExcelConfigs 目录，非临时 .xlsx/.xls 文件改动后
    /// 延迟调用 ExcelExEditor.GenerateConfigs() 一次。
    /// </summary>
    [InitializeOnLoad]
    public static class ExcelWatcher
    {
        private static FileSystemWatcher _watcher;
        private static bool _dirty; // 是否有改动未处理
        private static readonly string[] _extensions = {".xlsx", ".xls"};
        private const string Excel_File_Path = "Assets/ExcelConfigs";

        static ExcelWatcher()
        {
            StartWatch();
            // 域重载后重新挂接
            AssemblyReloadEvents.afterAssemblyReload += StartWatch;
        }

        private static void StartWatch()
        {
            _watcher?.Dispose();

            string root = Excel_File_Path;
            if (!Directory.Exists(root)) Directory.CreateDirectory(root);

            _watcher = new FileSystemWatcher(root)
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                IncludeSubdirectories = false,
                EnableRaisingEvents = true
            };

            _watcher.Changed += OnChanged;
            _watcher.Created += OnChanged;
            _watcher.Deleted += OnChanged;
            _watcher.Renamed += OnChanged;
        }

        private static void OnChanged(object sender, FileSystemEventArgs e)
        {
            string ext = Path.GetExtension(e.FullPath).ToLower();
            string fileName = Path.GetFileName(e.FullPath);

            // 仅处理目标扩展名，且排除临时文件
            if (!_extensions.Contains(ext) || fileName.StartsWith("~$"))
                return;

            if (!_dirty)
            {
                _dirty = true;
                // 延迟到下一帧统一执行一次
                EditorApplication.update += DelayGenerate;
            }
        }

        private static void DelayGenerate()
        {
            EditorApplication.update -= DelayGenerate;
            if (_dirty)
            {
                _dirty = false;
                EditorUtils.GenerateConfigs();
            }
        }
    }
}
