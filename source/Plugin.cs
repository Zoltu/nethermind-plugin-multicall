using System;
using System.Threading.Tasks;
using Nethermind.Api;
using Nethermind.Api.Extensions;
using Nethermind.Blockchain;
using Nethermind.JsonRpc;
using Nethermind.JsonRpc.Modules;
using Nethermind.Logging;

namespace Zoltu.Nethermind.Plugin.Multicall
{
	public sealed class Plugin : DisposeSyncOnce, INethermindPlugin
	{
		public String Name => "Multicall";
		public String Description => "Adds support for calling multiple transactions in sequence against shared state.";
		public String Author => "Micah";
		private INethermindApi? _nethermindApi;
		private ILogger? _logger;
		private IMulticallConfig? _config;
		private readonly Int32 _cpuCount = Environment.ProcessorCount;

		public async Task Init(INethermindApi nethermindApi)
		{
			_nethermindApi = nethermindApi;
			_logger = nethermindApi.LogManager.GetClassLogger();
			_config = nethermindApi.Config<IMulticallConfig>();
			await Task.CompletedTask;
		}

		public Task InitNetworkProtocol() => Task.CompletedTask;

		public async Task InitRpcModules()
		{
			// if any of these aren null something is very wrong, fail hard and fast
			if (_nethermindApi == null || _logger == null || _config == null) throw new Exception($"InitRpcModules called on {Name} plugin before Init was called.");
			var jsonRpcConfig = _nethermindApi.Config<IJsonRpcConfig>() ?? throw new Exception($"JsonRpc configuration not found.");
			var blockProcessor = _nethermindApi.MainBlockProcessor ?? throw new Exception($"api.MainBlockProcessor is null.");
			var blockTree = _nethermindApi.BlockTree?.AsReadOnly() ?? throw new Exception($"api.BlockTree is null.");

			try
			{
				if (_config.Enabled == false) throw new Exception($"{Name}.Enabled configuration variables set to false, halting initialization of {Name} plugin.");
				_logger.Info($"{Name} Plugin enabled, initializing...");
				var multiCallModuleFactory = new MulticallModuleFactory(blockProcessor, blockTree, jsonRpcConfig);
				_nethermindApi.RpcModuleProvider.Register(new BoundedModulePool<IMulticallModule>(multiCallModuleFactory, _cpuCount, jsonRpcConfig.Timeout));
				await Task.CompletedTask;
			}
			catch (Exception exception)
			{
				_logger.Warn(exception.Message);
			}
		}

		protected override void DisposeOnce() { }
	}
}
