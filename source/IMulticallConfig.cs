using System;
using Nethermind.Config;

namespace Zoltu.Nethermind.Plugin.Multicall
{
	public interface IMulticallConfig : IConfig
	{
		[ConfigItem(Description = "If 'true' then multicalls are enabled via JSON-RPC.", DefaultValue = "false")]
		public Boolean Enabled { get; set; }
		[ConfigItem(Description = "Address (as a hex string with or without leading 0x prefix) of the block author.", DefaultValue = "0x0000000000000000000000000000000000000000")]
		public String BlockProducer { get; set; }
	}
}
