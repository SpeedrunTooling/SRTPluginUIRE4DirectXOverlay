using GameOverlay.Drawing;
using GameOverlay.Windows;
using SRTPluginBase;
using SRTPluginProducerRE4R;
using SRTPluginProducerRE4R.Structs;
using SRTPluginUIRE4DirectXOverlay.UI;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace SRTPluginUIRE4DirectXOverlay
{
	public class SRTPluginUIRE4DirectXOverlay : PluginBase<SRTPluginProducerRE4R.SRTPluginProducerRE4R>, IPluginConsumer
    {
		private const int DRAW_LOOP_INTERVAL = 16;

		public override IPluginInfo Info => new PluginInfo();

		private readonly ILogger<SRTPluginUIRE4DirectXOverlay> logger;
		private IPluginHost pluginHost;

        private SRTPluginProducerRE4R.SRTPluginProducerRE4R? producer;
        private PluginConfiguration? Config => producer?.Configuration as PluginConfiguration;
        private IGameMemoryRE4R? gameMemory;
        private CancellationTokenSource? renderThreadCTS;
        private Thread? renderThread;

        // DirectX Overlay-specific.
        private OverlayWindow? window;
        private Graphics? graphics;
        private SharpDX.Direct2D1.WindowRenderTarget? device;
		private UIComponents? ui;

		private Process? GetProcess() => Process.GetProcessesByName("re4")?.FirstOrDefault();
        private Process? gameProcess;
        private IntPtr gameWindowHandle;
        float? duffel = null;

        public SRTPluginUIRE4DirectXOverlay(ILogger<SRTPluginUIRE4DirectXOverlay> logger, IPluginHost pluginHost) : base()
        {
            this.pluginHost = pluginHost;
            this.logger = logger;

			this.producer = GetProducerReference();
			if (producer == default)
				throw new PluginNotFoundException(nameof(SRTPluginProducerRE4R.SRTPluginProducerRE4R));

			this.gameProcess = GetProcess();
			if (gameProcess == default)
				throw new PluginInitializationException(nameof(SRTPluginUIRE4DirectXOverlay), $"Unable to initialize plugin.{Environment.NewLine}\"{nameof(gameProcess)}\" is null or default");

			Init();
		}

		private SRTPluginProducerRE4R.SRTPluginProducerRE4R? GetProducerReference() => pluginHost.GetPluginReference<SRTPluginProducerRE4R.SRTPluginProducerRE4R>(nameof(SRTPluginProducerRE4R.SRTPluginProducerRE4R)) ?? default;

		public void Init()
        {
			gameWindowHandle = gameProcess?.MainWindowHandle ?? default;

			DEVMODE devMode = default;
			devMode.dmSize = (short)Marshal.SizeOf<DEVMODE>();
			PInvoke.EnumDisplaySettings(null, -1, ref devMode);

			// Create and initialize the overlay window.
			window = new OverlayWindow(0, 0, devMode.dmPelsWidth, devMode.dmPelsHeight);
			window?.Create();

			// Create and initialize the graphics object.
			graphics = new Graphics()
			{
				MeasureFPS = false,
				PerPrimitiveAntiAliasing = false,
				TextAntiAliasing = true,
				UseMultiThreadedFactories = false,
				VSync = false,
				Width = window?.Width ?? 0,
				Height = window?.Height ?? 0,
				WindowHandle = window?.Handle ?? default
			};
			graphics?.Setup();

			// Get a refernence to the underlying RenderTarget from SharpDX. This'll be used to draw portions of images.
			device = typeof(Graphics).GetField("_device", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(graphics) as SharpDX.Direct2D1.WindowRenderTarget;

			ui = new UIComponents(graphics);

			renderThreadCTS = new CancellationTokenSource();
			renderThread = new Thread(ReceiveData)
			{
				IsBackground = true,
				Priority = ThreadPriority.Normal,
			};
			renderThread.Start();
		}

        public override void Dispose()
        {
            renderThreadCTS?.Cancel();
            renderThread?.Join();
            renderThread = null;
            renderThreadCTS?.Dispose();
            device = null; // We didn't create this object so we probably shouldn't be the one to dispose of it. Just set the variable to null so the reference isn't held.
            graphics?.Dispose(); // This should technically be the one to dispose of the _device object since it was pulled from this instance.
            graphics = null;
            window?.Dispose();
            window = null;
            gameProcess?.Dispose();
            gameProcess = null;
			ui?.Dispose();
			ui = null;
			return;
        }

		public override async ValueTask DisposeAsync()
		{
            Dispose();
            GC.SuppressFinalize(this);
            await Task.CompletedTask;
		}

        public void ReceiveData()
        {
			while (!(renderThreadCTS?.Token.IsCancellationRequested ?? false))
            {
				Thread.Sleep(DRAW_LOOP_INTERVAL); // Don't go too hard now ;3

				gameMemory = producer?.Refresh() as IGameMemoryRE4R;
				window?.PlaceAbove(gameWindowHandle);
                window?.FitTo(gameWindowHandle, true);

				try
				{
					graphics?.BeginScene();
					graphics?.ClearScene();
					if (device is not null)
						device.Transform = new SharpDX.Mathematics.Interop.RawMatrix3x2(Config?.ScalingFactor ?? 1f, 0f, 0f, Config?.ScalingFactor ?? 1f, 0f, 0f);
					DrawOverlay();
				}
				catch (Exception ex)
				{
					logger.LogCritical(ex, $"A {ex.GetType().Name} exception occurred while trying to render overlay");
				}
				finally
				{
					graphics?.EndScene();
				}
            }
        }

        private void DrawOverlay()
        {
			if ((gameMemory?.Timer?.MeasureDemoSpendingTime ?? default) && (Config?.HideUICutscene ?? default)) return;
			if ((gameMemory?.Timer?.MeasurePauseSpendingTime ?? default) && (Config?.HideUIPause ?? default)) return;
			if ((gameMemory?.Timer?.MeasureInventorySpendingTime ?? default) && (Config?.HideUIInventory ?? default)) return;
			if ((gameMemory?.IsInGameShopOpen ?? default) && (Config?.HideUIInShop ?? default)) return;

			float baseXOffset = Config?.PositionX ?? default;
            float baseYOffset = Config?.PositionY ?? default;

            // Player HP
            float statsXOffset = baseXOffset + 5f;
            float statsYOffset = baseYOffset + 0f;

            float textOffsetX = 0f;
            textOffsetX = (Config?.PositionX ?? default) + 15f;
			if (Config?.AlignInfoTop ?? default)
			{
				List<string> labels = new List<string>()
				{
					"IGT:",
					"PTAS:",
					"SPINEL:",
					"X:",
					"Y:",
					"Z:",
					"RW:",
					"RY:",
					"RANK:",
					"ACTION POINT:",
					"ITEM POINT:",
					"KILL COUNT:",
					"DUFFLE:",
				};

				List<string> vals = new List<string>()
				{
					gameMemory?.Timer?.IGTFormattedString ?? GameTimer.IGT_DEFAULT_STRING,
					(gameMemory?.PTAS ?? default).ToString(),
					(gameMemory?.Spinel ?? default).ToString(),
					(gameMemory?.PlayerContext?.Position?.X ?? default).ToString("F3"),
					(gameMemory?.PlayerContext?.Position?.Y ?? default).ToString("F3"),
					(gameMemory?.PlayerContext?.Position?.Z ?? default).ToString("F3"),
					(gameMemory?.PlayerContext?.Rotation?.W ?? default).ToString("F3"),
					(gameMemory?.PlayerContext?.Rotation?.Y ?? default).ToString("F3"),
					(gameMemory?.Rank.Rank ?? default).ToString(),
					(gameMemory?.Rank.ActionPoint ?? default).ToString(),
					(gameMemory?.Rank.ItemPoint ?? default).ToString(),
					(gameMemory?.GameStatsKillCountElement.Count ?? default).ToString(),
					duffel is not null ? $"On {(duffel ?? default).ToString("F3")}" : "Off",
				};
				ui?.DrawTextBlockRows(graphics, Config, ref textOffsetX, ref statsYOffset, labels, vals, ui?.brushes["green"]);
			}

			if (!(Config?.AlignInfoTop ?? default) && (Config?.ShowIGT ?? default))
				ui?.DrawTextBlock(graphics, Config, ref textOffsetX, ref statsYOffset, "IGT:", gameMemory?.Timer?.IGTFormattedString ?? GameTimer.IGT_DEFAULT_STRING, ui?.brushes["green"]);

			float gfxHeight = ui?.GetStringSize(graphics, ui?.fonts[Config?.StringFontName ?? Constants.DEFAULT_FONT_NAME + " Bold"], "0", Config?.FontSize ?? Constants.DEFAULT_FONT_SIZE).Y ?? default;

			if (!(Config?.AlignInfoTop ?? default))
			{
				if (Config?.Debug ?? default)
				{
					ui?.DrawTextBlock(graphics, Config, ref textOffsetX, ref statsYOffset, "ACTIVE:", ui?.FormattedString(gameMemory?.Timer?.GameSaveData?.GameElapsedTime ?? default) ?? string.Empty, ui?.brushes["green"]);
					ui?.DrawTextBlock(graphics, Config, ref textOffsetX, ref statsYOffset, "CUTSCENE:", ui?.FormattedString(gameMemory?.Timer?.GameSaveData?.DemoSpendingTime ?? default) ?? string.Empty, ui?.brushes["green"]);
					ui?.DrawTextBlock(graphics, Config, ref textOffsetX, ref statsYOffset, "INVENTORY:", ui?.FormattedString(gameMemory?.Timer?.GameSaveData?.InventorySpendingTime ?? default) ?? string.Empty, ui?.brushes["green"]);
					ui?.DrawTextBlock(graphics, Config, ref textOffsetX, ref statsYOffset, "PAUSE:", ui?.FormattedString(gameMemory?.Timer?.GameSaveData?.PauseSpendingTime ?? default) ?? string.Empty, ui?.brushes["green"]);
					ui?.DrawTextBlock(graphics, Config, ref textOffsetX, ref statsYOffset, "TIMER OFFSET:", ui?.FormattedString(gameMemory?.Timer?.TimerOffset ?? default) ?? string.Empty, ui?.brushes["green"]);
				}

				if (Config?.ShowPTAS ?? default)
				{
					textOffsetX = (Config?.PositionX ?? default) + 15f;
					statsYOffset += gfxHeight * 1.2f;
					ui?.DrawTextBlockRow(graphics, Config, ref textOffsetX, ref statsYOffset, "PTAS:", (gameMemory?.PTAS ?? default).ToString(), ui?.brushes["green"]);
					ui?.DrawTextBlockRow(graphics, Config, ref textOffsetX, ref statsYOffset, "SPINEL:", (gameMemory?.Spinel ?? default).ToString(), ui?.brushes["green"]);
				}

				if (Config?.ShowPosition ?? default)
				{
					textOffsetX = (Config?.PositionX ?? default) + 15f;
					statsYOffset += gfxHeight * 1.2f;
					ui?.DrawTextBlockRow(graphics, Config, ref textOffsetX, ref statsYOffset, "X:", (gameMemory?.PlayerContext?.Position.X ?? default).ToString("F3"), ui?.brushes["green"]);
					ui?.DrawTextBlockRow(graphics, Config, ref textOffsetX, ref statsYOffset, "Y:", (gameMemory?.PlayerContext?.Position.Y ?? default).ToString("F3"), ui?.brushes["green"]);
					ui?.DrawTextBlockRow(graphics, Config, ref textOffsetX, ref statsYOffset, "Z:", (gameMemory?.PlayerContext?.Position.Z ?? default).ToString("F3"), ui?.brushes["green"]);
				}

				if (Config?.ShowRotation ?? default)
				{
					textOffsetX = (Config?.PositionX ?? default) + 15f;
					statsYOffset += gfxHeight * 1.2f;
					ui?.DrawTextBlockRow(graphics, Config, ref textOffsetX, ref statsYOffset, "RW:", (gameMemory?.PlayerContext?.Rotation.W ?? default).ToString("F3"), ui?.brushes["green"]);
					ui?.DrawTextBlockRow(graphics, Config, ref textOffsetX, ref statsYOffset, "RY:", (gameMemory?.PlayerContext?.Rotation.Y ?? default).ToString("F3"), ui?.brushes["green"]);
				}

				if (Config?.ShowDifficultyAdjustment ?? default)
				{
					textOffsetX = (Config?.PositionX ?? default) + 15f;
                    ui?.DrawTextBlock(graphics, Config, ref textOffsetX, ref statsYOffset, "RANK:", (gameMemory?.Rank.Rank ?? default).ToString(), ui?.brushes["green"]);
					ui?.DrawTextBlock(graphics, Config, ref textOffsetX, ref statsYOffset, "ACTION POINT:", (gameMemory?.Rank.ActionPoint ?? default).ToString(), ui?.brushes["green"]);
					ui?.DrawTextBlock(graphics, Config, ref textOffsetX, ref statsYOffset, "ITEM POINT:", (gameMemory?.Rank.ItemPoint ?? default).ToString(), ui?.brushes["green"]);
					ui?.DrawTextBlock(graphics, Config, ref textOffsetX, ref statsYOffset, "KILL COUNT:", (gameMemory?.GameStatsKillCountElement.Count ?? default).ToString(), ui?.brushes["green"]);
				}

				if (Config?.ShowDuffle ?? default)
				{
					if (!(gameMemory?.PlayerContext?.IsLoaded ?? default))
						duffel = null;

					if (string.Equals(gameMemory?.CurrentChapter, "Chapter15"))
					{
						if ((gameMemory?.PlayerContext?.Position.Y ?? default) < 0f && (gameMemory?.PlayerContext?.Position.Y ?? default) >= -0.5f)
							duffel = null;
						else if ((gameMemory?.PlayerContext?.Position.Y ?? default) < -0.5f && (gameMemory?.PlayerContext?.Position.Y ?? default) > -1)
							duffel = gameMemory?.PlayerContext?.Position.Y ?? default;
						ui?.DrawTextBlock(graphics, Config, ref textOffsetX, ref statsYOffset, "Duffle:", duffel is not null ? $"On {duffel?.ToString("F3")}" : "Off", duffel is not null ? ui?.brushes["green"] : ui?.brushes["red"]);
					}
				}
			}

            var xOffsetPlayer = (Config?.PlayerHPPositionX == -1 ? statsXOffset : Config?.PlayerHPPositionX) ?? default;
            var yOffsetPlayer = (Config?.PlayerHPPositionY == -1 ? statsYOffset : Config?.PlayerHPPositionY) ?? default;

            //// Player HP
            HPType type = (HPType)(Config?.PlayerHPType ?? default);
            HPPosition alignment = (HPPosition)(Config?.PlayerHPPosition ?? default);


            ui?.DrawHP(graphics, window, Config, gameMemory?.PlayerContext, type, alignment, ref xOffsetPlayer, ref yOffsetPlayer, 4f);
            if (gameMemory?.PartnerContext?[0] is not null && (gameMemory?.PartnerContext?[0]?.IsLoaded ?? false))
                ui?.DrawHP(graphics, window, Config, gameMemory?.PartnerContext?[0], type, alignment, ref xOffsetPlayer, ref yOffsetPlayer, 3f);
            if (gameMemory?.PartnerContext?[1] is not null && (gameMemory?.PartnerContext?[1]?.IsLoaded ?? false))
                ui?.DrawHP(graphics, window, Config, gameMemory?.PartnerContext?[1], type, alignment, ref xOffsetPlayer, ref yOffsetPlayer, 2f);

            //// Enemy HP
            var xOffset = (Config?.EnemyHPPositionX == -1 ? statsXOffset : Config?.EnemyHPPositionX) ?? default;
			var yOffset = 0f;
            if (Config?.EnemyHPPositionY == -1 && alignment != HPPosition.Right)
                yOffset = yOffsetPlayer;
            else if (Config?.EnemyHPPositionY != -1)
                yOffset = (Config?.EnemyHPPositionY ?? default);
            else
                yOffset = statsYOffset;

			yOffset += 8f;
			if ((Config?.EnemyLimit ?? -1) == -1)
			{
			    var enemyList = (gameMemory?.Enemies ?? new PlayerContext[0])
			        .Where(a => GetEnemyFilters(a))
			        .OrderByDescending(a => a?.Health?.MaxHP ?? default)
			        .ThenBy(a => a?.Health?.Percentage)
			        .ThenByDescending(a => a?.Health?.CurrentHP);

			    if (Config?.ShowHPBars ?? default)
					if ((Config?.ShowBossOnly ?? default) && (Config?.CenterBossHP ?? default))
                        ui?.DrawHP(graphics, window, Config, enemyList.FirstOrDefault(), type, HPPosition.Custom, ref xOffsetPlayer, ref yOffset, 0f);
					else
						foreach (PlayerContext? enemy in enemyList)
							ui?.DrawHP(graphics, window, Config, enemy, type, HPPosition.Left, ref xOffsetPlayer, ref yOffset, 0f);
            }
			else
			{
			    var enemyListLimited = (gameMemory?.Enemies ?? new PlayerContext[0])
					.Where(a => GetEnemyFilters(a))
			        .OrderByDescending(a => a?.Health?.MaxHP)
			        .ThenBy(a => a?.Health?.Percentage)
			        .ThenByDescending(a => a?.Health?.CurrentHP)
			        .Take(Config?.EnemyLimit ?? default);

			    if (Config?.ShowHPBars ?? default)
                    if ((Config?.ShowBossOnly ?? default) && (Config?.CenterBossHP ?? default))
                        ui?.DrawHP(graphics, window, Config, enemyListLimited.FirstOrDefault(), type, HPPosition.Custom, ref xOffsetPlayer, ref yOffset, 0f);
                    else
                        foreach (PlayerContext? enemy in enemyListLimited)
                        ui?.DrawHP(graphics, window, Config, enemy, type, HPPosition.Left, ref xOffsetPlayer, ref yOffsetPlayer, 0f);
            }
		}

        private bool GetEnemyFilters(PlayerContext? enemy)
        {
            if (Config?.ShowBossOnly ?? default)
                return (enemy?.Health?.IsAlive ?? default) && (enemy?.IsBoss ?? default);
            else if (Config?.ShowDamagedEnemiesOnly ?? default)
                return (enemy?.Health?.IsAlive ?? default) && !(enemy?.IsAnimal ?? default) && !(enemy?.IsIgnored ?? default) && (enemy?.Health?.IsDamaged ?? default);
            return (enemy?.Health?.IsAlive ?? default) && !(enemy?.IsAnimal ?? default) && !(enemy?.IsIgnored ?? default);
        }

        public bool Equals(IPluginConsumer? other) => (this as IPluginConsumer).Equals(other);
    }
}
