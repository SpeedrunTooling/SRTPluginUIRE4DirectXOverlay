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

namespace SRTPluginUIRE4DirectXOverlay
{
    public class SRTPluginUIRE4DirectXOverlay : PluginBase<SRTPluginProducerRE4R.SRTPluginProducerRE4R>, IPluginConsumer
    {
        internal static PluginInfo _Info = new PluginInfo();
        public override IPluginInfo Info => _Info;
        public string RequiredProvider => "SRTPluginProviderRE4R";
		private readonly ILogger<SRTPluginUIRE4DirectXOverlay> logger;
		private IPluginHost pluginHost;
        private SRTPluginProducerRE4R.SRTPluginProducerRE4R producer;
        private PluginConfiguration Config => (PluginConfiguration)producer.Configuration;
        private IGameMemoryRE4R gameMemory;
        private CancellationTokenSource renderThreadCTS;
        private Thread renderThread;

        // DirectX Overlay-specific.
        private OverlayWindow _window;
        private Graphics _graphics;
        private SharpDX.Direct2D1.WindowRenderTarget _device;
		private UIComponents ui;

		private Process GetProcess() => Process.GetProcessesByName("re4")?.FirstOrDefault();
        private Process gameProcess;
        private IntPtr gameWindowHandle;

        private string PlayerName = "";
        private string PartnerName = "";
        private string PartnerName2 = "";
        float? duffel = null;

        public SRTPluginUIRE4DirectXOverlay(ILogger<SRTPluginUIRE4DirectXOverlay> logger, IPluginHost pluginHost) : base()
        {
            this.pluginHost = pluginHost;
            this.logger = logger;

			producer = pluginHost.GetPluginReference<SRTPluginProducerRE4R.SRTPluginProducerRE4R>(nameof(SRTPluginProducerRE4R.SRTPluginProducerRE4R));

			Init();
		}

        public void Init()
        {
			gameProcess = GetProcess();
			if (gameProcess == default)
				return;
			gameWindowHandle = gameProcess.MainWindowHandle;

			DEVMODE devMode = default;
			devMode.dmSize = (short)Marshal.SizeOf<DEVMODE>();
			PInvoke.EnumDisplaySettings(null, -1, ref devMode);

			// Create and initialize the overlay window.
			_window = new OverlayWindow(0, 0, devMode.dmPelsWidth, devMode.dmPelsHeight);
			_window?.Create();

			// Create and initialize the graphics object.
			_graphics = new Graphics()
			{
				MeasureFPS = false,
				PerPrimitiveAntiAliasing = false,
				TextAntiAliasing = true,
				UseMultiThreadedFactories = false,
				VSync = false,
				Width = _window.Width,
				Height = _window.Height,
				WindowHandle = _window.Handle
			};
			_graphics?.Setup();

			// Get a refernence to the underlying RenderTarget from SharpDX. This'll be used to draw portions of images.
			_device = (SharpDX.Direct2D1.WindowRenderTarget)typeof(Graphics).GetField("_device", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(_graphics);

			ui = new UIComponents(_graphics, Config);

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
            _device = null; // We didn't create this object so we probably shouldn't be the one to dispose of it. Just set the variable to null so the reference isn't held.
            _graphics?.Dispose(); // This should technically be the one to dispose of the _device object since it was pulled from this instance.
            _graphics = null;
            _window?.Dispose();
            _window = null;
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
                _window?.PlaceAbove(gameWindowHandle);
                _window?.FitTo(gameWindowHandle, true);

                try
                {
                    _graphics?.BeginScene();
                    _graphics?.ClearScene();
                    if (Config.ScalingFactor != 1f)
                        _device.Transform = new SharpDX.Mathematics.Interop.RawMatrix3x2(Config.ScalingFactor, 0f, 0f, Config.ScalingFactor, 0f, 0f);
                    DrawOverlay();
                    if (Config.ScalingFactor != 1f)
                        _device.Transform = new SharpDX.Mathematics.Interop.RawMatrix3x2(1f, 0f, 0f, 1f, 0f, 0f);
                }
                catch (Exception ex)
                {
                    //hostDelegates.ExceptionMessage.Invoke(ex);
                }
                finally
                {
                    _graphics?.EndScene();
                }

                Thread.Sleep(16); // Scene drawn, now sleep for a bit before we GO AGANE!
            }
        }

        private void DrawOverlay()
        {
			if (gameMemory.Timer.MeasureDemoSpendingTime || gameMemory.Timer.MeasurePauseSpendingTime || gameMemory.Timer.MeasureInventorySpendingTime || gameMemory.IsInGameShopOpen) return;
            float baseXOffset = Config.PositionX;
            float baseYOffset = Config.PositionY;

            // Player HP
            float statsXOffset = baseXOffset + 5f;
            float statsYOffset = baseYOffset + 0f;

            float textOffsetX = 0f;
            textOffsetX = Config.PositionX + 15f;
			ui.DrawTextBlock(_graphics, Config, ref textOffsetX, ref statsYOffset, "IGT:", gameMemory.Timer.IGTFormattedString, ui.brushes["green"]);
			
			PlayerName = string.Format("{0}: ", gameMemory.PlayerContext.SurvivorTypeString);
			PartnerName = string.Format("{0}: ", gameMemory.PartnerContext[0]?.SurvivorTypeString);
			PartnerName2 = string.Format("{0}: ", gameMemory.PartnerContext[1]?.SurvivorTypeString);
			ui.DrawPlayerHP(_graphics, _window, Config, gameMemory.PlayerContext, PlayerName, ref statsXOffset, ref statsYOffset);
			if (gameMemory.PartnerContext[0] != null && gameMemory.PartnerContext[0].IsLoaded)
				ui.DrawPartnerHP(_graphics, _window, Config, gameMemory.PartnerContext[0], PartnerName, ref statsXOffset, ref statsYOffset);
			if (gameMemory.PartnerContext[1] != null && gameMemory.PartnerContext[1].IsLoaded)
				ui.DrawPartnerHP(_graphics, _window, Config, gameMemory.PartnerContext[1], PartnerName2, ref statsXOffset, ref statsYOffset);

			if (!Config.CenterPlayerHP)
				statsYOffset += 6f;

			if (Config.Debug)
			{
				ui.DrawTextBlock(_graphics, Config, ref textOffsetX, ref statsYOffset, "Active:", ui.FormattedString(gameMemory.Timer.GameSaveData.GameElapsedTime), ui.brushes["green"]);
				ui.DrawTextBlock(_graphics, Config, ref textOffsetX, ref statsYOffset, "Cutscene:", ui.FormattedString(gameMemory.Timer.GameSaveData.DemoSpendingTime), ui.brushes["green"]);
				ui.DrawTextBlock(_graphics, Config, ref textOffsetX, ref statsYOffset, "Inventory:", ui.FormattedString(gameMemory.Timer.GameSaveData.InventorySpendingTime), ui.brushes["green"]);
				ui.DrawTextBlock(_graphics, Config, ref textOffsetX, ref statsYOffset, "Pause:", ui.FormattedString(gameMemory.Timer.GameSaveData.PauseSpendingTime), ui.brushes["green"]);
			}

			if (Config.ShowPTAS)
			{
			    textOffsetX = Config.PositionX + 15f;
			    statsYOffset += 24f;
			    ui.DrawTextBlockRow(_graphics, Config, ref textOffsetX, ref statsYOffset, "PTAS:", gameMemory.PTAS.ToString(), ui.brushes["green"]);
			    ui.DrawTextBlockRow(_graphics, Config, ref textOffsetX, ref statsYOffset, "Spinel:", gameMemory.Spinel.ToString(), ui.brushes["green"]);
			}

			if (Config.ShowPosition)
			{
			    textOffsetX = Config.PositionX + 15f;
			    statsYOffset += 24f;
			    ui.DrawTextBlockRow(_graphics, Config, ref textOffsetX, ref statsYOffset, "X:", gameMemory.PlayerContext.Position.X.ToString("F3"), ui.brushes["green"]);
			    ui.DrawTextBlockRow(_graphics, Config, ref textOffsetX, ref statsYOffset, "Y:", gameMemory.PlayerContext.Position.Y.ToString("F3"), ui.brushes["green"]);
			    ui.DrawTextBlockRow(_graphics, Config, ref textOffsetX, ref statsYOffset, "Z:", gameMemory.PlayerContext.Position.Z.ToString("F3"), ui.brushes["green"]);
			}

			if (Config.ShowRotation)
			{
			    textOffsetX = Config.PositionX + 15f;
			    statsYOffset += 24;
			    ui.DrawTextBlockRow(_graphics, Config, ref textOffsetX, ref statsYOffset, "RW:", gameMemory.PlayerContext.Rotation.W.ToString("F3"), ui.brushes["green"]);
			    ui.DrawTextBlockRow(_graphics, Config, ref textOffsetX, ref statsYOffset, "RY:", gameMemory.PlayerContext.Rotation.Y.ToString("F3"), ui.brushes["green"]);
			}

			if (Config.ShowDifficultyAdjustment)
			{
			    textOffsetX = Config.PositionX + 15f;
			    ui.DrawTextBlock(_graphics, Config, ref textOffsetX, ref statsYOffset, "Rank:", gameMemory.Rank.Rank.ToString(), ui.brushes["green"]);
			    ui.DrawTextBlock(_graphics, Config, ref textOffsetX, ref statsYOffset, "Action Point:", gameMemory.Rank.ActionPoint.ToString(), ui.brushes["green"]);
			    ui.DrawTextBlock(_graphics, Config, ref textOffsetX, ref statsYOffset, "Item Point:", gameMemory.Rank.ItemPoint.ToString(), ui.brushes["green"]);
				ui.DrawTextBlock(_graphics, Config, ref textOffsetX, ref statsYOffset, "Kill Count:", gameMemory.GameStatsKillCountElement.Count.ToString(), ui.brushes["green"]);
			}

			if (!gameMemory.PlayerContext.IsLoaded)
			    duffel = null;

			if (Config.ShowDuffle && gameMemory.CurrentChapter == "Chapter15")
			{
			    if (gameMemory.PlayerContext.Position.Y < 0f && gameMemory.PlayerContext.Position.Y >= -0.5f)
			        duffel = null;
			    else if (gameMemory.PlayerContext.Position.Y < -0.5f && gameMemory.PlayerContext.Position.Y > -1)
			        duffel = gameMemory.PlayerContext.Position.Y;
			    ui.DrawTextBlock(_graphics, Config, ref textOffsetX, ref statsYOffset, "Duffle:", duffel != null ? String.Format("On {0}", duffel?.ToString("F3")) : "Off", duffel != null ? ui.brushes["green"] : ui.brushes["red"]);
			}

			//// Enemy HP
			var xOffset = Config.EnemyHPPositionX == -1 ? statsXOffset : Config.EnemyHPPositionX;
			var yOffset = Config.EnemyHPPositionY == -1 ? statsYOffset : Config.EnemyHPPositionY;
			if (PlayerName.Contains("Ashley"))
			{
			    ui.DrawTextBlock(_graphics, Config, ref textOffsetX, ref statsYOffset, "Enemy Count:", gameMemory.Enemies.Where(a => a.Health.IsAlive).ToArray().Count().ToString(), ui.brushes["green"]);
			    return;
			}

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
			            ui.DrawEnemies(_graphics, _window, Config, enemy, ref xOffset, ref yOffset);
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
			            ui.DrawEnemies(_graphics, _window, Config, enemy, ref xOffset, ref yOffset);
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
