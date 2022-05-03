using Nethermind.Core;
using Nethermind.Int256;

namespace Zoltu.Nethermind.Plugin.Multicall
{
	public struct BalanceChange
	{
		public Address Address;
		public UInt256 Before;
		public UInt256 After;
	}
}
