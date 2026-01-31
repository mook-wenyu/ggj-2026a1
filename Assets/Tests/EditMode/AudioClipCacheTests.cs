using System;

using NUnit.Framework;
using UnityEngine;

public sealed class AudioClipCacheTests
{
    [Test]
    public void TryGet_WhenPathIsWhitespace_ReturnsFalse()
    {
        var cache = new AudioClipCache(_ => null);
        Assert.IsFalse(cache.TryGet("   ", out _));
    }

    [Test]
    public void TryGet_TrimsPath_BeforeLoading()
    {
        string receivedPath = null;
        var clip = AudioClip.Create("c", 4410, 1, 44100, false);
        var cache = new AudioClipCache(path =>
        {
            receivedPath = path;
            return clip;
        });

        Assert.IsTrue(cache.TryGet("  Audios/Voice/item_test  ", out var loaded));
        Assert.AreSame(clip, loaded);
        Assert.AreEqual("Audios/Voice/item_test", receivedPath);
    }

    [Test]
    public void TryGet_CachesNullResult()
    {
        var calls = 0;
        var cache = new AudioClipCache(_ =>
        {
            calls++;
            return null;
        });

        Assert.IsFalse(cache.TryGet("Audios/Missing", out _));
        Assert.IsFalse(cache.TryGet("Audios/Missing", out _));
        Assert.AreEqual(1, calls);
    }
}
