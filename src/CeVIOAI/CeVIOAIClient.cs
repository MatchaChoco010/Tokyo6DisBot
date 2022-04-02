using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NMeCab.Specialized;

public sealed class CeVIOAIClient : IDisposable
{
    readonly SemaphoreSlim _semaphore = new(1, 1);

    readonly Type _serviceControl2Type;
    readonly MeCabIpaDicTagger _tagger;
    readonly dynamic _talker;

    CancellationTokenSource? _nowSpeakingTaskCancellationTokenSource;

    public CeVIOAIClient()
    {
        var cevioAiPath = Environment.ExpandEnvironmentVariables("%ProgramW6432%") +
            @"\CeVIO\CeVIO AI\CeVIO.Talk.RemoteService2.dll";

        Assembly asm = Assembly.LoadFrom(cevioAiPath);

        _serviceControl2Type = asm.GetType("CeVIO.Talk.RemoteService2.ServiceControl2")!;

        var startHostMethod =
            _serviceControl2Type.GetMethod("StartHost", BindingFlags.Static | BindingFlags.Public)!;
        startHostMethod.Invoke(null, new object?[] { false });

        var talkerAgent2Type = asm.GetType("CeVIO.Talk.RemoteService2.TalkerAgent2")!;
        var avilableCastsProp =
            talkerAgent2Type.GetProperty("AvailableCasts", BindingFlags.Static | BindingFlags.Public)!;
        dynamic availableCasts = avilableCastsProp.GetValue(null, null)!;

        var talker2Type = asm.GetType("CeVIO.Talk.RemoteService2.Talker2")!;
        var talker2Constructor = talker2Type.GetConstructor(new[] { typeof(string) })!;
        _talker = talker2Constructor.Invoke(new object?[] { "" });

        _tagger = MeCabIpaDicTagger.Create();

        Directory.CreateDirectory(WavDirectoryPath);
    }

    static string WavDirectoryPath
    {
        get
        {
            var exePath = Assembly.GetEntryAssembly()?.Location ?? "";
            var wavDirectoryPath = Path.Combine(Path.GetDirectoryName(exePath) ?? "", "audio");
            return wavDirectoryPath;
        }
    }

    /// <summary>
    ///     声量 0~100
    /// </summary>
    public uint Volume
    {
        get => _talker.Volume;
        set => _talker.Volume = value;
    }

    /// <summary>
    ///     話す速さ 0~100
    /// </summary>
    public uint Speed
    {
        get => _talker.Speed;
        set => _talker.Speed = value;
    }

    /// <summary>
    ///     声の高さ 0~100
    /// </summary>
    public uint Tone
    {
        get => _talker.Tone;
        set => _talker.Tone = value;
    }

    /// <summary>
    ///     声質 0~100
    /// </summary>
    public uint Alpha
    {
        get => _talker.Alpha;
        set => _talker.Alpha = value;
    }

    /// <summary>
    ///     抑揚 0~100
    /// </summary>
    public uint ToneScale
    {
        get => _talker.ToneScale;
        set => _talker.ToneScale = value;
    }

    public void Dispose()
    {
        var closeHostMethod =
            _serviceControl2Type.GetMethod("CloseHost", BindingFlags.Static | BindingFlags.Public)!;
        closeHostMethod.Invoke(null, new object[] { 0 });
        _tagger.Dispose();
        _semaphore.Dispose();
    }

    /// <summary>
    ///     index 0: 嬉しい 0~100
    ///     index 1: 普通 0~100
    ///     index 2: 怒り 0~100
    ///     index 3: 悲しみ 0~100
    ///     index 4: 落ち着き 0~100
    /// </summary>
    public uint GetComponent(int index)
    {
        return _talker.Components[index].Value;
    }

    /// <summary>
    ///     index 0: 嬉しい 0~100
    ///     index 1: 普通 0~100
    ///     index 2: 怒り 0~100
    ///     index 3: 悲しみ 0~100
    ///     index 4: 落ち着き 0~100
    /// </summary>
    public void SetComponent(int index, uint value)
    {
        _talker.Components[index].Value = value;
    }

    public async Task<string[]> OutputWavFiles(string castName, string text,
        CancellationToken cancellationToken = new())
    {
        // マルチスレッドでCeVIO AIが死ぬので、内部でCeVIO AIのAPIを叩くのは同時に1スレッドまでに制限している。
        // 発話中に次の発話リクエストが来た場合はキャンセルを飛ばしてから次の発話を実行している。

        _nowSpeakingTaskCancellationTokenSource?.Cancel(true);
        _nowSpeakingTaskCancellationTokenSource =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _nowSpeakingTaskCancellationTokenSource.Token;
        var pathList = new List<string>();

        try
        {
            await _semaphore.WaitAsync(token);

            _talker.Cast = castName;

            foreach (var line in text.Split('\n'))
            {
                var nodes = _tagger.Parse(line);
                var speakingText = "";
                foreach (var node in nodes)
                {
                    // CeVIO AI Talkは100文字以上連続して読み上げできないので適当な文節で区切る
                    if (speakingText.Length + node.Surface.Length >= 100)
                    {
                        var wavFileName = $"{Guid.NewGuid()}.wav";
                        var wavFilePath = Path.Combine(WavDirectoryPath, wavFileName);
                        var result = _talker.OutputWaveToFile(text, wavFilePath);
                        if (!result)
                        {
                            return pathList.ToArray();
                        }
                        else
                        {
                            pathList.Add(wavFilePath);
                        }

                        speakingText = "";
                    }

                    speakingText += node.Surface;
                }

                {
                    var wavFileName = $"{Guid.NewGuid()}.wav";
                    var wavFilePath = Path.Combine(WavDirectoryPath, wavFileName);
                    var result = _talker.OutputWaveToFile(text, wavFilePath);
                    if (!result)
                    {
                        return pathList.ToArray();
                    }
                    else
                    {
                        pathList.Add(wavFilePath);
                    }
                }
            }
        }
        finally
        {
            _semaphore.Release();
        }

        _nowSpeakingTaskCancellationTokenSource = null;

        return pathList.ToArray();
    }
}
