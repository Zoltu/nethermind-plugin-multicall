using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Nethermind.Blockchain;
using Nethermind.Blockchain.Processing;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Evm.Tracing;
using Nethermind.Int256;
using Nethermind.JsonRpc;
using Nethermind.JsonRpc.Data;
using static Zoltu.Nethermind.Plugin.Multicall.IMulticallModule;

namespace Zoltu.Nethermind.Plugin.Multicall
{
	public sealed class MulticallModule : IMulticallModule
	{
		private readonly IBlockProcessor blockProcessor;
		private readonly IBlockTree blockTree;
		private readonly IJsonRpcConfig jsonRpcConfig;
		public MulticallModule(IBlockProcessor blockProcessor, IBlockTree blockTree, IJsonRpcConfig jsonRpcConfig)
		{
			this.blockProcessor = blockProcessor;
			this.blockTree = blockTree;
			this.jsonRpcConfig = jsonRpcConfig;
		}
		public ResultWrapper<CallResult[]> eth_multicall(TransactionForRpc[] transactions)
		{
			var blockHeader = new BlockHeader(blockTree.Head.Hash!, Keccak.EmptyTreeHash, Address.Zero, blockTree.Head.Difficulty, blockTree.Head.Number + 1, blockTree.Head.GasLimit, blockTree.Head.Timestamp + 1, Array.Empty<Byte>());
			var block = new Block(blockHeader, transactions.Select(x => x.ToTransaction()), Enumerable.Empty<BlockHeader>());
			var cancellationToken = new CancellationTokenSource(jsonRpcConfig.Timeout).Token;
			var blockTracer = new MyBlockTracer(cancellationToken);
			_ = blockProcessor.Process(blockTree.Head.StateRoot!, new List<Block> { block }, ProcessingOptions.NoValidation | ProcessingOptions.ReadOnlyChain, new CancellationBlockTracer(blockTracer, cancellationToken));
			return ResultWrapper<CallResult[]>.Success(blockTracer.Results.ToArray());
		}

		private class MyBlockTracer : IBlockTracer
		{
			public Boolean IsTracingRewards => false;

			private CallOutputTracer? txTracer;
			// TODO: figure out if this needs to be concurrency safe, or if it can just be a List
			private readonly ConcurrentStack<CallResult> results = new();
			public CallResult[] Results => results.ToArray();
			private readonly CancellationToken cancellationToken;

			public MyBlockTracer(CancellationToken cancellationToken)
			{
				this.cancellationToken = cancellationToken;
			}

			public void ReportReward(Address author, String rewardType, UInt256 rewardValue) { }
			public void StartNewBlockTrace(Block block) => results.Clear();
			public ITxTracer StartNewTxTrace(Keccak txHash) => new CancellationTxTracer(txTracer = new CallOutputTracer(), cancellationToken);
			public void EndTxTrace()
			{
				if (txTracer == null) return;
				var callResult = new CallResult() { StatusCode = txTracer.StatusCode, GasSpent = txTracer.GasSpent, ReturnValue = txTracer.ReturnValue, Error = txTracer.Error };
				results.Push(callResult);
				txTracer = null;
			}
		}
	}
}
