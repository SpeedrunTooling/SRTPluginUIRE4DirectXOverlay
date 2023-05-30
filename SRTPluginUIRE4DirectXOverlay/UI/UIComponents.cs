using SRTPluginProducerRE4R;
using SRTPluginProducerRE4R.Structs;
using GameOverlay.Drawing;
using GameOverlay.Windows;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Drawing.Text;

namespace SRTPluginUIRE4DirectXOverlay.UI
{
	public class UIComponents
	{
        public Dictionary<string, SolidBrush?> brushes;
        public Dictionary<string, Font?> fonts;

		public SolidBrush?[] fine;
		public SolidBrush?[] caution;
		public SolidBrush?[] danger;
        public SolidBrush?[] dead;

        private float GetHPBarSize(PluginConfiguration? config) => (config?.FontSize ?? Constants.DEFAULT_FONT_SIZE) * 20;
		private float GetBossBarSize(float s) => s * 32;
		public UIComponents(Graphics? _graphics)
		{
            brushes = new Dictionary<string, SolidBrush?>()
			{
				{ "black", _graphics?.CreateSolidBrush(0, 0, 0, 200) },
				{ "white", _graphics?.CreateSolidBrush(255, 255, 255) },
				{ "lightgrey", _graphics?.CreateSolidBrush(128, 128, 128) },
				{ "grey", _graphics?.CreateSolidBrush(64, 64, 64) },
				{ "darkgrey", _graphics?.CreateSolidBrush(24, 24, 24) },
				{ "lightgreen", _graphics?.CreateSolidBrush(150, 255, 150) },
				{ "green", _graphics?.CreateSolidBrush(124, 252, 0) },
				{ "darkgreen", _graphics?.CreateSolidBrush(0, 102, 0, 100) },
				{ "lightyellow", _graphics?.CreateSolidBrush(255, 255, 150) },
                { "yellow", _graphics?.CreateSolidBrush(249, 115, 22) },
                { "darkyellow", _graphics?.CreateSolidBrush(218, 165, 32, 100) },
				{ "lightred", _graphics?.CreateSolidBrush(255, 172, 172) },
				{ "red", _graphics?.CreateSolidBrush(248, 113, 113) },
				{ "darkred", _graphics?.CreateSolidBrush(153, 0, 0, 100) },
			};

            fonts = new Dictionary<string, Font?>();
			var fetchedFonts = GetAvailableFonts();
			if (fetchedFonts.Count > 0)
				foreach (string font in fetchedFonts)
				{
					fonts.Add(font, _graphics?.CreateFont(font, 16f, false));
					fonts.Add(font + " Bold", _graphics?.CreateFont(font, 16f, true));
				}

            fine = new SolidBrush?[2] { brushes["darkgreen"], brushes["lightgreen"] };
            caution = new SolidBrush?[2] { brushes["darkyellow"], brushes["lightyellow"] };
            danger = new SolidBrush?[2] { brushes["darkred"], brushes["lightred"] };
            dead = new SolidBrush?[2] { brushes["darkgrey"], brushes["lightgrey"] };
        }

		public List<string> GetAvailableFonts()
		{
			var results = new List<string>();
			var fontCollection = new InstalledFontCollection();

			foreach (var fontFamily in fontCollection.Families)
			{
				results.Add(fontFamily.Name);
			}

			return results;
		}

        public SolidBrush?[] GetColors(PlayerState healthState)
		{
			if (healthState == PlayerState.Fine) // Fine
                return fine;
			else if (healthState == PlayerState.Caution) // Caution (Yellow)
                return caution;
			else if (healthState == PlayerState.Danger) // Danger (Red)
                return danger;
			else
                return dead;
		}

        public SolidBrush? GetColor(PlayerState healthState)
        {
            if (healthState == PlayerState.Fine) // Fine
                return brushes!["green"];
            else if (healthState == PlayerState.Caution) // Caution (Yellow)
                return brushes!["yellow"];
            else if (healthState == PlayerState.Danger) // Danger (Red)
                return brushes!["red"];
            else
                return brushes!["white"];
        }

        public Point GetStringSize(Graphics? _graphics, Font? fontType, string str, float size = 16f) => _graphics?.MeasureString(fontType, size, str) ?? default;

		private float AlignRight(
			Graphics? _graphics, 
			Font? fontType, 
			string textString, 
			float startPosition, 
			float width
		) => (startPosition + width) - GetStringSize(_graphics, fontType, textString).X;

		public void DrawTextBlock(Graphics? _graphics, PluginConfiguration? config, ref float dx, ref float dy, string label, string val, SolidBrush? color)
		{
			Point gfxSize = GetStringSize(_graphics, fonts[config?.StringFontName ?? Constants.DEFAULT_FONT_NAME + " Bold"], label, config?.FontSize ?? Constants.DEFAULT_FONT_SIZE);
			_graphics?.DrawText(fonts[config?.StringFontName ?? Constants.DEFAULT_FONT_NAME + " Bold"], config?.FontSize ?? Constants.DEFAULT_FONT_SIZE, brushes["white"], dx, dy += gfxSize.Y * 1.2f, label);
			var dx2 = dx + gfxSize.X + (config?.FontSize ?? Constants.DEFAULT_FONT_SIZE);
			_graphics?.DrawText(fonts[config?.StringFontName ?? string.Empty], config?.FontSize ?? Constants.DEFAULT_FONT_SIZE, color, dx2, dy, val);
		}

		public void DrawTextBlockRow(Graphics? _graphics, PluginConfiguration? config, ref float dx, ref float dy, string label, string val, SolidBrush? color)
		{
			float marginX = 40f;
			Point gfxSize = GetStringSize(_graphics, fonts[config?.StringFontName ?? Constants.DEFAULT_FONT_NAME + " Bold"], label, config?.FontSize ?? Constants.DEFAULT_FONT_SIZE);
			Point gfxSize2 = GetStringSize(_graphics, fonts[config?.StringFontName ?? Constants.DEFAULT_FONT_NAME + " Bold"], val, config?.FontSize ?? Constants.DEFAULT_FONT_SIZE);
			_graphics?.DrawText(fonts[config?.StringFontName ?? Constants.DEFAULT_FONT_NAME + " Bold"], config?.FontSize ?? Constants.DEFAULT_FONT_SIZE, brushes["white"], dx, dy, label);
			var dx2 = dx + gfxSize.X + (config?.FontSize ?? Constants.DEFAULT_FONT_SIZE);
			_graphics?.DrawText(fonts[config?.StringFontName ?? string.Empty], config?.FontSize ?? Constants.DEFAULT_FONT_SIZE, color, dx2, dy, val);
			dx += gfxSize.X + gfxSize2.X + marginX;
		}

		public void DrawTextBlockRows(Graphics? _graphics, PluginConfiguration? config, ref float dx, ref float dy, List<string> labels, List<string> vals, SolidBrush? color)
		{
			float marginX = 40f;

			List<bool> enabled = new List<bool>()
			{
				config?.ShowIGT ?? default,
				config?.ShowPTAS ?? default,
				config?.ShowPTAS ?? default,
				config?.ShowPosition ?? default,
				config?.ShowPosition ?? default,
				config?.ShowPosition ?? default,
				config?.ShowRotation ?? default,
				config?.ShowRotation ?? default,
				config?.ShowDifficultyAdjustment ?? default,
				config?.ShowDifficultyAdjustment ?? default,
				config?.ShowDifficultyAdjustment ?? default,
				config?.ShowDifficultyAdjustment ?? default,
				config?.ShowDuffle ?? default,
			};
			dx = 8f;
			dy = 0;

			float xLength = 0;
			float yHeight = config?.FontSize ?? Constants.DEFAULT_FONT_SIZE;
			for (var i = 0; i < labels.Count; i++)
			{
				if (!enabled[i]) continue;
				Point gfxSize = GetStringSize(_graphics, fonts[config?.StringFontName ?? Constants.DEFAULT_FONT_NAME + " Bold"], labels[i], config?.FontSize ?? Constants.DEFAULT_FONT_SIZE);
				Point gfxSize2 = GetStringSize(_graphics, fonts[config?.StringFontName ?? Constants.DEFAULT_FONT_NAME + " Bold"], vals[i], config?.FontSize ?? Constants.DEFAULT_FONT_SIZE);
				xLength += gfxSize.X + ((config?.FontSize ?? Constants.DEFAULT_FONT_SIZE) * 2.5f) + gfxSize2.X;
				yHeight = gfxSize.Y + 2f;
			}
			_graphics?.FillRectangle(brushes["black"], 0, 0, xLength, yHeight);

			for (var i = 0; i < labels.Count; i++)
			{
				if (!enabled[i]) continue;
				if (vals[i] == "Off") color = brushes["red"];
				Point gfxSize = GetStringSize(_graphics, fonts[config?.StringFontName ?? Constants.DEFAULT_FONT_NAME + " Bold"], labels[i], config?.FontSize ?? Constants.DEFAULT_FONT_SIZE);
				Point gfxSize2 = GetStringSize(_graphics, fonts[config?.StringFontName ?? Constants.DEFAULT_FONT_NAME + " Bold"], vals[i], config?.FontSize ?? Constants.DEFAULT_FONT_SIZE);
				_graphics?.DrawText(fonts[config?.StringFontName ?? Constants.DEFAULT_FONT_NAME + " Bold"], config?.FontSize ?? Constants.DEFAULT_FONT_SIZE, brushes["white"], dx, dy, labels[i]);
				var dx2 = dx + gfxSize.X + (config?.FontSize ?? Constants.DEFAULT_FONT_SIZE);
				_graphics?.DrawText(fonts[config?.StringFontName ?? string.Empty], config?.FontSize ?? Constants.DEFAULT_FONT_SIZE, color, dx2, dy, vals[i]);
				dx += gfxSize.X + gfxSize2.X + marginX;
			}
		}

        public void DrawHP(
			Graphics? _graphics,
			OverlayWindow? _window,
			PluginConfiguration? config,
			PlayerContext? pc,
			HPType hpType,
			HPPosition hpPosition,
			ref float xOffset,
			ref float yOffset,
			float steps
		) => DrawHPBar(_graphics, _window, config, pc, hpPosition, ref xOffset, ref yOffset, steps);

		private float getHPPercentPosition(float width, float x, float x2, float padding) => x + width - x2 - padding;

		private Rectangle CreateHPContainer(OverlayWindow? _window, PluginConfiguration? config, HPPosition hpPosition, ref float xOffset, ref float yOffset, float widthBar, float heightBar, float steps)
		{
			var x = 0f;
			var y = 0f;

			if (steps == 0f)
			{
				x = config?.EnemyHPPositionX ?? default;
                y = config?.EnemyHPPositionY ?? default;
            }
            else
            {
                x = config?.PlayerHPPositionX ?? default;
                y = config?.PlayerHPPositionY ?? default;
            }

            if (hpPosition == HPPosition.Left)
                return new Rectangle(xOffset, yOffset += heightBar, xOffset + widthBar, yOffset + heightBar);
            else if (hpPosition == HPPosition.Center)
                return new Rectangle((((_window?.Width ?? default) - widthBar) / 2f), ((_window?.Height ?? default) - (heightBar * steps)), (((_window?.Width ?? default) - widthBar) / 2f) + widthBar, ((_window?.Height ?? default) - (heightBar * steps) + heightBar));
			else if (hpPosition == HPPosition.Right)
                return new Rectangle((_window?.Width ?? default) - widthBar - (config?.PositionX ?? default), yOffset += heightBar, (_window?.Width ?? default) - (config?.PositionX ?? default), yOffset + heightBar);
			else
                return new Rectangle(x, y += heightBar, x + widthBar, y + heightBar);
        }
        
		public void DrawHPBar(Graphics? _graphics, OverlayWindow? _window, PluginConfiguration? config, PlayerContext? pc, HPPosition hpPosition, ref float xOffset, ref float yOffset, float steps)
		{
			// If show hp disabled cancel draw action
			if (!config?.ShowHPBars ?? default) return;
            // If show damaged enemies only and enemy undamaged cancel draw action
            if ((config?.ShowDamagedEnemiesOnly ?? default) && steps == 0f && (pc?.Health?.Percentage ?? 0f) == 1f) return;
			// If show boss only checked and is not boss cacel draw action
            if ((config?.ShowBossOnly ?? default) && (!pc?.IsBoss ?? default)) return;
            // If is boss and center boss hp is checked then reroute draw method to DrawBossBar
            if ((pc?.IsBoss ?? default) && (config?.CenterBossHP ?? default))
			{
				DrawBossBar(_graphics, _window, config, pc);
				return;
			}
			// Else continue drawing player and enemy health HUD
            string percentString = string.Format("{0:P1}", pc?.Health?.Percentage ?? 0f);
            Point gfxSize = GetStringSize(_graphics, fonts[config?.StringFontName ?? string.Empty], percentString, config?.FontSize ?? Constants.DEFAULT_FONT_SIZE);

            float widthBar = GetHPBarSize(config);
            float heightBar = (gfxSize.Y / 4) + gfxSize.Y;
			Rectangle rect;

			// Set rect to match position alignment
			rect = CreateHPContainer(_window, config, hpPosition, ref xOffset, ref yOffset, widthBar, heightBar, steps);

            float endOfBar = getHPPercentPosition(widthBar, rect.Left, gfxSize.X, 8f);

			// Draws HP as progress bar with text info
            if (((HPType)(config?.PlayerHPType ?? default)) == HPType.Bar)
			{
				var colors = GetColors(steps != 0f ? pc?.HealthState ?? PlayerState.Dead : PlayerState.Danger);
				_graphics?.DrawRectangle(brushes["lightgrey"], rect.Left, rect.Top, rect.Right, rect.Bottom, 4f);
				_graphics?.FillRectangle(brushes["darkgrey"], rect.Left + 1f, rect.Top + 1f, rect.Right - 2f, rect.Bottom - 2f);
				_graphics?.FillRectangle(colors[0], rect.Left + 1f, rect.Top + 1f, hpPosition == HPPosition.Left ? ((rect.Right - 2f) * (pc?.Health?.Percentage ?? 0f)) : (rect.Left + ((widthBar * (pc?.Health?.Percentage ?? 0f)) - 2f)), rect.Bottom - 2f);
				_graphics?.DrawText(fonts[config?.StringFontName ?? Constants.DEFAULT_FONT_NAME + " Bold"], config?.FontSize ?? Constants.DEFAULT_FONT_SIZE, colors[1], rect.Left + 8f, rect.Top + 1, string.Format("{0} {1} / {2}", pc?.SurvivorTypeString.Replace("_", "") ?? string.Empty, pc?.Health?.CurrentHP, pc?.Health?.MaxHP));
				_graphics?.DrawText(fonts[config?.StringFontName ?? Constants.DEFAULT_FONT_NAME + " Bold"], config?.FontSize ?? Constants.DEFAULT_FONT_SIZE, colors[1], endOfBar, rect.Top + 1, percentString);
			}
			// Draws HP as text info only
			else
			{
                var color = GetColor(steps != 0f ? pc?.HealthState ?? PlayerState.Dead : PlayerState.Danger);
                _graphics?.FillRectangle(brushes["black"], rect.Left + 1f, rect.Top + 1f, rect.Right - 2f, rect.Bottom - 2f);
                _graphics?.DrawText(fonts[config?.StringFontName ?? Constants.DEFAULT_FONT_NAME + " Bold"], config?.FontSize ?? Constants.DEFAULT_FONT_SIZE, color, rect.Left + 8f, rect.Top + 1, string.Format("{0} {1} / {2}", pc?.SurvivorTypeString.Replace("_", "") ?? string.Empty, pc?.Health?.CurrentHP, pc?.Health?.MaxHP));
                _graphics?.DrawText(fonts[config?.StringFontName ?? Constants.DEFAULT_FONT_NAME + " Bold"], config?.FontSize ?? Constants.DEFAULT_FONT_SIZE, color, endOfBar, rect.Top + 1, percentString);
            }
        }

		private void DrawBossBar(Graphics? _graphics, OverlayWindow? window, PluginConfiguration? config, PlayerContext? pc)
		{
			string name = pc?.SurvivorTypeString.Replace("_", " ").ToUpperInvariant() ?? string.Empty;
            float fSize = 24f;
			float widthBar = GetBossBarSize(fSize);
			float heightBar = (fSize / 2f) + fSize;
			var xOffset = (((window?.Width ?? default) / 2f) - (widthBar / 2f)) * (config?.ScalingFactor ?? default);
			var yOffset = 4f * (config?.ScalingFactor ?? default);
			if (pc?.SurvivorTypeString.Contains("dog", StringComparison.InvariantCultureIgnoreCase) ?? default) return;
			if ((config?.ShowDamagedEnemiesOnly ?? default) && pc?.Health?.Percentage == 1f) return;
			string perc = float.IsNaN(pc?.Health?.Percentage ?? 0f) ? "0%" : string.Format("{0:P1}", pc?.Health?.Percentage);
			Point gfxSize = GetStringSize(_graphics, fonts[config?.StringFontName ?? Constants.DEFAULT_FONT_NAME + " Bold"], perc, fSize);
			float endOfBar = ((window?.Width ?? default) / 2f) - (widthBar / 2f) + widthBar - gfxSize.X - 8f;
			_graphics?.DrawRectangle(brushes["lightgrey"], xOffset, yOffset += 28f, xOffset + widthBar, yOffset + heightBar, 4f);
			_graphics?.FillRectangle(brushes["darkgrey"], xOffset + 1f, yOffset + 1f, xOffset + widthBar - 2f, yOffset + (heightBar - 2f));
			_graphics?.FillRectangle(brushes["darkred"], xOffset + 1f, yOffset + 1f, xOffset + ((widthBar - 2f) * pc?.Health?.Percentage ?? 0f), yOffset + heightBar - 2f);
			_graphics?.DrawText(fonts[config?.StringFontName ?? Constants.DEFAULT_FONT_NAME + " Bold"], fSize, brushes["lightred"], xOffset + 8f, yOffset, string.Format("{0} {1} / {2}", name, chealth, mhealth));
			_graphics?.DrawText(fonts[config?.StringFontName ?? Constants.DEFAULT_FONT_NAME + " Bold"], fSize, brushes["lightred"], endOfBar, yOffset, perc);
		}

		private long TimestampCalculated(long timestamp) => unchecked(timestamp);

		private long CalculatedTicks(long timestamp) => unchecked(TimestampCalculated(timestamp) * 10L);

		private TimeSpan TicksToTimeSpan(long ticks)
		{
			TimeSpan timespan;
			if (ticks <= TimeSpan.MaxValue.Ticks)
				timespan = new TimeSpan(ticks);
			else
				timespan = new TimeSpan();
			return timespan;
		}

		private const string TIMESPAN_STRING_FORMAT = @"hh\:mm\:ss";

		public string FormattedString(long timestamp) => TicksToTimeSpan(CalculatedTicks(timestamp)).ToString(TIMESPAN_STRING_FORMAT, CultureInfo.InvariantCulture);

		public void Dispose()
		{
            foreach (var pair in brushes) pair.Value?.Dispose();
            foreach (var pair in fonts) pair.Value?.Dispose();
            foreach (var c in fine) c?.Dispose();
            foreach (var c in caution) c?.Dispose();
            foreach (var c in danger) c?.Dispose();
            foreach (var c in dead) c?.Dispose();
            return;
		}

		public async ValueTask DisposeAsync()
		{
			Dispose();
			await Task.CompletedTask;
		}
    }
}
