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
		public Dictionary<string, SolidBrush> brushes;
        public Dictionary<string, Font> fonts;

		private SolidBrush[] PlayerHPColors;
		private SolidBrush[] AshleyHPColors;
		private SolidBrush[] LuisHPColors;

		private float GetHPBarSize(PluginConfiguration config) => config.FontSize * 20;
		private float GetHPBarSize2(PluginConfiguration config) => config.FontSize * 12;
		private float GetBossBarSize(float s) => s * 32;
		public UIComponents(Graphics _graphics, PluginConfiguration config)
		{
            brushes = new Dictionary<string, SolidBrush>()
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

            fonts = new Dictionary<string, Font>();
			var fetchedFonts = GetAvailableFonts();
			if (fetchedFonts.Count > 0)
				foreach (string font in fetchedFonts)
				{
					fonts.Add(font, _graphics?.CreateFont(font, 16f, false));
					fonts.Add(font + " Bold", _graphics?.CreateFont(font, 16f, true));
				}

			PlayerHPColors = new SolidBrush[2];
			AshleyHPColors = new SolidBrush[2];
			LuisHPColors = new SolidBrush[2];

			PlayerHPColors[0] = brushes["darkgrey"];
			PlayerHPColors[1] = brushes["lightgrey"];

            AshleyHPColors[0] = brushes["darkgrey"];
            AshleyHPColors[1] = brushes["lightgrey"];

            LuisHPColors[0] = brushes["darkgrey"];
            LuisHPColors[1] = brushes["lightgrey"];
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

		private SolidBrush[] GetColor(string name)
		{
			if (name.Contains("LEON") || name.Contains("ASHLEY") && !name.Contains("_"))
				return new SolidBrush[2] { PlayerHPColors[0], PlayerHPColors[1] };
			if (name.Contains("LUIS"))
				return new SolidBrush[2] { LuisHPColors[0], LuisHPColors[1] };
			return new SolidBrush[2] { AshleyHPColors[0], AshleyHPColors[1] };
		}

		private void SetColors(PlayerState healthState, SolidBrush[] c)
		{
			if (healthState == PlayerState.Fine) // Fine
			{
				c[0] = brushes["darkgreen"];
				c[1] = brushes["lightgreen"];
				return;
			}
			else if (healthState == PlayerState.Caution) // Caution (Yellow)
			{
				c[0] = brushes["darkyellow"];
				c[1] = brushes["lightyellow"];
				return;
			}
			else if (healthState == PlayerState.Danger) // Danger (Red)
			{
				c[0] = brushes["darkred"];
				c[1] = brushes["lightred"];
				return;
			}
			else
			{
				c[0] = brushes["darkgrey"];
				c[1] = brushes["lightgrey"];
				return;
			}
		}

		public Point GetStringSize(Graphics _graphics, Font fontType, string str, float size = 16f)
		{
			Point sizePoint = (Point)_graphics?.MeasureString(fontType, size, str);
			return new Point(sizePoint.X, sizePoint.Y);
		}

		private float AlignRight(Graphics _graphics, Font fontType, string textString, float startPosition, float width) => (startPosition + width) - GetStringSize(_graphics, fontType, textString).X;

		public void DrawTextBlock(Graphics _graphics, PluginConfiguration config, ref float dx, ref float dy, string label, string val, SolidBrush color)
		{
			Point gfxSize = GetStringSize(_graphics, fonts[config.StringFontName + " Bold"], label, config.FontSize);
			_graphics?.DrawText(fonts[config.StringFontName + " Bold"], config.FontSize, brushes["white"], dx, dy += gfxSize.Y * 1.5f, label);
			var dx2 = dx + gfxSize.X + config.FontSize;
			_graphics?.DrawText(fonts[config.StringFontName], config.FontSize, color, dx2, dy, val);
		}

		public void DrawTextBlockRow(Graphics _graphics, PluginConfiguration config, ref float dx, ref float dy, string label, string val, SolidBrush color)
		{
			float marginX = 40f;
			Point gfxSize = GetStringSize(_graphics, fonts[config.StringFontName + " Bold"], label, config.FontSize);
			Point gfxSize2 = GetStringSize(_graphics, fonts[config.StringFontName + " Bold"], val, config.FontSize);
			_graphics?.DrawText(fonts[config.StringFontName + " Bold"], config.FontSize, brushes["white"], dx, dy, label);
			var dx2 = dx + gfxSize.X + config.FontSize;
			_graphics?.DrawText(fonts[config.StringFontName], config.FontSize, color, dx2, dy, val);
			dx += gfxSize.X + gfxSize2.X + marginX;
		}

		public void DrawTextBlockRows(Graphics _graphics, PluginConfiguration config, ref float dx, ref float dy, List<string> labels, List<string> vals, SolidBrush color)
		{
			float marginX = 40f;

			List<bool> enabled = new List<bool>()
			{
				config.Debug,
				config.ShowPTAS,
				config.ShowPTAS,
				config.ShowPosition,
				config.ShowPosition,
				config.ShowPosition,
				config.ShowRotation,
				config.ShowRotation,
				config.ShowDifficultyAdjustment,
				config.ShowDifficultyAdjustment,
				config.ShowDifficultyAdjustment,
				config.ShowDifficultyAdjustment,
				config.ShowDuffle,
			};
			dx = 8f;
			dy = 0;

			float xLength = 0;
			float yHeight = config.FontSize;
			for (var i = 0; i < labels.Count; i++)
			{
				if (!enabled[i]) continue;
				Point gfxSize = GetStringSize(_graphics, fonts[config.StringFontName + " Bold"], labels[i], config.FontSize);
				Point gfxSize2 = GetStringSize(_graphics, fonts[config.StringFontName + " Bold"], vals[i], config.FontSize);
				xLength += gfxSize.X + (config.FontSize * 2.5f) + gfxSize2.X;
				yHeight = gfxSize.Y + 2f;
			}
			_graphics.FillRectangle(brushes["black"], 0, 0, xLength, yHeight);

			for (var i = 0; i < labels.Count; i++)
			{
				if (!enabled[i]) continue;
				if (vals[i] == "Off") color = brushes["red"];
				Point gfxSize = GetStringSize(_graphics, fonts[config.StringFontName + " Bold"], labels[i], config.FontSize);
				Point gfxSize2 = GetStringSize(_graphics, fonts[config.StringFontName + " Bold"], vals[i], config.FontSize);
				_graphics?.DrawText(fonts[config.StringFontName + " Bold"], config.FontSize, brushes["white"], dx, dy, labels[i]);
				var dx2 = dx + gfxSize.X + config.FontSize;
				_graphics?.DrawText(fonts[config.StringFontName], config.FontSize, color, dx2, dy, vals[i]);
				dx += gfxSize.X + gfxSize2.X + marginX;
			}
		}

		// PLAYER AND PARTNER HP METHODS
		public void DrawPlayerHP(Graphics _graphics, OverlayWindow _window, PluginConfiguration config, PlayerContext pc, string _playerName, ref float xOffset, ref float yOffset)
		{
			SetColors(pc.HealthState, PlayerHPColors);
			if (config.ShowHPBars)
			{
			    if (pc.IsLoaded)
				{
					if (config.CenterPlayerHP)
					{
						DrawPlayerBar(_graphics, _window, config, _playerName, pc.Health.CurrentHP, pc.Health.MaxHP, pc.Health.Percentage);
						return;
					}
					DrawHealthBar(_graphics, config, ref xOffset, ref yOffset, _playerName, pc.Health.CurrentHP, pc.Health.MaxHP, pc.Health.Percentage);
				}
			}
		}

		public void DrawPartnerHP(Graphics _graphics, OverlayWindow _window, PluginConfiguration config, PlayerContext pc, string _playerName, ref float xOffset, ref float yOffset)
		{
			SolidBrush[] colors = _playerName.ToUpper().Contains("ASHLEY") ? AshleyHPColors : LuisHPColors;
			SetColors(pc.HealthState, colors);
			if (config.ShowHPBars)
			{
				if (pc.IsLoaded)
				{
					if (config.CenterPlayerHP && _playerName.Contains("ASHLEY"))
					{
						DrawPartnerBar(_graphics, _window, config, _playerName, pc.Health.CurrentHP, pc.Health.MaxHP, pc.Health.Percentage);
						return;
					}
					DrawHealthBar(_graphics, config, ref xOffset, ref yOffset, _playerName, pc.Health.CurrentHP, pc.Health.MaxHP, pc.Health.Percentage);
				}
			}
		}

		private void DrawHealthBar(Graphics _graphics, PluginConfiguration config, ref float xOffset, ref float yOffset, string name, float chealth, float mhealth, float percentage = 1f)
		{
			// Debugger.Break();
			string perc = float.IsNaN(percentage) ? "0%" : string.Format("{0:P1}", percentage);
			Point gfxSize = GetStringSize(_graphics, fonts[config.StringFontName], perc, config.FontSize);
			float widthBar = GetHPBarSize(config);
			float heightBar = (gfxSize.Y / 4) + gfxSize.Y;
			var colors = GetColor(name);
			float endOfBar = config.PositionX + widthBar - gfxSize.X - 8f;
			_graphics.DrawRectangle(brushes["lightgrey"], xOffset, yOffset += heightBar, xOffset + widthBar, yOffset + heightBar, 4f);
			_graphics.FillRectangle(brushes["darkgrey"], xOffset + 1f, yOffset + 1f, xOffset + (widthBar - 2f), yOffset + (heightBar - 2f));
			_graphics.FillRectangle(colors[0], xOffset + 1f, yOffset + 1f, xOffset + ((widthBar - 2f) * percentage), yOffset + (heightBar - 2f));
			_graphics.DrawText(fonts[config.StringFontName + " Bold"], config.FontSize, colors[1], xOffset + 10f, yOffset += 1, string.Format("{0}{1} / {2}", name.Replace("_", ""), chealth, mhealth));
			_graphics.DrawText(fonts[config.StringFontName + " Bold"], config.FontSize, colors[1], endOfBar, yOffset, perc);
		}

		private void DrawPlayerBar(Graphics _graphics, OverlayWindow _window, PluginConfiguration config, string name, float chealth, float mhealth, float percentage = 1f)
		{
			float widthBar = GetHPBarSize(config);
			float heightBar = config.FontSize + 8f;
			var xOffset = ((_window.Width / 2f) - (widthBar / 2f)) * config.ScalingFactor;
			var yOffset = (_window.Height - (heightBar * 4f)) * config.ScalingFactor;
			// var yOffset = ((_window.Height / 2f) - (heightBar / 2f)) * config.ScalingFactor;
			string perc = float.IsNaN(percentage) ? "0%" : string.Format("{0:P1}", percentage);
			Point gfxSize = GetStringSize(_graphics, fonts[config.StringFontName], perc, config.FontSize);
			float endOfBar = (_window.Width / 2f) - (widthBar / 2f) + widthBar - gfxSize.X - 8f;
			_graphics.DrawRectangle(brushes["lightgrey"], xOffset, yOffset += heightBar + 2, xOffset + widthBar, yOffset + heightBar, 4f);
			_graphics.FillRectangle(brushes["darkgrey"], xOffset + 1f, yOffset + 1f, xOffset + (widthBar - 2f), yOffset + (heightBar - 2f));
			_graphics.FillRectangle(PlayerHPColors[0], xOffset + 1f, yOffset + 1f, xOffset + ((widthBar - 2f) * percentage), yOffset + (heightBar - 2f));
			_graphics.DrawText(fonts[config.StringFontName + " Bold"], config.FontSize, PlayerHPColors[1], xOffset + 10f, yOffset, string.Format("{0}{1} / {2}", name.Replace("_", ""), chealth, mhealth));
			_graphics.DrawText(fonts[config.StringFontName + " Bold"], config.FontSize, PlayerHPColors[1], endOfBar, yOffset, perc);
		}

		private void DrawPartnerBar(Graphics _graphics, OverlayWindow _window, PluginConfiguration config, string name, float chealth, float mhealth, float percentage = 1f)
		{
			float widthBar = GetHPBarSize(config);
			float heightBar = config.FontSize + 8f;
			var xOffset = ((_window.Width / 2f) - (widthBar / 2f)) * config.ScalingFactor;
			var yOffset = (_window.Height - (heightBar * 3f)) + 2f * config.ScalingFactor;
			var colors = GetColor(name);
			string perc = float.IsNaN(percentage) ? "0%" : string.Format("{0:P1}", percentage);
			Point gfxSize = GetStringSize(_graphics, fonts[config.StringFontName], perc, config.FontSize);
			float endOfBar = (_window.Width / 2f) - (widthBar / 2f) + widthBar - gfxSize.X - 8f;
			_graphics.DrawRectangle(brushes["lightgrey"], xOffset, yOffset += heightBar + 2, xOffset + widthBar, yOffset + heightBar, 4f);
			_graphics.FillRectangle(brushes["darkgrey"], xOffset + 1f, yOffset + 1f, xOffset + (widthBar - 2f), yOffset + (heightBar - 2f));
			_graphics.FillRectangle(colors[0], xOffset + 1f, yOffset + 1f, xOffset + ((widthBar - 2f) * percentage), yOffset + (heightBar - 2f));
			_graphics.DrawText(fonts[config.StringFontName + " Bold"], config.FontSize, colors[1], xOffset + 8f, yOffset, string.Format("{0}{1} / {2}", name.Replace("_", ""), chealth, mhealth));
			_graphics.DrawText(fonts[config.StringFontName + " Bold"], config.FontSize, colors[1], endOfBar, yOffset, perc);
		}

		// ENEMY HP METHODS
		private void DrawProgressBar(Graphics _graphics, PluginConfiguration config, ref float xOffset, ref float yOffset, string name, float chealth, float mhealth, float percentage = 1f)
		{
			if (name == "Dog") return;
			if (config.ShowDamagedEnemiesOnly && percentage == 1f) return;

			string perc = float.IsNaN(percentage) ? "0%" : string.Format("{0:P1}", percentage);
			Point gfxSize = GetStringSize(_graphics, fonts[config.StringFontName], perc, config.FontSize);
			float widthBar = GetHPBarSize2(config);
			float heightBar = gfxSize.Y + 4f;
			float endOfBar = config.PositionX + widthBar - gfxSize.X - 8f;

			_graphics.DrawRectangle(brushes["lightgrey"], xOffset, yOffset += heightBar, xOffset + widthBar, yOffset + heightBar, 4f);
			_graphics.FillRectangle(brushes["darkgrey"], xOffset + 1f, yOffset + 1f, xOffset + widthBar - 2f, yOffset + (heightBar - 2f));
			_graphics.FillRectangle(brushes["darkred"], xOffset + 1f, yOffset + 1f, xOffset + ((widthBar - 2f) * percentage), yOffset + (heightBar - 2f));
			_graphics.DrawText(fonts[config.StringFontName + " Bold"], config.FontSize, brushes["lightred"], xOffset + 10f, yOffset += 1, string.Format("{0} / {1}", chealth, mhealth));
			_graphics.DrawText(fonts[config.StringFontName + " Bold"], config.FontSize, brushes["lightred"], endOfBar, yOffset, perc);
		}

		private void DrawBossBar(Graphics _graphics, OverlayWindow _window, PluginConfiguration config, string name, float chealth, float mhealth, float percentage = 1f)
		{
			float fSize = 24f;
			float widthBar = GetBossBarSize(fSize);
			float heightBar = (fSize / 2f) + fSize;
			var xOffset = ((_window.Width / 2f) - (widthBar / 2f)) * config.ScalingFactor;
			var yOffset = 4f * config.ScalingFactor;
			if (name == "Dog") return;
			if (config.ShowDamagedEnemiesOnly && percentage == 1f) return;
			string perc = float.IsNaN(percentage) ? "0%" : string.Format("{0:P1}", percentage);
			Point gfxSize = GetStringSize(_graphics, fonts[config.StringFontName + " Bold"], perc, fSize);
			float endOfBar = (_window.Width / 2f) - (widthBar / 2f) + widthBar - gfxSize.X - 8f;
			_graphics.DrawRectangle(brushes["lightgrey"], xOffset, yOffset += 28f, xOffset + widthBar, yOffset + heightBar, 4f);
			_graphics.FillRectangle(brushes["darkgrey"], xOffset + 1f, yOffset + 1f, xOffset + widthBar - 2f, yOffset + (heightBar - 2f));
			_graphics.FillRectangle(brushes["darkred"], xOffset + 1f, yOffset + 1f, xOffset + ((widthBar - 2f) * percentage), yOffset + heightBar - 2f);
			_graphics.DrawText(fonts[config.StringFontName + " Bold"], fSize, brushes["lightred"], xOffset + 8f, yOffset, string.Format("{0} {1} / {2}", name.Replace("_", " "), chealth, mhealth));
			_graphics.DrawText(fonts[config.StringFontName + " Bold"], fSize, brushes["lightred"], endOfBar, yOffset, perc);
		}

		public void DrawEnemies(Graphics graphics, OverlayWindow window, PluginConfiguration config, PlayerContext enemy, ref float xOffset, ref float yOffset)
		{
			if (config.ShowBossOnly)
			{
				if (!enemy.IsBoss) return;
				if (enemy.IsBoss)
				{
					if (config.CenterBossHP) DrawBossBar(graphics, window, config, enemy.SurvivorTypeString.ToUpper(), enemy.Health.CurrentHP, enemy.Health.MaxHP, enemy.Health.Percentage);
					else DrawProgressBar(graphics, config, ref xOffset, ref yOffset, enemy.SurvivorTypeString.ToUpper(), enemy.Health.CurrentHP, enemy.Health.MaxHP, enemy.Health.Percentage);
				}
			}
			else if (config.CenterBossHP && enemy.IsBoss)
				DrawBossBar(graphics, window, config, enemy.SurvivorTypeString.ToUpper(), enemy.Health.CurrentHP, enemy.Health.MaxHP, enemy.Health.Percentage);
			else
				DrawProgressBar(graphics, config, ref xOffset, ref yOffset, enemy.SurvivorTypeString.ToUpper(), enemy.Health.CurrentHP, enemy.Health.MaxHP, enemy.Health.Percentage);
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
            foreach (var pair in brushes) pair.Value.Dispose();
            foreach (var pair in fonts) pair.Value.Dispose();
            return;
		}

		public async ValueTask DisposeAsync()
		{
			Dispose();
			await Task.CompletedTask;
		}
	}
}
