using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.Importers;
using Utf8Json;

namespace UnityEditor.ShaderGraph
{
    [Serializable]
    // [JsonFormatter(typeof(UnityObjectFormatter<MaterialGraph>))]
    [JsonVersioned(typeof(MaterialGraph0))]
    public class MaterialGraph : AbstractMaterialGraph, IShaderGraph, IUpgradableFrom<MaterialGraph1>
    {
        public IMasterNode masterNode
        {
            get { return GetNodes<INode>().OfType<IMasterNode>().FirstOrDefault(); }
        }

        public string GetShader(string name, GenerationMode mode, out List<PropertyCollector.TextureInfo> configuredTextures, List<string> sourceAssetDependencyPaths = null)
        {
            return masterNode.GetShader(mode, name, out configuredTextures, sourceAssetDependencyPaths);
        }

        public void LoadedFromDisk()
        {
            OnEnable();
            ValidateGraph();
        }
    }

    public class MaterialGraph1 : IUpgradableFrom<MaterialGraph0>, IUpgradableTo<MaterialGraph>
    {
        public MaterialGraph Upgrade()
        {
            throw new NotImplementedException();
        }
    }

    public class MaterialGraph0 : IUpgradableTo<MaterialGraph1>
    {
        public MaterialGraph1 Upgrade()
        {
            throw new NotImplementedException();
        }
    }
}
