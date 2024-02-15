using System;
using System.Threading.Tasks;
using Nethermind.Api;
using Nethermind.Api.Extensions;
using Nethermind.Blockchain;
using Nethermind.Db;
using Nethermind.JsonRpc;
using Nethermind.JsonRpc.Modules;
using Nethermind.Logging;

namespace Zoltu.Nethermind.Plugin.Multicall
{
	public sealed class MulticallPlugin : DisposeAsyncOnce, INethermindPlugin
	{
		public String Name => "Multicall";
		public String Description => "Adds support for calling multiple transactions in sequence against shared state.";
		public String Author => "Micah";
		private INethermindApi? _nethermindApi;
		private ILogger? _logger;
		private IMulticallConfig? _config;

		// Don't remove default constructor. It is used by reflection when we're loading plugins
		public MulticallPlugin() { }

		public async Task Init(INethermindApi nethermindApi)
		{
			_nethermindApi = nethermindApi ?? throw new ArgumentNullException(nameof(nethermindApi));
			_logger = nethermindApi.LogManager.GetClassLogger() ?? throw new ArgumentNullException(nameof(_logger));
			_config = nethermindApi.Config<IMulticallConfig>() ?? throw new ArgumentNullException(nameof(_config));
			await Task.CompletedTask;
		}

		public Task InitNetworkProtocol() => Task.CompletedTask;

		public async Task InitRpcModules()
		{
			// if any of these aren null something is very wrong, fail hard and fast
			if (_nethermindApi == null || _logger == null || _config == null) throw new Exception($"InitRpcModules called on {Name} plugin before Init was called.");
			var jsonRpcConfig = _nethermindApi.Config<IJsonRpcConfig>() ?? throw new Exception($"JsonRpc configuration not found.");
			var worldStateManager = _nethermindApi.WorldStateManager ?? throw new Exception($"api.worldStateManager is null.");
			var blockTree = _nethermindApi.BlockTree?.AsReadOnly() ?? throw new Exception($"api.BlockTree is null.");
			var recoveryStep = _nethermindApi.BlockPreprocessor ?? throw new Exception($"api.BlockPreprocessor is null.");
			var rewardCalculatorSource = _nethermindApi.RewardCalculatorSource ?? throw new Exception($"api.RewardCalculatorSource is null.");
			var receiptFinder = _nethermindApi.ReceiptStorage ?? throw new Exception($"api.ReceiptStorage is null.");
			var specProvider = _nethermindApi.SpecProvider ?? throw new Exception($"api.SpecProvider is null.");
			var logManager = _nethermindApi.LogManager ?? throw new Exception($"api.LogManager is null.");
			var rpcModuleProvider = _nethermindApi.RpcModuleProvider ?? throw new Exception($"api.RpcModuleProvider is null.");

			try
			{
				if (_config.Enabled == false) throw new Exception($"{Name}.Enabled configuration variables set to false, halting initialization of {Name} plugin.");
				_logger.Info($"{Name} Plugin enabled, initializing...");
				var multiCallModuleFactory = new MulticallModuleFactory(worldStateManager, blockTree, jsonRpcConfig, recoveryStep, rewardCalculatorSource, receiptFinder, specProvider, logManager);
				rpcModuleProvider.RegisterBoundedByCpuCount(multiCallModuleFactory, jsonRpcConfig.Timeout);
				await Task.CompletedTask;
			}
			catch (Exception exception)
			{
				_logger.Warn(exception.Message);
			}
		}

		protected override async ValueTask DisposeOnce() => await ValueTask.CompletedTask;
	}
}
