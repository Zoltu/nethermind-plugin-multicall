using System;
using Nethermind.Config;

namespace Zoltu.Nethermind.Plugin.Multicall
{
	public interface IMulticallConfig : IConfig
	{
		[ConfigItem(Description = "If 'true' then multicalls are enabled via JSON-RPC.", DefaultValue = "false")]
		public Boolean Enabled { get; set; }
	}
}
