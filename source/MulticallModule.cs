using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Nethermind.Blockchain;
using Nethermind.Blockchain.Tracing;
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
		private readonly IBlockTree blockTree;
		private readonly IJsonRpcConfig jsonRpcConfig;
		private readonly ITracer tracer;
		public MulticallModule(ITracer tracer, IBlockTree blockTree, IJsonRpcConfig jsonRpcConfig)
		{
			this.tracer = tracer;
			this.blockTree = blockTree;
			this.jsonRpcConfig = jsonRpcConfig;
		}
		public ResultWrapper<CallResult[]> eth_multicall(TransactionForRpc[] transactions)
		{
			var headBlock = blockTree.Head!;
			var blockHeader = new BlockHeader(headBlock.Hash!, Keccak.EmptyTreeHash, Address.Zero, headBlock.Difficulty, headBlock.Number + 1, headBlock.GasLimit, headBlock.Timestamp + 1, Array.Empty<Byte>());
			var block = new Block(blockHeader, transactions.Select(x => x.ToTransaction()), Enumerable.Empty<BlockHeader>());
			var cancellationToken = new CancellationTokenSource(jsonRpcConfig.Timeout).Token;
			var blockTracer = new MyBlockTracer(cancellationToken);
			var postTraceStateRoot = this.tracer.Trace(block, blockTracer);
			return ResultWrapper<CallResult[]>.Success(blockTracer.Results.ToArray());
		}

		private class MyBlockTracer : IBlockTracer
		{
			public Boolean IsTracingRewards => false;

			private CallOutputTracer? txTracer;
			private ImmutableArray<CallResult> results = ImmutableArray<CallResult>.Empty;
			public CallResult[] Results => results.ToArray();
			private readonly CancellationToken cancellationToken;

			public MyBlockTracer(CancellationToken cancellationToken)
			{
				this.cancellationToken = cancellationToken;
			}

			public void ReportReward(Address author, String rewardType, UInt256 rewardValue) { }
			public void StartNewBlockTrace(Block block) => results.Clear();
			public ITxTracer StartNewTxTrace(Keccak? txHash) => new CancellationTxTracer(txTracer = new CallOutputTracer(), cancellationToken);
			public void EndTxTrace()
			{
				if (txTracer == null) return;
				var callResult = new CallResult() { StatusCode = txTracer.StatusCode, GasSpent = txTracer.GasSpent, ReturnValue = txTracer.ReturnValue, Error = txTracer.Error };
				results = results.Add(callResult);
				txTracer = null;
			}
		}
	}
}
