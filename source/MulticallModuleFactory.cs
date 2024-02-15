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
		private readonly IWorldStateManager worldStateManager;
		private readonly IBlockTree blockTree;
		private readonly IJsonRpcConfig jsonRpcConfig;
		private readonly IBlockPreprocessorStep recoveryStep;
		private readonly IRewardCalculatorSource rewardCalculatorSource;
		private readonly IReceiptStorage receiptFinder;
		private readonly ISpecProvider specProvider;
		private readonly ILogManager logManager;

		public MulticallModuleFactory(IWorldStateManager worldStateManager, IBlockTree blockTree, IJsonRpcConfig jsonRpcConfig, IBlockPreprocessorStep recoveryStep, IRewardCalculatorSource rewardCalculatorSource, IReceiptStorage receiptFinder, ISpecProvider specProvider, ILogManager logManager)
		{
			this.worldStateManager = worldStateManager;
			this.blockTree = blockTree.AsReadOnly();
			this.jsonRpcConfig = jsonRpcConfig;
			this.recoveryStep = recoveryStep;
			this.rewardCalculatorSource = rewardCalculatorSource;
			this.receiptFinder = receiptFinder;
			this.specProvider = specProvider;
			this.logManager = logManager;
		}
		public override IMulticallModule Create()
		{
			var txProcessingEnv = new ReadOnlyTxProcessingEnv(worldStateManager, blockTree, specProvider, logManager);
			var rewardCalculator = rewardCalculatorSource.Get(txProcessingEnv.TransactionProcessor);
			var chainProcessingEnv = new ReadOnlyChainProcessingEnv(txProcessingEnv, Always.Valid, recoveryStep, rewardCalculator, receiptFinder, specProvider, logManager);
			var tracer = new MyTracer(chainProcessingEnv.StateProvider, chainProcessingEnv.ChainProcessor, chainProcessingEnv.ChainProcessor);
			return new MulticallModule(tracer, blockTree, jsonRpcConfig);
		}
	}

	// ripped from Nethermind codebase so we can enable nonce checking, since processing options isn't exposed
	public class MyTracer : ITracer
	{
		private readonly IWorldState _stateProvider;
		private readonly IBlockchainProcessor _traceProcessor;
		private readonly IBlockchainProcessor _executeProcessor;

		public MyTracer(IWorldState stateProvider, IBlockchainProcessor traceProcessor, IBlockchainProcessor executeProcessor)
		{
			_stateProvider = stateProvider ?? throw new ArgumentNullException(nameof(stateProvider));
			_traceProcessor = traceProcessor ?? throw new ArgumentNullException(nameof(traceProcessor));
			_executeProcessor = executeProcessor ?? throw new ArgumentNullException(nameof(executeProcessor));
		}

		public void Trace(Block block, IBlockTracer blockTracer) => Process(block, blockTracer, _traceProcessor);
		public void Execute(Block block, IBlockTracer tracer) => Process(block, tracer, _executeProcessor);

		public void Accept(ITreeVisitor visitor, Hash256 stateRoot)
		{
			if (visitor == null) throw new ArgumentNullException(nameof(visitor));
			if (stateRoot == null) throw new ArgumentNullException(nameof(stateRoot));

			_stateProvider.Accept(visitor, stateRoot);
		}

		private void Process(Block block, IBlockTracer blockTracer, IBlockchainProcessor processor)
		{
			/* We force process since we want to process a block that has already been processed in the past and normally it would be ignored.
			We also want to make it read only so the state is not modified persistently in any way. */

			blockTracer.StartNewBlockTrace(block);

			try
			{
				processor.Process(block, ProcessingOptions.ProducingBlock, blockTracer);
			}
			catch (Exception)
			{
				_stateProvider.Reset();
				throw;
			}

			blockTracer.EndBlockTrace();
		}
	}
}
