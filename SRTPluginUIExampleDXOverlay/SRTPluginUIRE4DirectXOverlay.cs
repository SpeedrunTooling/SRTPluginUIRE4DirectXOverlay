using GameOverlay.Drawing;
using GameOverlay.Windows;
using SRTPluginBase;
using SRTPluginProviderRE4R;
using SRTPluginProviderRE4R.Structs;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace SRTPluginUIRE4DirectXOverlay
{
    public class SRTPluginUIRE4DirectXOverlay : PluginBase, IPluginUI
    {
        internal static PluginInfo _Info = new PluginInfo();
        public override IPluginInfo Info => _Info;
        public string RequiredProvider => "SRTPluginProviderRE4R";
        private IPluginHostDelegates hostDelegates;
        private IGameMemoryRE4R gameMemory;

        // DirectX Overlay-specific.
        private OverlayWindow _window;
        private Graphics _graphics;
        private SharpDX.Direct2D1.WindowRenderTarget _device;

        private Font _consolasBold;

        private SolidBrush _black;
        private SolidBrush _white;
        private SolidBrush _grey;
        private SolidBrush _darkred;
        private SolidBrush _red;
        private SolidBrush _yellow_500;
        private SolidBrush _slate_800;
        private SolidBrush _slate_900;
        private SolidBrush _sky_500;
        private SolidBrush _lightred;
        private SolidBrush _lightyellow;
        private SolidBrush _lightgreen;
        private SolidBrush _lawngreen;
        private SolidBrush _goldenrod;
        private SolidBrush _greydark;
        private SolidBrush _greydarker;
        private SolidBrush _darkgreen;
        private SolidBrush _darkyellow;

        private SolidBrush _lightpurple;
        private SolidBrush _darkpurple;

        public PluginConfiguration config;
        private Process GetProcess() => Process.GetProcessesByName("re4")?.FirstOrDefault();
        private Process gameProcess;
        private IntPtr gameWindowHandle;

        //STUFF
        SolidBrush HPBarColor;
        SolidBrush TextColor;

        private string PlayerName = "";

        [STAThread]
        public override int Startup(IPluginHostDelegates hostDelegates)
        {
            this.hostDelegates = hostDelegates;
            config = LoadConfiguration<PluginConfiguration>();

            gameProcess = GetProcess();
            if (gameProcess == default)
                return 1;
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

            _consolasBold = _graphics?.CreateFont(config.StringFontName, config.FontSize, true);

            _yellow_500 = _graphics?.CreateSolidBrush(245, 158, 11, 255);
            _sky_500 = _graphics?.CreateSolidBrush(14, 165, 233, 255);
            _slate_800 = _graphics?.CreateSolidBrush(30, 41, 59, 100);
            _slate_900 = _graphics?.CreateSolidBrush(15, 23, 42, 255);
            _black = _graphics?.CreateSolidBrush(0, 0, 0);
            _white = _graphics?.CreateSolidBrush(255, 255, 255);
            _grey = _graphics?.CreateSolidBrush(128, 128, 128);
            _greydark = _graphics?.CreateSolidBrush(64, 64, 64);
            _greydarker = _graphics?.CreateSolidBrush(24, 24, 24, 100);
            _darkred = _graphics?.CreateSolidBrush(153, 0, 0, 100);
            _darkgreen = _graphics?.CreateSolidBrush(0, 102, 0, 100);
            _darkyellow = _graphics?.CreateSolidBrush(218, 165, 32, 100);
            _red = _graphics?.CreateSolidBrush(255, 0, 0);
            _lightred = _graphics?.CreateSolidBrush(255, 172, 172);
            _lightyellow = _graphics?.CreateSolidBrush(255, 255, 150);
            _lightgreen = _graphics?.CreateSolidBrush(150, 255, 150);
            _lawngreen = _graphics?.CreateSolidBrush(124, 252, 0);
            _goldenrod = _graphics?.CreateSolidBrush(218, 165, 32);

            _lightpurple = _graphics?.CreateSolidBrush(222, 182, 255);
            _darkpurple = _graphics?.CreateSolidBrush(73, 58, 85, 100);

            HPBarColor = _grey;
            TextColor = _white;
            return 0;
        }

        public override int Shutdown()
        {
            SaveConfiguration(config);

            _black?.Dispose();
            _white?.Dispose();
            _grey?.Dispose();
            _darkred?.Dispose();
            _red?.Dispose();
            _yellow_500?.Dispose();
            _slate_800?.Dispose();
            _slate_900?.Dispose();
            _sky_500?.Dispose();
            _lightred?.Dispose();
            _lightyellow?.Dispose();
            _lightgreen?.Dispose();
            _lawngreen?.Dispose();
            _goldenrod?.Dispose();
            _greydark?.Dispose();
            _greydarker?.Dispose();
            _darkgreen?.Dispose();
            _darkyellow?.Dispose();
            _lightpurple?.Dispose();
            _darkpurple?.Dispose();

            _consolasBold?.Dispose();

            _device = null; // We didn't create this object so we probably shouldn't be the one to dispose of it. Just set the variable to null so the reference isn't held.
            _graphics?.Dispose(); // This should technically be the one to dispose of the _device object since it was pulled from this instance.
            _graphics = null;
            _window?.Dispose();
            _window = null;

            gameProcess?.Dispose();
            gameProcess = null;

            return 0;
        }

        public int ReceiveData(object gameMemory)
        {
            this.gameMemory = (IGameMemoryRE4R)gameMemory;
            _window?.PlaceAbove(gameWindowHandle);
            _window?.FitTo(gameWindowHandle, true);

            try
            {
                _graphics?.BeginScene();
                _graphics?.ClearScene();
                if (config.ScalingFactor != 1f)
                    _device.Transform = new SharpDX.Mathematics.Interop.RawMatrix3x2(config.ScalingFactor, 0f, 0f, config.ScalingFactor, 0f, 0f);
                DrawOverlay();
                if (config.ScalingFactor != 1f)
                    _device.Transform = new SharpDX.Mathematics.Interop.RawMatrix3x2(1f, 0f, 0f, 1f, 0f, 0f);
            }
            catch (Exception ex)
            {
                hostDelegates.ExceptionMessage.Invoke(ex);
            }
            finally
            {
                _graphics?.EndScene();
            }

            return 0;
        }

        private float GetStringSize(string str)
        {
            return (float)_graphics?.MeasureString(_consolasBold, _consolasBold.FontSize, str).X;
        }

        private float AlignRight(string s, float x)
        {
            return (x + 170f) - GetStringSize(s);
        }

        private void DrawTextBlock(ref float dx, ref float dy, string label, string val, SolidBrush color)
        {
            _graphics?.DrawText(_consolasBold, _consolasBold.FontSize, _white, dx, dy += 24f, label);
            var dx2 = dx + GetStringSize(label) + 8f;
            _graphics?.DrawText(_consolasBold, _consolasBold.FontSize, color, dx2, dy, val);
        }

        private void DrawTextBlockRow(ref float dx, ref float dy, string label, string val, SolidBrush color)
        {
            float marginX = 40f;
            _graphics?.DrawText(_consolasBold, _consolasBold.FontSize, _white, dx, dy, label);
            var dx2 = dx + GetStringSize(label) + 8f;
            _graphics?.DrawText(_consolasBold, _consolasBold.FontSize, color, dx2, dy, val);
            dx += GetStringSize(label) + GetStringSize(val) + marginX;
        }

        private float GetStringSize(string str, float size = 20f)
        {
            return (float)_graphics?.MeasureString(_consolasBold, size, str).X;
        }

        private void DrawProgressBar(ref float xOffset, ref float yOffset, string name, float chealth, float mhealth, float percentage = 1f)
        {
            if (config.ShowDamagedEnemiesOnly && percentage == 1f) { return; }
            string perc = float.IsNaN(percentage) ? "0%" : string.Format("{0:P1}", percentage);
            float endOfBar = config.PositionX + 342f - GetStringSize(perc, config.FontSize);
            _graphics.DrawRectangle(_greydark, xOffset, yOffset += 28f, xOffset + 342f, yOffset + 22f, 4f);
            _graphics.FillRectangle(_greydarker, xOffset + 1f, yOffset + 1f, xOffset + 340f, yOffset + 20f);
            _graphics.FillRectangle(_darkred, xOffset + 1f, yOffset + 1f, xOffset + (340f * percentage), yOffset + 20f);
            _graphics.DrawText(_consolasBold, config.FontSize, _lightred, xOffset + 10f, yOffset + 2, string.Format("{0} {1} / {2}", name, chealth, mhealth));
            _graphics.DrawText(_consolasBold, config.FontSize, _lightred, endOfBar, yOffset + 2, perc);
        }

        private void DrawHealthBar(ref float xOffset, ref float yOffset, string name, float chealth, float mhealth, float percentage = 1f)
        {
            string perc = float.IsNaN(percentage) ? "0%" : string.Format("{0:P1}", percentage);
            float endOfBar = config.PositionX + 342f - GetStringSize(perc, config.FontSize);
            _graphics.DrawRectangle(_greydark, xOffset, yOffset += 28f, xOffset + 342f, yOffset + 22f, 4f);
            _graphics.FillRectangle(_greydarker, xOffset + 1f, yOffset + 1f, xOffset + 340f, yOffset + 20f);
            _graphics.FillRectangle(HPBarColor, xOffset + 1f, yOffset + 1f, xOffset + (340f * percentage), yOffset + 20f);
            _graphics.DrawText(_consolasBold, config.FontSize, TextColor, xOffset + 10f, yOffset + 2, string.Format("{0}{1} / {2}", name, chealth, mhealth));
            _graphics.DrawText(_consolasBold, config.FontSize, TextColor, endOfBar, yOffset + 2, perc);
        }

        private void SetColors()
        {
            if (gameMemory.PlayerContext.HealthState == PlayerState.Fine) // Fine
            {
                HPBarColor = _darkgreen;
                TextColor = _lightgreen;
                return;
            }
            else if (gameMemory.PlayerContext.HealthState == PlayerState.Caution) // Caution (Yellow)
            {
                HPBarColor = _darkyellow;
                TextColor = _lightyellow;
                return;
            }
            else if (gameMemory.PlayerContext.HealthState == PlayerState.Danger) // Danger (Red)
            {
                HPBarColor = _darkred;
                TextColor = _lightred;
                return;
            }
            else
            {
                HPBarColor = _greydarker;
                TextColor = _white;
                return;
            }
        }


        private void DrawOverlay()
        {
            if (gameMemory.Timer.MeasureDemoSpendingTime) return;
            float baseXOffset = config.PositionX;
            float baseYOffset = config.PositionY;

            // Player HP
            float statsXOffset = baseXOffset + 5f;
            float statsYOffset = baseYOffset + 0f;

            float textOffsetX = 0f;
            textOffsetX = config.PositionX + 15f;
            DrawTextBlock(ref textOffsetX, ref statsYOffset, "IGT:", gameMemory.Timer.IGTFormattedString, _lawngreen);

            PlayerName = string.Format("{0}: ", gameMemory.PlayerContext.SurvivorTypeString);
            SetColors();

            if (config.ShowHPBars)
            {
                if (gameMemory.PlayerContext.IsLoaded)
                    DrawHealthBar(ref statsXOffset, ref statsYOffset, PlayerName, gameMemory.PlayerContext.Health.CurrentHP, gameMemory.PlayerContext.Health.MaxHP, gameMemory.PlayerContext.Health.Percentage);
            }
            else
            {
                string perc = float.IsNaN(gameMemory.PlayerContext.Health.Percentage) ? "0%" : string.Format("{0:P1}", gameMemory.PlayerContext.Health.Percentage);
                if (gameMemory.PlayerContext.IsLoaded)
                    _graphics?.DrawText(_consolasBold, 20f, TextColor, statsXOffset + 10f, statsYOffset += 24, string.Format("{0}{1} / {2} {3:P1}", PlayerName, gameMemory.PlayerContext.Health.CurrentHP, gameMemory.PlayerContext.Health.MaxHP, perc));
            }

            textOffsetX = 0f;
            if (config.Debug)
            {
                _graphics?.DrawText(_consolasBold, config.FontSize, _grey, statsXOffset, statsYOffset += 24, "Raw IGT");
                _graphics?.DrawText(_consolasBold, config.FontSize, _grey, statsXOffset, statsYOffset += 24, string.Format("A:{0}", gameMemory.Timer.GameSaveData.GameElapsedTime.ToString("00000000000000000000")));
                _graphics?.DrawText(_consolasBold, config.FontSize, _grey, statsXOffset, statsYOffset += 24, string.Format("C:{0}", gameMemory.Timer.GameSaveData.DemoSpendingTime.ToString("00000000000000000000")));
                _graphics?.DrawText(_consolasBold, config.FontSize, _grey, statsXOffset, statsYOffset += 24, string.Format("M:{0}", gameMemory.Timer.GameSaveData.InventorySpendingTime.ToString("00000000000000000000")));
                _graphics?.DrawText(_consolasBold, config.FontSize, _grey, statsXOffset, statsYOffset += 24, string.Format("P:{0}", gameMemory.Timer.GameSaveData.PauseSpendingTime.ToString("00000000000000000000")));
            }

            if (config.ShowPosition)
            {
                textOffsetX = config.PositionX + 15f;
                statsYOffset += 32;
                DrawTextBlockRow(ref textOffsetX, ref statsYOffset, "X:", gameMemory.PlayerContext.Position.X.ToString("F3"), _lawngreen);
                DrawTextBlockRow(ref textOffsetX, ref statsYOffset, "Y:", gameMemory.PlayerContext.Position.Y.ToString("F3"), _lawngreen);
                DrawTextBlockRow(ref textOffsetX, ref statsYOffset, "Z:", gameMemory.PlayerContext.Position.Z.ToString("F3"), _lawngreen);
            }

            if (config.ShowRotation)
            {
                textOffsetX = config.PositionX + 15f;
                statsYOffset += 24;
                DrawTextBlockRow(ref textOffsetX, ref statsYOffset, "RW:", gameMemory.PlayerContext.Rotation.W.ToString("F3"), _lawngreen);
                DrawTextBlockRow(ref textOffsetX, ref statsYOffset, "RY:", gameMemory.PlayerContext.Rotation.Y.ToString("F3"), _lawngreen);
            }

            if (config.ShowDifficultyAdjustment)
            {
                textOffsetX = config.PositionX + 15f;
                DrawTextBlock(ref textOffsetX, ref statsYOffset, "Rank:", gameMemory.Rank.Rank.ToString(), _lawngreen);
                textOffsetX = config.PositionX + 15f;
                DrawTextBlock(ref textOffsetX, ref statsYOffset, "Action Point:", gameMemory.Rank.ActionPoint.ToString(), _lawngreen);
                textOffsetX = config.PositionX + 15f;
                DrawTextBlock(ref textOffsetX, ref statsYOffset, "Item Point:", gameMemory.Rank.ItemPoint.ToString(), _lawngreen);
            }

            textOffsetX = config.PositionX + 15f;
            DrawTextBlock(ref textOffsetX, ref statsYOffset, "Kill Count:", gameMemory.GameStatsKillCountElement.Count.ToString(), _lawngreen);

            // Enemy HP
            var xOffset = config.EnemyHPPositionX == -1 ? statsXOffset : config.EnemyHPPositionX;
            var yOffset = config.EnemyHPPositionY == -1 ? statsYOffset : config.EnemyHPPositionY;
            foreach (PlayerContext enemy in gameMemory.Enemies.Where(a => a.Health.IsAlive).OrderBy(a => a.Health.MaxHP).ThenBy(a => a.Health.Percentage).ThenByDescending(a => a.Health.CurrentHP))
                if (config.ShowHPBars)
                {
                    DrawProgressBar(ref xOffset, ref yOffset, enemy.SurvivorTypeString, enemy.Health.CurrentHP, enemy.Health.MaxHP, enemy.Health.Percentage);
                }
                else
                {
                    _graphics.DrawText(_consolasBold, config.FontSize, _white, xOffset + 10f, yOffset += 28f, string.Format("{0} / {1} {2:P1}", enemy.Health.CurrentHP, enemy.Health.MaxHP, enemy.Health.Percentage));
                }
        }

    }

}
