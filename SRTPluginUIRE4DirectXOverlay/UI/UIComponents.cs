using SRTPluginProducerRE4R;
using SRTPluginProducerRE4R.Structs;
using GameOverlay.Drawing;
using GameOverlay.Windows;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Drawing.Text;
using System.Diagnostics;

namespace SRTPluginUIRE4DirectXOverlay.UI
{
	public class UIComponents
	{
		public Dictionary<string, SolidBrush?> brushes;
        public Dictionary<string, Font?> fonts;

		private SolidBrush?[]? playerHPColors;
		private SolidBrush?[]? ashleyHPColors;
		private SolidBrush?[]? luisHPColors;

		private float GetHPBarSize(PluginConfiguration? config) => (config?.FontSize ?? Constants.DEFAULT_FONT_SIZE) * 20;
		private float GetHPBarSize2(PluginConfiguration? config) => (config?.FontSize ?? Constants.DEFAULT_FONT_SIZE) * 12;
		private float GetBossBarSize(float s) => s * 32;
		public UIComponents(Graphics? _graphics, PluginConfiguration? config)
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
				{ "darkyellow", _graphics?.CreateSolidBrush(218, 165, 32, 100) },
				{ "lightred", _graphics?.CreateSolidBrush(255, 172, 172) },
				{ "red", _graphics?.CreateSolidBrush(255, 0, 0) },
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

			playerHPColors = new SolidBrush?[2]
			{
				brushes["darkgrey"],
				brushes["lightgrey"]
			};
			ashleyHPColors = new SolidBrush?[2]
			{
				brushes["darkgrey"],
				brushes["lightgrey"]
			};
			luisHPColors = new SolidBrush?[2]
			{
				brushes["darkgrey"],
				brushes["lightgrey"]
			};
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

		private SolidBrush?[] GetColor(string name)
		{
			if (name.Contains("LEON") || name.Contains("ASHLEY") && !name.Contains('_'))
				return new SolidBrush?[2] { playerHPColors?[0], playerHPColors?[1] };
			if (name.Contains("LUIS"))
				return new SolidBrush?[2] { luisHPColors?[0], luisHPColors?[1] };
			return new SolidBrush?[2] { ashleyHPColors?[0], ashleyHPColors?[1] };
		}

		private void SetColors(PlayerState healthState, SolidBrush?[]? color)
		{
			if (color is null)
				return;

			if (healthState == PlayerState.Fine) // Fine
			{
				color[0] = brushes["darkgreen"];
				color[1] = brushes["lightgreen"];
			}
			else if (healthState == PlayerState.Caution) // Caution (Yellow)
			{
				color[0] = brushes["darkyellow"];
				color[1] = brushes["lightyellow"];
			}
			else if (healthState == PlayerState.Danger) // Danger (Red)
			{
				color[0] = brushes["darkred"];
				color[1] = brushes["lightred"];
			}
			else
			{
				color[0] = brushes["darkgrey"];
				color[1] = brushes["lightgrey"];
			}
		}

		public Point GetStringSize(Graphics? _graphics, Font? fontType, string str, float size = 16f)
		{
			return _graphics?.MeasureString(fontType, size, str) ?? default;
		}

		private float AlignRight(Graphics? _graphics, Font? fontType, string textString, float startPosition, float width) => (startPosition + width) - GetStringSize(_graphics, fontType, textString).X;

		public void DrawTextBlock(Graphics? _graphics, PluginConfiguration? config, ref float dx, ref float dy, string label, string val, SolidBrush? color)
		{
			Point gfxSize = GetStringSize(_graphics, fonts[config?.StringFontName ?? Constants.DEFAULT_FONT_NAME + " Bold"], label, config?.FontSize ?? Constants.DEFAULT_FONT_SIZE);
			_graphics?.DrawText(fonts[config?.StringFontName ?? Constants.DEFAULT_FONT_NAME + " Bold"], config?.FontSize ?? Constants.DEFAULT_FONT_SIZE, brushes["white"], dx, dy += gfxSize.Y * 1.5f, label);
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

		// PLAYER AND PARTNER HP METHODS
		public void DrawPlayerHP(Graphics? _graphics, OverlayWindow? _window, PluginConfiguration? config, PlayerContext? pc, string _playerName, ref float xOffset, ref float yOffset)
		{
			SetColors(pc?.HealthState ?? default, playerHPColors);
			if (config?.ShowHPBars ?? default)
			{
			    if (pc?.IsLoaded ?? default)
				{
					if (config?.CenterPlayerHP ?? default)
					{
						DrawPlayerBar(_graphics, _window, config, _playerName, pc?.Health?.CurrentHP ?? default, pc?.Health?.MaxHP ?? default, pc?.Health?.Percentage ?? default);
						return;
					}
					DrawHealthBar(_graphics, config, ref xOffset, ref yOffset, _playerName, pc?.Health?.CurrentHP ?? default, pc?.Health?.MaxHP ?? default, pc?.Health?.Percentage ?? default);
				}
			}
		}

		public void DrawPartnerHP(Graphics? _graphics, OverlayWindow? _window, PluginConfiguration? config, PlayerContext? pc, string playerName, ref float xOffset, ref float yOffset)
		{
			SolidBrush?[]? colors = playerName.Contains("ASHLEY") ? ashleyHPColors : luisHPColors;
			SetColors(pc?.HealthState ?? default, colors);
			if (config?.ShowHPBars ?? default)
			{
				if (pc?.IsLoaded ?? default)
				{
					if ((config?.CenterPlayerHP ?? default) && playerName.Contains("ASHLEY"))
					{
						DrawPartnerBar(_graphics, _window, config, playerName, pc?.Health?.CurrentHP ?? default, pc?.Health?.MaxHP ?? default, pc?.Health?.Percentage ?? default);
						return;
					}
					DrawHealthBar(_graphics, config, ref xOffset, ref yOffset, playerName, pc?.Health?.CurrentHP ?? default, pc?.Health?.MaxHP ?? default, pc?.Health?.Percentage ?? default);
				}
			}
		}

		private void DrawHealthBar(Graphics? _graphics, PluginConfiguration? config, ref float xOffset, ref float yOffset, string name, float chealth, float mhealth, float percentage = 1f)
		{
			// Debugger.Break();
			string perc = float.IsNaN(percentage) ? "0%" : string.Format("{0:P1}", percentage);
			Point gfxSize = GetStringSize(_graphics, fonts[config?.StringFontName ?? string.Empty], perc, config?.FontSize ?? Constants.DEFAULT_FONT_SIZE);
			float widthBar = GetHPBarSize(config);
			float heightBar = (gfxSize.Y / 4) + gfxSize.Y;
			var colors = GetColor(name);
			float endOfBar = (config?.PositionX ?? default) + widthBar - gfxSize.X - 8f;
			_graphics?.DrawRectangle(brushes["lightgrey"], xOffset, yOffset += heightBar, xOffset + widthBar, yOffset + heightBar, 4f);
			_graphics?.FillRectangle(brushes["darkgrey"], xOffset + 1f, yOffset + 1f, xOffset + (widthBar - 2f), yOffset + (heightBar - 2f));
			_graphics?.FillRectangle(colors[0], xOffset + 1f, yOffset + 1f, xOffset + ((widthBar - 2f) * percentage), yOffset + (heightBar - 2f));
			_graphics?.DrawText(fonts[config?.StringFontName ?? Constants.DEFAULT_FONT_NAME + " Bold"], config?.FontSize ?? Constants.DEFAULT_FONT_SIZE, colors[1], xOffset + 10f, yOffset += 1, string.Format("{0}{1} / {2}", name.Replace("_", ""), chealth, mhealth));
			_graphics?.DrawText(fonts[config?.StringFontName ?? Constants.DEFAULT_FONT_NAME + " Bold"], config?.FontSize ?? Constants.DEFAULT_FONT_SIZE, colors[1], endOfBar, yOffset, perc);
		}

		private void DrawPlayerBar(Graphics? _graphics, OverlayWindow? window, PluginConfiguration? config, string name, float chealth, float mhealth, float percentage = 1f)
		{
			float widthBar = GetHPBarSize(config);
			float heightBar = (config?.FontSize ?? Constants.DEFAULT_FONT_SIZE) + 8f;
			var xOffset = (((window?.Width ?? default) / 2f) - (widthBar / 2f)) * (config?.ScalingFactor ?? default);
			var yOffset = ((window?.Height ?? default) - (heightBar * 4f)) * (config?.ScalingFactor ?? default);
			// var yOffset = (((window?.Height ?? default) / 2f) - (heightBar / 2f)) * (config?.ScalingFactor ?? default); // TODO: Squirrelies checking center.
			string perc = float.IsNaN(percentage) ? "0%" : string.Format("{0:P1}", percentage);
			Point gfxSize = GetStringSize(_graphics, fonts[config?.StringFontName ?? string.Empty], perc, config?.FontSize ?? Constants.DEFAULT_FONT_SIZE);
			float endOfBar = ((window?.Width ?? default) / 2f) - (widthBar / 2f) + widthBar - gfxSize.X - 8f;
			_graphics?.DrawRectangle(brushes["lightgrey"], xOffset, yOffset += heightBar + 2, xOffset + widthBar, yOffset + heightBar, 4f);
			_graphics?.FillRectangle(brushes["darkgrey"], xOffset + 1f, yOffset + 1f, xOffset + (widthBar - 2f), yOffset + (heightBar - 2f));
			_graphics?.FillRectangle(playerHPColors?[0], xOffset + 1f, yOffset + 1f, xOffset + ((widthBar - 2f) * percentage), yOffset + (heightBar - 2f));
			_graphics?.DrawText(fonts[config?.StringFontName ?? Constants.DEFAULT_FONT_NAME + " Bold"], config?.FontSize ?? Constants.DEFAULT_FONT_SIZE, playerHPColors?[1], xOffset + 10f, yOffset, string.Format("{0}{1} / {2}", name.Replace("_", ""), chealth, mhealth));
			_graphics?.DrawText(fonts[config?.StringFontName ?? Constants.DEFAULT_FONT_NAME + " Bold"], config?.FontSize ?? Constants.DEFAULT_FONT_SIZE, playerHPColors?[1], endOfBar, yOffset, perc);
		}

		private void DrawPartnerBar(Graphics? _graphics, OverlayWindow? window, PluginConfiguration? config, string name, float chealth, float mhealth, float percentage = 1f)
		{
			float widthBar = GetHPBarSize(config);
			float heightBar = (config?.FontSize ?? Constants.DEFAULT_FONT_SIZE) + 8f;
			var xOffset = (((window?.Width ?? default) / 2f) - (widthBar / 2f)) * (config?.ScalingFactor ?? default);
			var yOffset = ((window?.Height ?? default) - (heightBar * 3f)) + 2f * (config?.ScalingFactor ?? default);
			var colors = GetColor(name);
			string perc = float.IsNaN(percentage) ? "0%" : string.Format("{0:P1}", percentage);
			Point gfxSize = GetStringSize(_graphics, fonts[config?.StringFontName ?? string.Empty], perc, config?.FontSize ?? Constants.DEFAULT_FONT_SIZE);
			float endOfBar = ((window?.Width ?? default) / 2f) - (widthBar / 2f) + widthBar - gfxSize.X - 8f;
			_graphics?.DrawRectangle(brushes["lightgrey"], xOffset, yOffset += heightBar + 2, xOffset + widthBar, yOffset + heightBar, 4f);
			_graphics?.FillRectangle(brushes["darkgrey"], xOffset + 1f, yOffset + 1f, xOffset + (widthBar - 2f), yOffset + (heightBar - 2f));
			_graphics?.FillRectangle(colors[0], xOffset + 1f, yOffset + 1f, xOffset + ((widthBar - 2f) * percentage), yOffset + (heightBar - 2f));
			_graphics?.DrawText(fonts[config?.StringFontName ?? Constants.DEFAULT_FONT_NAME + " Bold"], config?.FontSize ?? Constants.DEFAULT_FONT_SIZE, colors[1], xOffset + 8f, yOffset, string.Format("{0}{1} / {2}", name.Replace("_", ""), chealth, mhealth));
			_graphics?.DrawText(fonts[config?.StringFontName ?? Constants.DEFAULT_FONT_NAME + " Bold"], config?.FontSize ?? Constants.DEFAULT_FONT_SIZE, colors[1], endOfBar, yOffset, perc);
		}

		// ENEMY HP METHODS
		private void DrawProgressBar(Graphics? _graphics, PluginConfiguration? config, ref float xOffset, ref float yOffset, string name, float chealth, float mhealth, float percentage = 1f)
		{
			if (name == "Dog") return;
			if ((config?.ShowDamagedEnemiesOnly ?? default) && percentage == 1f) return;

			string perc = float.IsNaN(percentage) ? "0%" : string.Format("{0:P1}", percentage);
			Point gfxSize = GetStringSize(_graphics, fonts[config?.StringFontName ?? string.Empty], perc, config?.FontSize ?? Constants.DEFAULT_FONT_SIZE);
			float widthBar = GetHPBarSize2(config);
			float heightBar = gfxSize.Y + 4f;
			float endOfBar = (config?.PositionX ?? default) + widthBar - gfxSize.X - 8f;

			_graphics?.DrawRectangle(brushes["lightgrey"], xOffset, yOffset += heightBar, xOffset + widthBar, yOffset + heightBar, 4f);
			_graphics?.FillRectangle(brushes["darkgrey"], xOffset + 1f, yOffset + 1f, xOffset + widthBar - 2f, yOffset + (heightBar - 2f));
			_graphics?.FillRectangle(brushes["darkred"], xOffset + 1f, yOffset + 1f, xOffset + ((widthBar - 2f) * percentage), yOffset + (heightBar - 2f));
			_graphics?.DrawText(fonts[config?.StringFontName ?? Constants.DEFAULT_FONT_NAME + " Bold"], config?.FontSize ?? Constants.DEFAULT_FONT_SIZE, brushes["lightred"], xOffset + 10f, yOffset += 1, string.Format("{0} / {1}", chealth, mhealth));
			_graphics?.DrawText(fonts[config?.StringFontName ?? Constants.DEFAULT_FONT_NAME + " Bold"], config?.FontSize ?? Constants.DEFAULT_FONT_SIZE, brushes["lightred"], endOfBar, yOffset, perc);
		}

		private void DrawBossBar(Graphics? _graphics, OverlayWindow? window, PluginConfiguration? config, string name, float chealth, float mhealth, float percentage = 1f)
		{
			float fSize = 24f;
			float widthBar = GetBossBarSize(fSize);
			float heightBar = (fSize / 2f) + fSize;
			var xOffset = (((window?.Width ?? default) / 2f) - (widthBar / 2f)) * (config?.ScalingFactor ?? default);
			var yOffset = 4f * (config?.ScalingFactor ?? default);
			if (name == "Dog") return;
			if ((config?.ShowDamagedEnemiesOnly ?? default) && percentage == 1f) return;
			string perc = float.IsNaN(percentage) ? "0%" : string.Format("{0:P1}", percentage);
			Point gfxSize = GetStringSize(_graphics, fonts[config?.StringFontName ?? Constants.DEFAULT_FONT_NAME + " Bold"], perc, fSize);
			float endOfBar = ((window?.Width ?? default) / 2f) - (widthBar / 2f) + widthBar - gfxSize.X - 8f;
			_graphics?.DrawRectangle(brushes["lightgrey"], xOffset, yOffset += 28f, xOffset + widthBar, yOffset + heightBar, 4f);
			_graphics?.FillRectangle(brushes["darkgrey"], xOffset + 1f, yOffset + 1f, xOffset + widthBar - 2f, yOffset + (heightBar - 2f));
			_graphics?.FillRectangle(brushes["darkred"], xOffset + 1f, yOffset + 1f, xOffset + ((widthBar - 2f) * percentage), yOffset + heightBar - 2f);
			_graphics?.DrawText(fonts[config?.StringFontName ?? Constants.DEFAULT_FONT_NAME + " Bold"], fSize, brushes["lightred"], xOffset + 8f, yOffset, string.Format("{0} {1} / {2}", name.Replace("_", " "), chealth, mhealth));
			_graphics?.DrawText(fonts[config?.StringFontName ?? Constants.DEFAULT_FONT_NAME + " Bold"], fSize, brushes["lightred"], endOfBar, yOffset, perc);
		}

		public void DrawEnemies(Graphics? graphics, OverlayWindow? window, PluginConfiguration? config, PlayerContext? enemy, ref float xOffset, ref float yOffset)
		{
			if (config?.ShowBossOnly ?? default)
			{
				if (!(enemy?.IsBoss ?? default)) return;
				if (enemy?.IsBoss ?? default)
				{
					if (config?.CenterBossHP ?? default) DrawBossBar(graphics, window, config, enemy?.SurvivorTypeString?.ToUpperInvariant() ?? string.Empty, enemy?.Health?.CurrentHP ?? default, enemy?.Health?.MaxHP ?? default, enemy?.Health?.Percentage ?? default);
					else DrawProgressBar(graphics, config, ref xOffset, ref yOffset, enemy?.SurvivorTypeString?.ToUpperInvariant() ?? string.Empty, enemy?.Health?.CurrentHP ?? default, enemy?.Health?.MaxHP ?? default, enemy?.Health?.Percentage ?? default);
				}
			}
			else if ((config?.CenterBossHP ?? default) && (enemy?.IsBoss ?? default))
				DrawBossBar(graphics, window, config, enemy?.SurvivorTypeString?.ToUpperInvariant() ?? string.Empty, enemy?.Health?.CurrentHP ?? default, enemy?.Health?.MaxHP ?? default, enemy?.Health?.Percentage ?? default);
			else
				DrawProgressBar(graphics, config, ref xOffset, ref yOffset, enemy?.SurvivorTypeString?.ToUpperInvariant() ?? string.Empty, enemy?.Health?.CurrentHP ?? default, enemy?.Health?.MaxHP ?? default, enemy?.Health?.Percentage ?? default);
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
            return;
		}

		public async ValueTask DisposeAsync()
		{
			Dispose();
			await Task.CompletedTask;
		}
	}
}
