using System;
using System.Reflection;

using UnityEngine;

/// <summary>
/// 单例创建器：负责创建/查找项目内的单例实例。
/// </summary>
public static class SingletonCreator
{
    /// <summary>
    /// 单元测试模式标记：
    /// - 为 true 时允许在非 PlayMode 下创建 MonoSingleton，且不会 DontDestroyOnLoad。
    /// </summary>
    public static bool IsUnitTestMode { get; set; }

    public static T CreateSingleton<T>() where T : class, ISingleton
    {
        var type = typeof(T);
        if (typeof(MonoBehaviour).IsAssignableFrom(type))
        {
            return CreateMonoSingleton<T>();
        }

        var instance = CreateNonPublicConstructorObject<T>();
        instance.OnSingletonInit();
        return instance;
    }

    /// <summary>
    /// 创建/查找 MonoBehaviour 单例。
    /// 说明：会优先复用场景中已有实例（包含 inactive），避免重复创建导致的 UI/引用丢失问题。
    /// </summary>
    public static T CreateMonoSingleton<T>() where T : class, ISingleton
    {
        if (!IsUnitTestMode && !Application.isPlaying)
        {
            return null;
        }

        var type = typeof(T);

        // 判断当前场景中是否存在 T 实例（包含非激活对象）。
        var existing = UnityEngine.Object.FindObjectsByType(type, FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (existing != null && existing.Length > 0)
        {
            var selected = ChooseBestExisting(existing);
            if (existing.Length > 1)
            {
                Debug.LogWarning($"SingletonCreator: 场景中存在多个 {type.Name}，将使用 {selected.name}。");
            }

            var instance = selected as T;
            if (instance != null)
            {
                instance.OnSingletonInit();
                return instance;
            }
        }

        // 优先尝试按路径创建。
        foreach (var attribute in type.GetCustomAttributes(true))
        {
            if (attribute is not MonoSingletonPathAttribute pathAttr)
            {
                continue;
            }

            var instance = CreateComponentOnGameObject<T>(pathAttr.PathInHierarchy, dontDestroy: true);
            instance?.OnSingletonInit();
            return instance;
        }

        // 如果还是无法找到 instance，则创建同名 GameObject 并挂载组件。
        var obj = new GameObject(type.Name);
        if (!IsUnitTestMode)
        {
            UnityEngine.Object.DontDestroyOnLoad(obj);
        }

        var created = obj.AddComponent(type) as T;
        created?.OnSingletonInit();
        return created;
    }

    private static UnityEngine.Object ChooseBestExisting(UnityEngine.Object[] existing)
    {
        for (var i = 0; i < existing.Length; i++)
        {
            if (existing[i] is Component c && c.gameObject.activeInHierarchy)
            {
                return existing[i];
            }
        }

        return existing[0];
    }

    private static T CreateNonPublicConstructorObject<T>() where T : class
    {
        var type = typeof(T);
        var constructorInfos = type.GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic);
        var ctor = Array.Find(constructorInfos, c => c.GetParameters().Length == 0);

        if (ctor == null)
        {
            throw new Exception("Non-Public Constructor() not found! in " + type);
        }

        return ctor.Invoke(null) as T;
    }

    private static T CreateComponentOnGameObject<T>(string path, bool dontDestroy) where T : class
    {
        var obj = FindGameObject(path, build: true, dontDestroy: dontDestroy);
        if (obj == null)
        {
            obj = new GameObject("Singleton of " + typeof(T).Name);
            if (dontDestroy && !IsUnitTestMode)
            {
                UnityEngine.Object.DontDestroyOnLoad(obj);
            }
        }

        return obj.AddComponent(typeof(T)) as T;
    }

    private static GameObject FindGameObject(string path, bool build, bool dontDestroy)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        var subPath = path.Split('/');
        if (subPath.Length == 0)
        {
            return null;
        }

        return FindGameObject(null, subPath, index: 0, build: build, dontDestroy: dontDestroy);
    }

    private static GameObject FindGameObject(
        GameObject root,
        string[] subPath,
        int index,
        bool build,
        bool dontDestroy)
    {
        GameObject client;

        if (root == null)
        {
            client = GameObject.Find(subPath[index]);
        }
        else
        {
            var child = root.transform.Find(subPath[index]);
            client = child != null ? child.gameObject : null;
        }

        if (client == null && build)
        {
            client = new GameObject(subPath[index]);
            if (root != null)
            {
                client.transform.SetParent(root.transform);
            }

            if (dontDestroy && index == 0 && !IsUnitTestMode)
            {
                GameObject.DontDestroyOnLoad(client);
            }
        }

        if (client == null)
        {
            return null;
        }

        index++;
        return index == subPath.Length
            ? client
            : FindGameObject(client, subPath, index, build, dontDestroy);
    }
}
