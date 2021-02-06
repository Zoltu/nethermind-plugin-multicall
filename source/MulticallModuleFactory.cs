using Nethermind.Blockchain;
using Nethermind.Blockchain.Processing;
using Nethermind.JsonRpc;
using Nethermind.JsonRpc.Modules;

namespace Zoltu.Nethermind.Plugin.Multicall
{
	public sealed class MulticallModuleFactory : ModuleFactoryBase<IMulticallModule>
	{
		private readonly IBlockProcessor blockProcessor;
		private readonly IBlockTree blockTree;
		private readonly IJsonRpcConfig jsonRpcConfig;
		public MulticallModuleFactory(IBlockProcessor blockProcessor, IBlockTree blockTree, IJsonRpcConfig jsonRpcConfig)
		{
			this.blockProcessor = blockProcessor;
			this.blockTree = blockTree;
			this.jsonRpcConfig = jsonRpcConfig;
		}
		public override IMulticallModule Create() => new MulticallModule(blockProcessor, blockTree, jsonRpcConfig);
	}
}
