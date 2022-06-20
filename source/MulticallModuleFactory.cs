using System;
using Nethermind.Blockchain;
using Nethermind.Blockchain.Receipts;
using Nethermind.Consensus.Processing;
using Nethermind.Consensus.Rewards;
using Nethermind.Consensus.Tracing;
using Nethermind.Consensus.Validators;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Core.Specs;
using Nethermind.Db;
using Nethermind.Evm.Tracing;
using Nethermind.JsonRpc;
using Nethermind.JsonRpc.Modules;
using Nethermind.Logging;
using Nethermind.State;
using Nethermind.Trie;
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
			var tracer = new MyTracer(chainProcessingEnv.StateProvider, chainProcessingEnv.ChainProcessor);
			return new MulticallModule(tracer, blockTree, jsonRpcConfig);
		}
	}

	// ripped from Nethermind codebase so we can enable nonce checking, since processing options isn't exposed
	public class MyTracer : ITracer
	{
		private readonly IStateProvider _stateProvider;
		private readonly IBlockchainProcessor _blockProcessor;

		public MyTracer(IStateProvider stateProvider, IBlockchainProcessor blockProcessor)
		{
			_stateProvider = stateProvider ?? throw new ArgumentNullException(nameof(stateProvider));
			_blockProcessor = blockProcessor ?? throw new ArgumentNullException(nameof(blockProcessor));
		}

		public Block? Trace(Block block, IBlockTracer blockTracer)
		{
			try
			{
				blockTracer.StartNewBlockTrace(block);
				/* We force process since we want to process a block that has already been processed in the past and normally it would be ignored.
				We also want to make it read only so the state is not modified persistently in any way. */
				Block? processedBlock = _blockProcessor.Process(block, ProcessingOptions.ForceProcessing | ProcessingOptions.ReadOnlyChain | ProcessingOptions.NoValidation, blockTracer);
				blockTracer.EndBlockTrace();
				return processedBlock;
			}
			catch (Exception)
			{
				_stateProvider.Reset();
				throw;
			}
		}

		public void Accept(ITreeVisitor visitor, Keccak stateRoot)
		{
			if (visitor == null) throw new ArgumentNullException(nameof(visitor));
			if (stateRoot == null) throw new ArgumentNullException(nameof(stateRoot));

			_stateProvider.Accept(visitor, stateRoot);
		}
	}
}
