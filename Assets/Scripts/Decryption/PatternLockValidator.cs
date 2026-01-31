using System.Collections.Generic;

/// <summary>
/// 九宫格图案校验逻辑（纯 C#）：用于提升可测试性与可维护性。
/// </summary>
public static class PatternLockValidator
{
    public static bool IsMatch(IReadOnlyList<int> input, IReadOnlyList<int> expected)
    {
        if (input == null || expected == null)
        {
            return false;
        }

        if (input.Count != expected.Count)
        {
            return false;
        }

        for (var i = 0; i < input.Count; i++)
        {
            if (input[i] != expected[i])
            {
                return false;
            }
        }

        return true;
    }
}
