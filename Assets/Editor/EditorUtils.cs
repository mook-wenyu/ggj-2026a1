using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using UnityEditor;
using UnityEngine;

namespace SimpleToolkits.Editor
{
    public class EditorUtils
    {
        /// <summary>
        /// 字段信息
        /// </summary>
        private class PropertyInfo
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public string Comment { get; set; }
        }

        /// <summary>
        /// Excel 配置信息
        /// </summary>
        private class ExcelConfig
        {
            /// <summary>
            /// 类名（用于类型查找），如 "Example"
            /// </summary>
            public string ClassName { get; set; }

            /// <summary>
            /// JSON文件名（包含工作表信息），如 "Example_Config"
            /// </summary>
            public string JsonName { get; set; }

            public List<PropertyInfo> Properties { get; set; }
            public ISheet Sheet { get; set; }
        }

        private static readonly List<ExcelConfig> _excelConfigs = new();
        private static readonly HashSet<string> _generatedClassFiles = new();
        private static readonly string[] _extensions = { ".xlsx", ".xls" };
        private const string Excel_File_Path = "Assets/ExcelConfigs";
        private const string Cs_Output_Path = "Assets/Scripts/Configs";
        private const string Json_Output_Path = "Assets/Resources/JsonConfigs";

        private static readonly JsonSerializerSettings _jsonSerializerSettings = new()
        {
            TypeNameHandling = TypeNameHandling.Objects,
            Formatting = Formatting.Indented,
        };

        [MenuItem("Tools/Excel To Json", priority = 0)]
        public static void GenerateConfigs()
        {
            DeleteAllOldFiles();
            _excelConfigs.Clear();        // 在开始处清空配置列表
            _generatedClassFiles.Clear(); // 清空已生成Class文件的跟踪集合

            string excelDirPath = Excel_File_Path;
            if (!Directory.Exists(excelDirPath)) Directory.CreateDirectory(excelDirPath);

            string[] excelFiles = Directory.EnumerateFiles(excelDirPath)
                .Where(file =>
                {
                    string fileName = Path.GetFileName(file);
                    string ext = Path.GetExtension(file);
                    return !fileName.StartsWith("~$") &&
                           _extensions.Contains(ext, StringComparer.OrdinalIgnoreCase);
                })
                .ToArray();
            if (excelFiles.Length == 0)
            {
                Debug.LogError("配置文件夹为空");
                return;
            }

            foreach (string excelFile in excelFiles)
            {
                ReadExcel(excelFile);
            }

            // 刷新资源数据库，确保新生成的类文件被编译
            AssetDatabase.Refresh();

            // 只在 delayCall 中生成 JSON，确保编译完成
            EditorApplication.delayCall += () =>
            {
                var successCount = 0;
                foreach (var excelConfig in _excelConfigs)
                {
                    if (GenerateConfigJson(excelConfig))
                    {
                        successCount++;
                    }
                }
                AssetDatabase.Refresh();
                _excelConfigs.Clear();
                Debug.Log($"JSON配置文件生成完成！成功生成 {successCount} 个文件。");
            };
        }

        // 读取Excel
        private static void ReadExcel(string excelFilePath)
        {
            IWorkbook wk;
            string extension = Path.GetExtension(excelFilePath);
            string fileName = Path.GetFileNameWithoutExtension(excelFilePath);

            using var stream = new FileStream(excelFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            if (extension.Equals(".xls"))
            {
                wk = new HSSFWorkbook(stream);
            }
            else
            {
                wk = new XSSFWorkbook(stream);
            }

            for (var i = 0; i < wk.NumberOfSheets; i++)
            {
                // 读取第i个工作表
                var sheet = wk.GetSheetAt(i);
                ReadExcelSheets(sheet, fileName);
            }
        }

        private static void ReadExcelSheets(ISheet sheet, string fileName)
        {
            var rowComment = sheet.GetRow(0); // 字段注释
            var row = sheet.GetRow(1);        // 字段名
            var rowType = sheet.GetRow(2);    // 字段类型

            var rowId = row.GetCell(0);
            if (rowId.ToString() != "id")
            {
                throw new Exception($"导出Configs错误！{fileName} - {sheet.SheetName}表中第一列不是id！");
            }

            List<PropertyInfo> properties = new();
            for (var i = 0; i < row.LastCellNum; i++)
            {
                string comment = rowComment.GetCell(i).ToString().Trim();
                string field = row.GetCell(i).ToString().Trim();
                string type = rowType.GetCell(i).ToString().Trim();

                if (string.IsNullOrEmpty(field) || string.IsNullOrEmpty(type)) break;

                properties.Add(new PropertyInfo
                {
                    Name = field,
                    Type = type,
                    Comment = comment
                });
            }

            // 类名
            var className = $"{fileName}Config";
            // Class文件生成：每个Excel文件只生成一个Class文件
            if (!_generatedClassFiles.Contains(fileName))
            {
                GenerateConfigClass(properties, className);
                _generatedClassFiles.Add(fileName);
            }

            // Json配置：每个工作表生成独立的配置，使用组合名称
            var jsonConfigName = $"{className}_{SanitizeSheetName(sheet.SheetName)}";
            _excelConfigs.Add(new ExcelConfig
            {
                ClassName = className,     // 类名使用文件名加Config，如 "ExampleConfig"
                JsonName = jsonConfigName, // JSON为类名包含工作表信息，如 "ExampleConfig_Config"
                Properties = properties,
                Sheet = sheet
            });
        }

        /// <summary>
        /// 生成配置类文件
        /// </summary>
        private static void GenerateConfigClass(List<PropertyInfo> properties, string configName)
        {
            string filePath = Path.Combine(Cs_Output_Path, $"{configName}.cs");
            string fileDir = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(fileDir))
            {
                throw new Exception($"生成配置类文件失败，文件路径为空！{filePath}");
            }
            if (!Directory.Exists(fileDir)) Directory.CreateDirectory(fileDir);
            if (File.Exists(filePath))
            {
                Debug.LogWarning($"配置类已存在！{filePath}");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"public class {configName} : BaseConfig");
            sb.AppendLine("{");
            foreach (var property in properties)
            {
                if (property.Name == "id") continue;
                if (!string.IsNullOrEmpty(property.Comment))
                {
                    sb.AppendLine("    /// <summary>");
                    sb.AppendLine($"    /// {property.Comment}");
                    sb.AppendLine("    /// </summary>");
                }
                sb.AppendLine($"    public {property.Type} {property.Name};");
            }
            sb.AppendLine("}");
            // 约定：生成文件使用 UTF-8 无 BOM（避免产生不可见差异）。
            File.WriteAllText(filePath, sb.ToString(), new UTF8Encoding(false));
        }

        /// <summary>
        /// 生成配置JSON文件
        /// </summary>
        /// <param name="excelConfig"></param>
        /// <returns>是否成功生成</returns>
        private static bool GenerateConfigJson(ExcelConfig excelConfig)
        {
            string filePath = Path.Combine(Json_Output_Path, $"{excelConfig.JsonName}.json");
            string fileDir = Path.GetDirectoryName(filePath);
            if (string.IsNullOrEmpty(fileDir))
            {
                Debug.LogError($"生成JSON文件失败，文件路径为空！{filePath}");
                return false;
            }
            if (!Directory.Exists(fileDir)) Directory.CreateDirectory(fileDir);

            // 改进的类型查找逻辑 - 使用 ClassName 而不是 JsonName
            var type = FindConfigType($"{excelConfig.ClassName}");
            if (type == null)
            {
                Debug.LogError($"找不到类型: {excelConfig.ClassName}，请确保类文件已编译完成");
                return false;
            }

            Dictionary<string, BaseConfig> rawDataDict = new();

            for (var i = 3; i <= excelConfig.Sheet.LastRowNum; i++)
            {
                var row = excelConfig.Sheet.GetRow(i);
                if (row == null) break;

                // 获取当前行的id值
                var idCell = row.GetCell(0);
                if (idCell == null)
                {
                    Debug.LogWarning($"第{i + 1}行的id列为空，跳过该行");
                    continue;
                }
                var id = idCell.ToString();
                if (string.IsNullOrEmpty(id))
                {
                    Debug.LogWarning($"第{i + 1}行的id值为空，跳过该行");
                    continue;
                }

                StringBuilder sb = new();
                sb.Append("{");

                // 修复循环边界，确保不超出任一边界
                int maxColumns = Math.Min(row.LastCellNum, excelConfig.Properties.Count);
                for (var j = 0; j < maxColumns; j++)
                {
                    var cell = row.GetCell(j);
                    if (cell == null) continue;

                    string value;
                    if (cell.CellType == CellType.Formula)
                    {
                        value = cell.CachedFormulaResultType == CellType.Numeric
                            ? cell.NumericCellValue.ToString(NumberFormatInfo.CurrentInfo)
                            : cell.StringCellValue.Replace(@"\", @"\\");
                    }
                    else
                    {
                        value = cell.ToString().Replace(@"\", @"\\");
                    }

                    if (string.IsNullOrEmpty(value))
                    {
                        if (excelConfig.Properties[j].Type != "string" && excelConfig.Properties[j].Type != "string[]")
                        {
                            value = "0";
                        }
                        Debug.LogWarning($"有空值！{excelConfig.JsonName}表中第{i + 1}行第{j + 1}列（字段名：{excelConfig.Properties[j].Name}）的值为空！");
                    }

                    if (excelConfig.Properties[j].Type == "bool")
                    {
                        // 布尔类型
                        if (value.ToLower() != "true" && value.ToLower() != "false")
                        {
                            value = value == "0" ? "false" : "true";
                        }
                        else
                        {
                            value = value.ToLower(); // "TRUE" -> "true", "FALSE" -> "false"
                        }
                    }

                    if (excelConfig.Properties[j].Type.Contains("[]"))
                    {
                        // 处理数组类型（支持数字数组和字符串数组）
                        if (excelConfig.Properties[j].Type == "string[]")
                        {
                            // 使用 ParseCsvStyleArray 解析复杂字符串
                            var parsedValues = ParseCsvStyleArray(value);
                            var escapedValues = parsedValues
                                .Select(s => $"\"{s.Replace("\"", "\\\"")}\""); // 转义双引号
                            value = $"[{string.Join(",", escapedValues.ToArray())}]";
                        }
                        else
                        {
                            // 其他数组（如 int[]、float[]）
                            value = $"[{value}]";
                        }
                    }
                    else
                    {
                        // 字符串类型
                        value = value.Replace("\"", "\\\""); // 转义引号
                        value = $"\"{value}\"";
                    }

                    sb.Append($"\"{excelConfig.Properties[j].Name}\":{value}");
                    if (j < maxColumns - 1) sb.Append(",");
                }
                sb.Append("}");

                try
                {
                    if (JsonConvert.DeserializeObject(sb.ToString(), type) is not BaseConfig configObj
                        || string.IsNullOrEmpty(configObj.id)) continue;
                    rawDataDict[id] = configObj;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"反序列化配置对象时发生异常，行{i + 1}，id: {id}，异常: {ex.Message}");
                }
            }

            string json = JsonConvert.SerializeObject(rawDataDict, _jsonSerializerSettings);
            // 约定：生成文件使用 UTF-8 无 BOM（避免产生不可见差异）。
            File.WriteAllText(filePath, json, new UTF8Encoding(false));

            Debug.Log($"JSON文件已生成: {filePath}，包含 {rawDataDict.Count} 条数据");
            return true;
        }

        /// <summary>
        /// 改进的类型查找方法，搜索所有已加载的程序集
        /// </summary>
        /// <param name="typeName">类型名称</param>
        /// <returns>找到的类型，如果没找到则返回null</returns>
        private static Type FindConfigType(string typeName)
        {
            // 尝试在 Assembly-CSharp 中查找
            var type = Type.GetType($"{typeName}, Assembly-CSharp");
            if (type != null) return type;

            // 搜索所有已加载的程序集
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                type = assembly.GetType(typeName);
                if (type != null) return type;
            }

            return null;
        }

        /// <summary>
        /// 清理工作表名称，确保生成有效的C#标识符和文件名
        /// </summary>
        /// <param name="sheetName">原始工作表名称</param>
        /// <returns>清理后的工作表名称</returns>
        private static string SanitizeSheetName(string sheetName)
        {
            if (string.IsNullOrEmpty(sheetName))
            {
                return "Sheet";
            }

            var sb = new StringBuilder();

            // 确保第一个字符是字母或下划线
            char firstChar = sheetName[0];
            if (char.IsLetter(firstChar) || firstChar == '_')
            {
                sb.Append(firstChar);
            }
            else
            {
                sb.Append('_');
            }

            // 处理其余字符，只保留字母、数字和下划线
            for (var i = 1; i < sheetName.Length; i++)
            {
                char c = sheetName[i];
                if (char.IsLetterOrDigit(c) || c == '_')
                {
                    sb.Append(c);
                }
                else
                {
                    sb.Append('_');
                }
            }

            // 移除连续的下划线并确保不以下划线结尾
            var result = sb.ToString();
            while (result.Contains("__"))
            {
                result = result.Replace("__", "_");
            }
            result = result.TrimEnd('_');

            // 确保结果不为空
            return string.IsNullOrEmpty(result) ? "Sheet" : result;
        }

        /// <summary>
        /// 解析类似 CSV 的字符串数组（支持引号包裹的逗号）
        /// </summary>
        private static List<string> ParseCsvStyleArray(string input)
        {
            if (string.IsNullOrEmpty(input)) return new List<string>();

            var result = new List<string>();
            var inQuotes = false;
            var currentItem = new StringBuilder();

            for (var i = 0; i < input.Length; i++)
            {
                char c = input[i];

                if (c == '"')
                {
                    // 检查是否是转义的引号（如 `""`）
                    if (i + 1 < input.Length && input[i + 1] == '"')
                    {
                        currentItem.Append('"');
                        i++; // 跳过下一个引号
                    }
                    else
                    {
                        inQuotes = !inQuotes; // 进入/退出引号模式
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    // 遇到逗号且不在引号内，分割当前元素
                    result.Add(currentItem.ToString().Trim());
                    currentItem.Clear();
                }
                else
                {
                    currentItem.Append(c);
                }
            }

            // 添加最后一个元素
            if (currentItem.Length > 0)
            {
                result.Add(currentItem.ToString().Trim());
            }

            return result;
        }

        /// <summary>
        /// 删除所有旧文件
        /// </summary>
        private static void DeleteAllOldFiles()
        {
            if (Directory.Exists(Cs_Output_Path)) Directory.Delete(Cs_Output_Path, true);
            if (Directory.Exists(Json_Output_Path)) Directory.Delete(Json_Output_Path, true);
            Directory.CreateDirectory(Cs_Output_Path);
            Directory.CreateDirectory(Json_Output_Path);
        }
    }
}
