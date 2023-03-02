﻿using AssetStudio;
using Microsoft.Extensions.Logging;
using Server.Base.Core.Abstractions;
using Server.Base.Core.Events;
using Server.Base.Core.Extensions;
using Server.Base.Core.Services;
using Server.Base.Network.Enums;
using System.Xml;
using Web.AssetBundles.Assets.LocalAssets;
using Web.AssetBundles.Events;
using Web.AssetBundles.Events.Arguments;
using Web.AssetBundles.Extensions;
using Web.AssetBundles.Helpers;
using Web.AssetBundles.Models;
using Web.Launcher.Models;

namespace Web.AssetBundles.Services;

public class BuildAssetList : IService
{
    private readonly AssetEventSink _assetSink;
    private readonly AssetBundleRwConfig _rwConfig;
    private readonly ServerConsole _console;
    private readonly ILogger<BuildAssetList> _logger;
    private readonly AssetBundleRConfig _rConfig;
    private readonly EventSink _sink;
    private readonly LauncherRwConfig _launcherWConfig;

    public readonly Dictionary<string, string> AssetDict;

    public readonly Dictionary<string, string> PublishConfigs;

    public string AssetDictLocation;

    public Dictionary<string, InternalAssetInfo> InternalAssets;

    public BuildAssetList(ILogger<BuildAssetList> logger, AssetBundleRConfig rConfig,
        EventSink sink, AssetEventSink assetSink, ServerConsole console, LauncherRwConfig launcherWConfig,
        AssetBundleRwConfig rwConfig)
    {
        _logger = logger;
        _rConfig = rConfig;
        _sink = sink;
        _assetSink = assetSink;
        _console = console;
        _launcherWConfig = launcherWConfig;
        _rwConfig = rwConfig;

        PublishConfigs = new Dictionary<string, string>();
        AssetDict = new Dictionary<string, string>();
    }

    public void Initialize() => _sink.WorldLoad += Load;

    public void Load()
    {
        _console.AddCommand(
            "refreshCacheDir",
            "Force generates asset dictionary from default caches directory.",
            NetworkType.Server | NetworkType.Client,
            _ => GenerateDefaultAssetList(true)
        );

        _console.AddCommand(
            "changeCacheDir",
            "Change the default cache directory and regenerate dictionary.",
            NetworkType.Server | NetworkType.Client,
            _ =>
            {
                _rwConfig.CacheInfoFile = GetInfoFile.TryGetInfoFile("Original", string.Empty, _logger);
                GenerateDefaultAssetList(true);
            }
        );
        
        _rwConfig.CacheInfoFile = GetInfoFile.TryGetInfoFile("Original", _rwConfig.CacheInfoFile, _logger);

        if (!string.IsNullOrEmpty(_rwConfig.WebPlayerInfoFile))
            _rwConfig.WebPlayerInfoFile = _rwConfig.GetWebPlayerInfoFile(_rConfig, _logger);
        
        if (_rwConfig.FlushCacheOnStart)
            InternalDirectory.Empty(_rConfig.BundleSaveDirectory);

        AssetDictLocation = Path.Combine(_rConfig.AssetSaveDirectory, _rConfig.StoredAssetDict);

        GenerateDefaultAssetList(false);
    }

    private void GenerateDefaultAssetList(bool forceGenerate)
    {
        _logger.LogDebug("Getting asset dictionary");

        var dictExists = File.Exists(AssetDictLocation);

        var assets = !dictExists || forceGenerate
            ? GetAssetsFromCache(Path.GetDirectoryName(_rwConfig.CacheInfoFile))
            : GetAssetsFromDictionary(File.ReadAllText(AssetDictLocation));

        InternalAssets = assets.GetClosestBundles(_launcherWConfig);

        InternalAssets.AddModifiedAssets(_rConfig);
        InternalAssets.AddLocalXmlFiles(_logger, _rConfig);

        _logger.LogInformation("Loaded {Count} assets to memory.", InternalAssets.Count);

        foreach (var asset in InternalAssets.Values.Where(x => x.Type == AssetInfo.TypeAsset.Unknown))
            _logger.LogError("Could not find type for asset '{Name}' in '{File}'.", asset.Name, asset.Path);

        var vgmtAssets = InternalAssets.Where(x =>
                _rConfig.VirtualGoods.Any(a => string.Equals(a, x.Key) || x.Key.StartsWith($"{a}Dict_")))
            .ToDictionary(x => x.Key, x => x.Value);

        if (!vgmtAssets.Any())
            _logger.LogError("Could not find any virtual good assets! " +
                             "Try adding them into the LocalAsset directory. " +
                             "The game will not run without these.");

        var gameAssets = InternalAssets
            .Where(x => !vgmtAssets.ContainsKey(x.Key))
            .Select(x => x.Value)
            .ToArray();

        PublishConfigs.Clear();
        AssetDict.Clear();

        AddPublishConfiguration(gameAssets, _rConfig.PublishConfigKey);
        AddAssetDictionary(gameAssets, _rConfig.PublishConfigKey);

        AddPublishConfiguration(vgmtAssets.Values, _rConfig.PublishConfigVgmtKey);
        AddAssetDictionary(vgmtAssets.Values, _rConfig.PublishConfigVgmtKey);

        _logger.LogInformation("Generated default dictionaries.");

        _assetSink.InvokeAssetBundlesLoaded(new AssetBundleLoadEventArgs(InternalAssets));
    }

    private IEnumerable<InternalAssetInfo> GetAssetsFromCache(string directoryPath)
    {
        if (_rConfig.ShouldLogAssets)
            Logger.Default = new AssetBundleLogger(_logger);

        var assets = new List<InternalAssetInfo>();

        var directories = directoryPath.GetLowestDirectories();

        using var defaultBar = new DefaultProgressBar(directories.Count, _rConfig.Message, _logger, _rwConfig);

        foreach (var asset in directories.Select(directory => GetAssetBundle(directory, defaultBar)))
        {
            if (asset != null)
                assets.Add(asset);

            defaultBar.TickBar();
        }

        defaultBar.SetMessage($"Finished {_rConfig.Message}");

        SaveStoredAssets(assets.OrderAssets(), AssetDictLocation);

        return assets;
    }

    private InternalAssetInfo GetAssetBundle(string folderName, DefaultProgressBar bar)
    {
        if (Directory.GetFiles(folderName).Length > 2)
        {
            bar.SetMessage($"Directory {folderName} has more than one cache item, skipping!");
            return null;
        }

        var infoFile = Path.Join(folderName, "__info");

        if (!File.Exists(infoFile))
        {
            bar.SetMessage($"Could not find info file in {folderName}, skipping!");
            return null;
        }

        var text = File.ReadAllLines(infoFile);

        if (text.Length < 4)
        {
            bar.SetMessage(
                $"Info file for {Path.GetDirectoryName(infoFile)} has only {text.Length} lines of text, skipping!");
            return null;
        }

        var time = long.Parse(text[1]);
        var file = Path.Join(folderName, text[3]);

        if (!File.Exists(file))
        {
            bar.SetMessage(
                $"Asset bundle for {Path.GetDirectoryName(file)} does not exist, skipping!!");
            return null;
        }

        var manager = new AssetsManager();
        manager.LoadFiles(file);

        var assetFile = manager.assetsFileList.FirstOrDefault();

        if (assetFile == null)
        {
            bar.SetMessage($"Could not find asset in {folderName}, skipping!");
            return null;
        }

        var name = assetFile.GetMainAssetName(bar);

        if (string.IsNullOrEmpty(name))
        {
            bar.SetMessage($"Could not find asset name in {folderName}, skipping!");
            return null;
        }

        var asset = new InternalAssetInfo
        {
            Name = name,
            Path = assetFile.fullName,
            CacheTime = time,
            Version = 0,
            Type = AssetInfo.TypeAsset.Unknown,
            BundleSize = Convert.ToInt32(new FileInfo(assetFile.fullName).Length / 1024),
            Locale = RFC1766Locales.LanguageCodes.en_us,
            UnityVersion = assetFile.unityVersion
        };

        var gameObj = assetFile.ObjectsDic.Values.ToArray().GetGameObject(asset.Name)?.m_Name;
        var musicObj = assetFile.ObjectsDic.Values.ToArray().GetMusic(asset.Name)?.m_Name;
        var textObj = assetFile.ObjectsDic.Values.ToArray().GetText(asset.Name)?.m_Name;

        if (!string.IsNullOrEmpty(gameObj))
        {
            asset.Name = gameObj;

            if (asset.Name.StartsWith("LV"))
                if (!asset.Name.Contains("mesh") && !asset.Name.Contains("plane"))
                {
                    asset.Type = AssetInfo.TypeAsset.Level;
                    bar.SetMessage(
                        $"{_rConfig.Message} - found possible level '{asset.Name}' in {assetFile.fileName.Split('/').Last()}");
                }

            if (asset.Type == AssetInfo.TypeAsset.Unknown)
                asset.Type = AssetInfo.TypeAsset.Prefab;
        }
        else if (!string.IsNullOrEmpty(musicObj))
        {
            asset.Name = musicObj;
            asset.Type = AssetInfo.TypeAsset.Audio;
        }
        else if (!string.IsNullOrEmpty(textObj))
        {
            asset.Name = textObj;

            if (asset.Name.StartsWith("NavMesh"))
            {
                asset.Type = AssetInfo.TypeAsset.NavMesh;
            }
            else
            {
                bar.SetMessage(
                    $"{_rConfig.Message} - found possible XML '{asset.Name}' in {assetFile.fileName.Split('/').Last()}");

                if (Enum.TryParse<RFC1766Locales.LanguageCodes>(
                        asset.Name.Split('_').Last().Replace('-', '_'),
                        true,
                        out var type)
                   )
                    asset.Locale = type;

                asset.Type = AssetInfo.TypeAsset.XML;
            }
        }

        if (asset.Type == AssetInfo.TypeAsset.Unknown)
            bar.SetMessage($"{_rConfig.Message} - WARNING: could not find type of asset {asset.Name}");

        return asset;
    }

    private void AddPublishConfiguration(IEnumerable<InternalAssetInfo> assets, string key)
    {
        var document = new XmlDocument();
        var root = document.CreateElement("PublishConfiguration");

        var xmlElements = document.CreateElement("xml_version");

        foreach (var asset in assets.Where(x => x.Type == AssetInfo.TypeAsset.XML))
            xmlElements.AppendChild(asset.ToAssetXml("item", document));

        root.AppendChild(xmlElements);

        var dict = document.CreateElement("item");
        dict.SetAttribute("name", _rConfig.AssetDictKey);
        dict.SetAttribute("value", _rConfig.AssetDictConfigs[key]);
        root.AppendChild(dict);

        document.AppendChild(root);

        var config = document.WriteToString();
        File.WriteAllText(Path.Combine(_rConfig.AssetSaveDirectory, _rConfig.PublishConfigs[key]), config);
        PublishConfigs.Add(key, config);
    }

    private void AddAssetDictionary(IEnumerable<InternalAssetInfo> assets, string key)
    {
        var document = new XmlDocument();
        var root = document.CreateElement("assets");

        foreach (var asset in assets)
            root.AppendChild(asset.ToPubXml("asset", document));

        document.AppendChild(root);

        var assetDict = document.WriteToString();
        File.WriteAllText(Path.Combine(_rConfig.AssetSaveDirectory, _rConfig.AssetDictConfigs[key]), assetDict);
        AssetDict.Add(key, assetDict);
    }

    private static void SaveStoredAssets(IEnumerable<InternalAssetInfo> assets, string saveDir)
    {
        var document = new XmlDocument();
        var root = document.CreateElement("assets");

        foreach (var asset in assets)
            root.AppendChild(asset.ToStoredXml("asset", document));

        document.AppendChild(root);

        File.WriteAllText(saveDir, document.WriteToString());
    }

    public static IEnumerable<InternalAssetInfo> GetAssetsFromDictionary(string xml)
    {
        var configuration = new List<InternalAssetInfo>();

        var document = new XmlDocument();
        document.LoadXml(xml);

        if (document.DocumentElement == null)
            return configuration;

        foreach (XmlNode node in document.DocumentElement.ChildNodes)
        {
            if (node is not XmlElement assetElement)
                continue;

            configuration.Add(assetElement.XmlToAsset());
        }

        return configuration;
    }
}
