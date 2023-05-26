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
        private readonly static PluginInfo info = new PluginInfo();
        public override IPluginInfo Info => info;

		private readonly ILogger<SRTPluginUIRE4DirectXOverlay> logger;
		private IPluginHost pluginHost;

        private SRTPluginProducerRE4R.SRTPluginProducerRE4R producer;
        private PluginConfiguration Config => (PluginConfiguration)producer.Configuration;
        private IGameMemoryRE4R gameMemory;
        private CancellationTokenSource renderThreadCTS;
        private Thread renderThread;

        // DirectX Overlay-specific.
        private OverlayWindow window;
        private Graphics graphics;
        private SharpDX.Direct2D1.WindowRenderTarget device;
		private UIComponents ui;

		private Process? GetProcess() => Process.GetProcessesByName("re4")?.FirstOrDefault();
        private Process? gameProcess;
        private IntPtr gameWindowHandle;

        private string playerName = "";
        private string partnerName = "";
        private string partnerName2 = "";
        float? duffel = null;

        public SRTPluginUIRE4DirectXOverlay(ILogger<SRTPluginUIRE4DirectXOverlay> logger, IPluginHost pluginHost) : base()
        {
            this.pluginHost = pluginHost;
            this.logger = logger;

			this.producer = pluginHost.GetPluginReference<SRTPluginProducerRE4R.SRTPluginProducerRE4R>(nameof(SRTPluginProducerRE4R.SRTPluginProducerRE4R)) ?? default;
			if (producer == default)
				throw new PluginNotFoundException(nameof(SRTPluginProducerRE4R.SRTPluginProducerRE4R));

			this.gameProcess = GetProcess();
			if (gameProcess == default)
				throw new PluginInitializationException(nameof(SRTPluginUIRE4DirectXOverlay), $"Unable to initialize plugin.{Environment.NewLine}\"{nameof(gameProcess)}\" is null or default");

			Init();
		}

        public void Init()
        {
			gameWindowHandle = gameProcess.MainWindowHandle;

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
				Width = window.Width,
				Height = window.Height,
				WindowHandle = window.Handle
			};
			graphics?.Setup();

			// Get a refernence to the underlying RenderTarget from SharpDX. This'll be used to draw portions of images.
			device = (SharpDX.Direct2D1.WindowRenderTarget)typeof(Graphics).GetField("_device", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(graphics);

			ui = new UIComponents(graphics, Config);

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
			while (!renderThreadCTS.Token.IsCancellationRequested)
            {
                gameMemory = (IGameMemoryRE4R)producer.Refresh();
                window?.PlaceAbove(gameWindowHandle);
                window?.FitTo(gameWindowHandle, true);

                try
                {
                    graphics?.BeginScene();
                    graphics?.ClearScene();
                    if (Config.ScalingFactor != 1f)
                        device.Transform = new SharpDX.Mathematics.Interop.RawMatrix3x2(Config.ScalingFactor, 0f, 0f, Config.ScalingFactor, 0f, 0f);
                    DrawOverlay();
                    if (Config.ScalingFactor != 1f)
                        device.Transform = new SharpDX.Mathematics.Interop.RawMatrix3x2(1f, 0f, 0f, 1f, 0f, 0f);
                }
                catch (Exception ex)
                {
                    //hostDelegates.ExceptionMessage.Invoke(ex);
                }
                finally
                {
                    graphics?.EndScene();
                }

                Thread.Sleep(16); // Scene drawn, now sleep for a bit before we GO AGANE!
            }
        }

        private void DrawOverlay()
        {
			if (gameMemory.Timer.MeasureDemoSpendingTime && Config.HideUICutscene) return;
			if (gameMemory.Timer.MeasurePauseSpendingTime && Config.HideUIPause) return;
			if (gameMemory.Timer.MeasureInventorySpendingTime && Config.HideUIInventory) return;
			if (gameMemory.IsInGameShopOpen && Config.HideUIInShop) return;

			float baseXOffset = Config.PositionX;
            float baseYOffset = Config.PositionY;

            // Player HP
            float statsXOffset = baseXOffset + 5f;
            float statsYOffset = baseYOffset + 0f;

            float textOffsetX = 0f;
            textOffsetX = Config.PositionX + 15f;
			if (Config.AlignInfoTop)
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
					gameMemory.Timer.IGTFormattedString,
					gameMemory.PTAS.ToString(),
					gameMemory.Spinel.ToString(),
					gameMemory.PlayerContext.Position.X.ToString("F3"),
					gameMemory.PlayerContext.Position.Y.ToString("F3"),
					gameMemory.PlayerContext.Position.Z.ToString("F3"),
					gameMemory.PlayerContext.Rotation.W.ToString("F3"),
					gameMemory.PlayerContext.Rotation.Y.ToString("F3"),
					gameMemory.Rank.Rank.ToString(),
					gameMemory.Rank.ActionPoint.ToString(),
					gameMemory.Rank.ItemPoint.ToString(),
					gameMemory.GameStatsKillCountElement.Count.ToString(),
					duffel != null ? String.Format("On {0}", duffel?.ToString("F3")) : "Off",
				};
				ui.DrawTextBlockRows(graphics, Config, ref textOffsetX, ref statsYOffset, labels, vals, ui.brushes["green"]);
			}

			if (!Config.AlignInfoTop && Config.ShowIGT)
				ui.DrawTextBlock(graphics, Config, ref textOffsetX, ref statsYOffset, "IGT:", gameMemory.Timer.IGTFormattedString, ui.brushes["green"]);
			
			if (Config.ShowHPBars)
			{
				playerName = string.Format("{0}: ", gameMemory.PlayerContext.SurvivorTypeString).ToUpper();
				partnerName = string.Format("{0}: ", gameMemory.PartnerContext[0]?.SurvivorTypeString).ToUpper();
				partnerName2 = string.Format("{0}: ", gameMemory.PartnerContext[1]?.SurvivorTypeString).ToUpper();
				ui.DrawPlayerHP(graphics, window, Config, gameMemory.PlayerContext, playerName, ref statsXOffset, ref statsYOffset);
				if (gameMemory.PartnerContext[0] != null && gameMemory.PartnerContext[0].IsLoaded)
					ui.DrawPartnerHP(graphics, window, Config, gameMemory.PartnerContext[0], partnerName, ref statsXOffset, ref statsYOffset);
				if (gameMemory.PartnerContext[1] != null && gameMemory.PartnerContext[1].IsLoaded)
					ui.DrawPartnerHP(graphics, window, Config, gameMemory.PartnerContext[1], partnerName2, ref statsXOffset, ref statsYOffset);
			}

			float gfxHeight = ui.GetStringSize(graphics, ui.fonts[Config.StringFontName + " Bold"], "0", Config.FontSize).Y;

			if (!Config.AlignInfoTop)
			{
				if (Config.Debug)
				{
					ui.DrawTextBlock(graphics, Config, ref textOffsetX, ref statsYOffset, "ACTIVE:", ui.FormattedString(gameMemory.Timer.GameSaveData.GameElapsedTime), ui.brushes["green"]);
					ui.DrawTextBlock(graphics, Config, ref textOffsetX, ref statsYOffset, "CUTSCENE:", ui.FormattedString(gameMemory.Timer.GameSaveData.DemoSpendingTime), ui.brushes["green"]);
					ui.DrawTextBlock(graphics, Config, ref textOffsetX, ref statsYOffset, "INVENTORY:", ui.FormattedString(gameMemory.Timer.GameSaveData.InventorySpendingTime), ui.brushes["green"]);
					ui.DrawTextBlock(graphics, Config, ref textOffsetX, ref statsYOffset, "PAUSE:", ui.FormattedString(gameMemory.Timer.GameSaveData.PauseSpendingTime), ui.brushes["green"]);
					ui.DrawTextBlock(graphics, Config, ref textOffsetX, ref statsYOffset, "TIMER OFFSET:", ui.FormattedString(gameMemory.Timer.TimerOffset), ui.brushes["green"]);
				}

				if (Config.ShowPTAS)
				{
					textOffsetX = Config.PositionX + 15f;
					statsYOffset += gfxHeight * 1.5f;
					ui.DrawTextBlockRow(graphics, Config, ref textOffsetX, ref statsYOffset, "PTAS:", gameMemory.PTAS.ToString(), ui.brushes["green"]);
					ui.DrawTextBlockRow(graphics, Config, ref textOffsetX, ref statsYOffset, "SPINEL:", gameMemory.Spinel.ToString(), ui.brushes["green"]);
				}

				if (Config.ShowPosition)
				{
					textOffsetX = Config.PositionX + 15f;
					statsYOffset += gfxHeight * 1.5f;
					ui.DrawTextBlockRow(graphics, Config, ref textOffsetX, ref statsYOffset, "X:", gameMemory.PlayerContext.Position.X.ToString("F3"), ui.brushes["green"]);
					ui.DrawTextBlockRow(graphics, Config, ref textOffsetX, ref statsYOffset, "Y:", gameMemory.PlayerContext.Position.Y.ToString("F3"), ui.brushes["green"]);
					ui.DrawTextBlockRow(graphics, Config, ref textOffsetX, ref statsYOffset, "Z:", gameMemory.PlayerContext.Position.Z.ToString("F3"), ui.brushes["green"]);
				}

				if (Config.ShowRotation)
				{
					textOffsetX = Config.PositionX + 15f;
					statsYOffset += gfxHeight * 1.5f;
					ui.DrawTextBlockRow(graphics, Config, ref textOffsetX, ref statsYOffset, "RW:", gameMemory.PlayerContext.Rotation.W.ToString("F3"), ui.brushes["green"]);
					ui.DrawTextBlockRow(graphics, Config, ref textOffsetX, ref statsYOffset, "RY:", gameMemory.PlayerContext.Rotation.Y.ToString("F3"), ui.brushes["green"]);
				}

				if (Config.ShowDifficultyAdjustment)
				{
					textOffsetX = Config.PositionX + 15f;
					ui.DrawTextBlock(graphics, Config, ref textOffsetX, ref statsYOffset, "RANK:", gameMemory.Rank.Rank.ToString(), ui.brushes["green"]);
					ui.DrawTextBlock(graphics, Config, ref textOffsetX, ref statsYOffset, "ACTION POINT:", gameMemory.Rank.ActionPoint.ToString(), ui.brushes["green"]);
					ui.DrawTextBlock(graphics, Config, ref textOffsetX, ref statsYOffset, "ITEM POINT:", gameMemory.Rank.ItemPoint.ToString(), ui.brushes["green"]);
					ui.DrawTextBlock(graphics, Config, ref textOffsetX, ref statsYOffset, "KILL COUNT:", gameMemory.GameStatsKillCountElement.Count.ToString(), ui.brushes["green"]);
				}

				if (Config.ShowDuffle)
				{
					if (!gameMemory.PlayerContext.IsLoaded)
						duffel = null;

					if (gameMemory.CurrentChapter == "Chapter15")
					{
						if (gameMemory.PlayerContext.Position.Y < 0f && gameMemory.PlayerContext.Position.Y >= -0.5f)
							duffel = null;
						else if (gameMemory.PlayerContext.Position.Y < -0.5f && gameMemory.PlayerContext.Position.Y > -1)
							duffel = gameMemory.PlayerContext.Position.Y;
						ui.DrawTextBlock(graphics, Config, ref textOffsetX, ref statsYOffset, "Duffle:", duffel != null ? String.Format("On {0}", duffel?.ToString("F3")) : "Off", duffel != null ? ui.brushes["green"] : ui.brushes["red"]);
					}
				}
			}

			//// Enemy HP
			var xOffset = Config.EnemyHPPositionX == -1 ? statsXOffset : Config.EnemyHPPositionX;
			var yOffset = Config.EnemyHPPositionY == -1 ? statsYOffset : Config.EnemyHPPositionY;
			if (playerName.Contains("Ashley"))
			{
			    ui.DrawTextBlock(graphics, Config, ref textOffsetX, ref statsYOffset, "Enemy Count:", gameMemory.Enemies.Where(a => a.Health.IsAlive).ToArray().Count().ToString(), ui.brushes["green"]);
			    return;
			}
			yOffset += 8f;
			// Show Damaged Enemies Only
			if (Config.EnemyLimit == -1)
			{
			    var enemyList = gameMemory.Enemies
			        .Where(a => GetEnemyFilters(a))
			        .OrderByDescending(a => a.Health.MaxHP)
			        .ThenBy(a => a.Health.Percentage)
			        .ThenByDescending(a => a.Health.CurrentHP);

			    if (Config.ShowHPBars)
			        foreach (PlayerContext enemy in enemyList)
			            ui.DrawEnemies(graphics, window, Config, enemy, ref xOffset, ref yOffset);
			}
			else
			{
			    var enemyListLimited = gameMemory.Enemies
			        .Where(a => GetEnemyFilters(a))
			        .OrderByDescending(a => a.Health.MaxHP)
			        .ThenBy(a => a.Health.Percentage)
			        .ThenByDescending(a => a.Health.CurrentHP)
			        .Take(Config.EnemyLimit);

			    if (Config.ShowHPBars)
			        foreach (PlayerContext enemy in enemyListLimited)
			            ui.DrawEnemies(graphics, window, Config, enemy, ref xOffset, ref yOffset);
			}
		}

        private bool GetEnemyFilters(PlayerContext enemy)
        {
            if (Config.ShowDamagedEnemiesOnly)
                return enemy.Health.IsAlive && !enemy.IsAnimal && !enemy.IsIgnored && enemy.Health.IsDamaged;
            return enemy.Health.IsAlive && !enemy.IsAnimal && !enemy.IsIgnored;
        }

        public bool Equals(IPluginConsumer? other) => (this as IPluginConsumer).Equals(other);
    }

}
