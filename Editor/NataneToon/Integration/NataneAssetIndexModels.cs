using System;
using System.Collections.Generic;

namespace NataneToon.Editor
{
    [Serializable]
    public sealed class NataneAssetIndexData
    {
        public int version = 1;
        public long generatedAtUtcTicks;
        public List<MaterialIndexEntry> materials = new List<MaterialIndexEntry>();
        public List<PrefabDependencyEntry> prefabs = new List<PrefabDependencyEntry>();
    }

    [Serializable]
    public sealed class MaterialIndexEntry
    {
        public string guid;
        public string path;
        public string name;
        public string shaderName;
        public string keywordSetKey;
        public bool isNataneShader;
        public bool isLilToonShader;
        public bool usesLightVolume;
        public bool usesLtcgi;
    }

    [Serializable]
    public sealed class PrefabDependencyEntry
    {
        public string guid;
        public string path;
        public string name;
        public List<string> materialGuids = new List<string>();
    }

    [Serializable]
    public sealed class NataneShaderUsageManifest
    {
        public int version = 1;
        public long generatedAtUtcTicks;
        public List<ShaderUsageManifestEntry> shaders = new List<ShaderUsageManifestEntry>();
    }

    [Serializable]
    public sealed class ShaderUsageManifestEntry
    {
        public string shaderName;
        public List<string> keywordSetKeys = new List<string>();
    }

    [Serializable]
    public sealed class NataneBuildPreparationReport
    {
        public long generatedAtUtcTicks;
        public string buildMode;
        public bool indexRebuilt;
        public bool manifestRegenerated;
        public int pendingAssetsBefore;
        public int materialCount;
        public int prefabCount;
        public List<string> warnings = new List<string>();
        public List<string> errors = new List<string>();
    }
}
