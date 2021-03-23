using System;
using Nethermind.JsonRpc;
using Nethermind.JsonRpc.Data;
using Nethermind.JsonRpc.Modules;

namespace Zoltu.Nethermind.Plugin.Multicall
{
	[RpcModule((ModuleType)100)]
	public interface IMulticallModule : IModule
	{
		[JsonRpcMethod(IsImplemented = true, Description = "Executes all calls and returns true if they all succeeded, false if any failed.", IsSharable = false, Availability = RpcEndpoint.All)]
		ResultWrapper<CallResult[]> eth_multicall(TransactionForRpc[] transactions);
		public struct CallResult
		{
			public Byte StatusCode { get; set; }
			public Int64 GasSpent { get; set; }
			public Byte[] ReturnValue { get; set; }
			public String Error { get; set; }
		}
	}
}
