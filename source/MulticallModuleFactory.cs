using Nethermind.Blockchain;
using Nethermind.Blockchain.Processing;
using Nethermind.Blockchain.Receipts;
using Nethermind.Blockchain.Rewards;
using Nethermind.Blockchain.Tracing;
using Nethermind.Blockchain.Validators;
using Nethermind.Core.Specs;
using Nethermind.Db;
using Nethermind.JsonRpc;
using Nethermind.JsonRpc.Modules;
using Nethermind.Logging;
using Nethermind.Trie.Pruning;

namespace Zoltu.Nethermind.Plugin.Multicall
{
	public sealed class MulticallModuleFactory : ModuleFactoryBase<IMulticallModule>
	{
		private readonly ReadOnlyDbProvider dbProvider;
		private readonly IBlockTree blockTree;
		private readonly IJsonRpcConfig jsonRpcConfig;
		private readonly IReadOnlyTrieStore trieNodeResolver;
		private readonly IBlockPreprocessorStep recoveryStep;
		private readonly IRewardCalculatorSource rewardCalculatorSource;
		private readonly IReceiptStorage receiptFinder;
		private readonly ISpecProvider specProvider;
		private readonly ILogManager logManager;
		public MulticallModuleFactory(IDbProvider dbProvider, IBlockTree blockTree, IJsonRpcConfig jsonRpcConfig, IReadOnlyTrieStore trieNodeResolver, IBlockPreprocessorStep recoveryStep, IRewardCalculatorSource rewardCalculatorSource, IReceiptStorage receiptFinder, ISpecProvider specProvider, ILogManager logManager)
		{
			this.dbProvider = dbProvider.AsReadOnly(false);
			this.blockTree = blockTree.AsReadOnly();
			this.jsonRpcConfig = jsonRpcConfig;
			this.trieNodeResolver = trieNodeResolver;
			this.recoveryStep = recoveryStep;
			this.rewardCalculatorSource = rewardCalculatorSource;
			this.receiptFinder = receiptFinder;
			this.specProvider = specProvider;
			this.logManager = logManager;
		}
		public override IMulticallModule Create()
		{
			var txProcessingEnv = new ReadOnlyTxProcessingEnv(dbProvider, trieNodeResolver, blockTree, specProvider, logManager);
			var rewardCalculator = rewardCalculatorSource.Get(txProcessingEnv.TransactionProcessor);
			var chainProcessingEnv = new ReadOnlyChainProcessingEnv(txProcessingEnv, Always.Valid, recoveryStep, rewardCalculator, receiptFinder, dbProvider, specProvider, logManager);
			var tracer = new Tracer(chainProcessingEnv.StateProvider, chainProcessingEnv.ChainProcessor);
			return new MulticallModule(tracer, blockTree, jsonRpcConfig);
		}
	}
}
