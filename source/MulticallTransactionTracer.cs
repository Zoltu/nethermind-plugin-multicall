using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Evm;
using Nethermind.Evm.Tracing;
using Nethermind.Int256;

namespace Zoltu.Nethermind.Plugin.Multicall
{
	public sealed class MulticallTransactionTracer : ITxTracer
	{
		public Boolean IsTracing => true;
		public Boolean IsTracingReceipt => true;
		public Boolean IsTracingState => true;
		public Boolean IsTracingAccess => false;
		public Boolean IsTracingActions => true;
		public Boolean IsTracingOpLevelStorage => false;
		public Boolean IsTracingMemory => false;
		public Boolean IsTracingInstructions => false;
		public Boolean IsTracingRefunds => false;
		public Boolean IsTracingCode => false;
		public Boolean IsTracingStack => false;
		public Boolean IsTracingBlockHash => false;
		public Boolean IsTracingStorage => false;
		public Boolean IsTracingFees => false;

		public ImmutableList<BalanceChange> BalanceChanges { get; private set; } = ImmutableList<BalanceChange>.Empty;
		public ImmutableArray<LogEntry> Events { get; private set; } = ImmutableArray<LogEntry>.Empty;
		public Byte[] ReturnValue { get; private set; } = Array.Empty<Byte>();
		public Int64 GasSpent { get; private set; }
		public String? Error { get; private set; }
		public Byte StatusCode { get; private set; }

		public void MarkAsSuccess(Address recipient, long gasSpent, byte[] output, LogEntry[] logs, Keccak? stateRoot = null)
		{
			this.GasSpent = gasSpent;
			this.ReturnValue = output;
			this.StatusCode = global::Nethermind.Evm.StatusCode.Success;
			this.Events = logs.ToImmutableArray();
		}

		public void MarkAsFailed(Address recipient, long gasSpent, byte[] output, string error, Keccak? stateRoot = null)
		{
			this.GasSpent = gasSpent;
			this.Error = error;
			this.ReturnValue = output;
			this.StatusCode = global::Nethermind.Evm.StatusCode.Failure;
		}

		public void ReportBalanceChange(Address address, UInt256? before, UInt256? after)
		{
			this.BalanceChanges = this.BalanceChanges.Add(new BalanceChange() { Address = address, Before = before ?? UInt256.Zero, After = after ?? UInt256.Zero });
		}

		public void StartOperation(Int32 depth, Int64 gas, Instruction opcode, Int32 pc, Boolean isPostMerge = false) { }
		public void LoadOperationStorage(Address address, UInt256 storageIndex, ReadOnlySpan<byte> value) { }
		public void ReportAccess(IReadOnlySet<Address> accessedAddresses, IReadOnlySet<StorageCell> accessedStorageCells) { }
		public void ReportAccountRead(Address address) { }
		public void ReportAction(Int64 gas, UInt256 value, Address from, Address? to, ReadOnlyMemory<Byte> input, ExecutionType callType, Boolean isPrecompileCall = false) { }
		public void ReportActionEnd(Int64 gas, ReadOnlyMemory<Byte> output) { }
		public void ReportActionEnd(Int64 gas, Address deploymentAddress, ReadOnlyMemory<Byte> deployedCode) { }
		public void ReportActionError(EvmExceptionType evmExceptionType) { }
		public void ReportBlockHash(Keccak blockHash) { }
		public void ReportByteCode(Byte[] byteCode) { }
		public void ReportCodeChange(Address address, Byte[]? before, Byte[]? after) { }
		public void ReportExtraGasPressure(Int64 extraGasPressure) { }
		public void ReportFees(UInt256 fees, UInt256 burntFees) { }
		public void ReportGasUpdateForVmTrace(Int64 refund, Int64 gasAvailable) { }
		public void ReportMemoryChange(Int64 offset, in ReadOnlySpan<Byte> data) { }
		public void ReportNonceChange(Address address, UInt256? before, UInt256? after) { }
		public void ReportOperationError(EvmExceptionType error) { }
		public void ReportOperationRemainingGas(Int64 gas) { }
		public void ReportRefund(Int64 refund) { }
		public void ReportSelfDestruct(Address address, UInt256 balance, Address refundAddress) { }
		public void ReportStackPush(in ReadOnlySpan<Byte> stackItem) { }
		public void ReportStorageChange(in ReadOnlySpan<Byte> key, in ReadOnlySpan<Byte> value) { }
		public void ReportStorageChange(in StorageCell storageCell, Byte[] before, Byte[] after) { }
		public void ReportStorageRead(in StorageCell storageCell) { }
		public void SetOperationMemory(List<String> memoryTrace) { }
		public void SetOperationMemorySize(UInt64 newSize) { }
		public void SetOperationStack(List<String> stackTrace) { }
		public void SetOperationStorage(Address address, UInt256 storageIndex, ReadOnlySpan<Byte> newValue, ReadOnlySpan<Byte> currentValue) { }
		public void StartOperation(Int32 depth, Int64 gas, Instruction opcode, Int32 pc) { }
	}
}
