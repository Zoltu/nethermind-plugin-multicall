using System;
using System.Threading.Tasks;
using Nethermind.Api;
using Nethermind.Api.Extensions;
using Nethermind.Blockchain;
using Nethermind.Core;
using Nethermind.Db;
using Nethermind.JsonRpc;
using Nethermind.JsonRpc.Modules;
using Nethermind.Logging;

namespace Zoltu.Nethermind.Plugin.Multicall
{
	public sealed class Plugin : DisposeAsyncOnce, INethermindPlugin
	{
		public String Name => "Multicall";
		public String Description => "Adds support for calling multiple transactions in sequence against shared state.";
		public String Author => "Micah";
		private INethermindApi? _nethermindApi;
		private ILogger? _logger;
		private IMulticallConfig? _config;

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
			var dbProvider = _nethermindApi.DbProvider?.AsReadOnly(false) ?? throw new Exception($"api.DbProvider is null.");
			var blockTree = _nethermindApi.BlockTree?.AsReadOnly() ?? throw new Exception($"api.BlockTree is null.");
			var trieNodeResolver = _nethermindApi.ReadOnlyTrieStore ?? throw new Exception($"api.ReadOnlyTrieStore is null.");
			var recoveryStep = _nethermindApi.BlockPreprocessor ?? throw new Exception($"api.BlockPreprocessor is null.");
			var rewardCalculatorSource = _nethermindApi.RewardCalculatorSource ?? throw new Exception($"api.RewardCalculatorSource is null.");
			var receiptFinder = _nethermindApi.ReceiptStorage ?? throw new Exception($"api.ReceiptStorage is null.");
			var specProvider = _nethermindApi.SpecProvider ?? throw new Exception($"api.SpecProvider is null.");
			var logManager = _nethermindApi.LogManager ?? throw new Exception($"api.LogManager is null.");
			var rpcModuleProvider = _nethermindApi.RpcModuleProvider ?? throw new Exception($"api.RpcModuleProvider is null.");
			var blockProducer = new Address(_config.BlockProducer);

			try
			{
				if (_config.Enabled == false) throw new Exception($"{Name}.Enabled configuration variables set to false, halting initialization of {Name} plugin.");
				_logger.Info($"{Name} Plugin enabled, initializing...");
				var multiCallModuleFactory = new MulticallModuleFactory(dbProvider, blockTree, jsonRpcConfig, trieNodeResolver, recoveryStep, rewardCalculatorSource, receiptFinder, specProvider, logManager, blockProducer);
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
