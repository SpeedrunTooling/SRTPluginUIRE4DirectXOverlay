using SRTPluginProducerRE4R;
using SRTPluginProducerRE4R.Structs;
using GameOverlay.Drawing;
using GameOverlay.Windows;
using System.Threading.Tasks;

namespace SRTPluginUIRE4DirectXOverlay.UI
{
	public class UIComponents
	{
		public Font _currentFont;

		public SolidBrush _white;
		public SolidBrush _grey;
		public SolidBrush _darkred;
		public SolidBrush _red;
		public SolidBrush _lightred;
		public SolidBrush _lightyellow;
		public SolidBrush _lightgreen;
		public SolidBrush _lawngreen;
		public SolidBrush _greydark;
		public SolidBrush _greydarker;
		public SolidBrush _darkgreen;
		public SolidBrush _darkyellow;

		public SolidBrush HPBarColor;
		public SolidBrush TextColor;
		public SolidBrush[] HPBarColor2;
		public SolidBrush[] TextColor2;

		public SolidBrush[] PlayerHPColors;
		public SolidBrush[] AshleyHPColors;
		public SolidBrush[] LuisHPColors;

		public UIComponents(Graphics _graphics, PluginConfiguration config)
		{
			HPBarColor2 = new SolidBrush[2];
			TextColor2 = new SolidBrush[2];

			PlayerHPColors = new SolidBrush[2];
			AshleyHPColors = new SolidBrush[2];
			LuisHPColors = new SolidBrush[2];

			_currentFont = _graphics?.CreateFont(config.StringFontName, config.FontSize, true);

			_white = _graphics?.CreateSolidBrush(255, 255, 255);
			_grey = _graphics?.CreateSolidBrush(128, 128, 128);
			_greydark = _graphics?.CreateSolidBrush(64, 64, 64);
			_greydarker = _graphics?.CreateSolidBrush(24, 24, 24);
			_darkred = _graphics?.CreateSolidBrush(153, 0, 0, 100);
			_darkgreen = _graphics?.CreateSolidBrush(0, 102, 0, 100);
			_darkyellow = _graphics?.CreateSolidBrush(218, 165, 32, 100);
			_red = _graphics?.CreateSolidBrush(255, 0, 0);
			_lightred = _graphics?.CreateSolidBrush(255, 172, 172);
			_lightyellow = _graphics?.CreateSolidBrush(255, 255, 150);
			_lightgreen = _graphics?.CreateSolidBrush(150, 255, 150);
			_lawngreen = _graphics?.CreateSolidBrush(124, 252, 0);


			PlayerHPColors[0] = _grey;
			PlayerHPColors[1] = _white;

			AshleyHPColors[0] = _grey;
			AshleyHPColors[1] = _white;

			LuisHPColors[0] = _grey;
			LuisHPColors[1] = _white;
		}

		private void SetColors(PlayerState healthState, SolidBrush[] c)
		{
			if (healthState == PlayerState.Fine) // Fine
			{
				c[0] = _darkgreen;
				c[1] = _lightgreen;
				return;
			}
			else if (healthState == PlayerState.Caution) // Caution (Yellow)
			{
				c[0] = _darkyellow;
				c[1] = _lightyellow;
				return;
			}
			else if (healthState == PlayerState.Danger) // Danger (Red)
			{
				c[0] = _darkred;
				c[1] = _lightred;
				return;
			}
			else
			{
				c[0] = _greydarker;
				c[1] = _white;
				return;
			}
		}

		private float GetStringSize(Graphics _graphics, string str)
		{
			return (float)_graphics?.MeasureString(_currentFont, _currentFont.FontSize, str).X;
		}

		private float GetStringSize(Graphics _graphics, string str, float size = 20f)
		{
			return (float)_graphics?.MeasureString(_currentFont, size, str).X;
		}

		// TEXT BOX METHODS
		public float AlignRight(Graphics _graphics, string s, float x, float width)
		{
			return (x + width) - GetStringSize(_graphics, s);
		}

		public void DrawTextBlock(Graphics _graphics, ref float dx, ref float dy, string label, string val, SolidBrush color)
		{
			_graphics?.DrawText(_currentFont, _currentFont.FontSize, _white, dx, dy += 24f, label);
			var dx2 = dx + GetStringSize(_graphics, label) + 8f;
			_graphics?.DrawText(_currentFont, _currentFont.FontSize, color, dx2, dy, val);
		}

		public void DrawTextBlockRow(Graphics _graphics, ref float dx, ref float dy, string label, string val, SolidBrush color)
		{
			float marginX = 40f;
			_graphics?.DrawText(_currentFont, _currentFont.FontSize, _white, dx, dy, label);
			var dx2 = dx + GetStringSize(_graphics, label) + 8f;
			_graphics?.DrawText(_currentFont, _currentFont.FontSize, color, dx2, dy, val);
			dx += GetStringSize(_graphics, label) + GetStringSize(_graphics, val) + marginX;
		}

		// PLAYER AND PARTNER HP METHODS
		public void DrawPlayerHP(Graphics _graphics, OverlayWindow _window, PluginConfiguration config, PlayerContext pc, string PlayerName, ref float xOffset, ref float yOffset)
		{
			SetColors(pc.HealthState, PlayerHPColors);
			if (config.ShowHPBars)
			{
			    if (pc.IsLoaded)
				{
					if (config.CenterPlayerHP)
					{
						DrawPlayerBar(_graphics, _window, config, PlayerName, pc.Health.CurrentHP, pc.Health.MaxHP, pc.Health.Percentage);
						return;
					}
					DrawHealthBar(_graphics, config, ref xOffset, ref yOffset, PlayerName, pc.Health.CurrentHP, pc.Health.MaxHP, pc.Health.Percentage);
				}
			}
		}

		private SolidBrush[] GetColor(string name)
		{
			if (name.Contains("Leon") || name.Contains("Ashley") && !name.Contains("_"))
				return new SolidBrush[2] { PlayerHPColors[0], PlayerHPColors[1] };
			if (name.Contains("Luis"))
				return new SolidBrush[2] { LuisHPColors[0], LuisHPColors[0] };
			return new SolidBrush[2] { AshleyHPColors[1], AshleyHPColors[1] };
		}

		public void DrawHealthBar(Graphics _graphics, PluginConfiguration config, ref float xOffset, ref float yOffset, string name, float chealth, float mhealth, float percentage = 1f)
		{
			float widthBar = 250f;
			float heightBar = config.FontSize + 8f;
			var colors = GetColor(name);
			string perc = float.IsNaN(percentage) ? "0%" : string.Format("{0:P1}", percentage);
			float endOfBar = config.PositionX + widthBar - GetStringSize(_graphics, perc, config.FontSize);
			_graphics.DrawRectangle(_greydark, xOffset, yOffset += 28f, xOffset + widthBar, yOffset + heightBar, 4f);
			_graphics.FillRectangle(_greydarker, xOffset + 1f, yOffset + 1f, xOffset + (widthBar - 2f), yOffset + (heightBar - 2f));
			_graphics.FillRectangle(colors[0], xOffset + 1f, yOffset + 1f, xOffset + ((widthBar - 2f) * percentage), yOffset + (heightBar - 2f));
			_graphics.DrawText(_currentFont, config.FontSize, colors[1], xOffset + 10f, yOffset, string.Format("{0}{1} / {2}", name.Replace("_", "").ToUpper(), chealth, mhealth));
			_graphics.DrawText(_currentFont, config.FontSize, colors[1], endOfBar, yOffset, perc);
		}

		public void DrawPlayerBar(Graphics _graphics, OverlayWindow _window, PluginConfiguration config, string name, float chealth, float mhealth, float percentage = 1f)
		{
			float widthBar = 250f;
			float heightBar = config.FontSize + 8f;
			var xOffset = ((_window.Width / 2f) - (widthBar / 2f)) * config.ScalingFactor;
			var yOffset = (_window.Height - 100f) * config.ScalingFactor;
			// var yOffset = ((_window.Height / 2f) - (heightBar / 2f)) * config.ScalingFactor;
			string perc = float.IsNaN(percentage) ? "0%" : string.Format("{0:P1}", percentage);
			float endOfBar = (_window.Width / 2f) - (widthBar / 2f) + widthBar - GetStringSize(_graphics, perc, config.FontSize) - 8f;
			_graphics.DrawRectangle(_greydark, xOffset, yOffset += 28f, xOffset + widthBar, yOffset + heightBar, 4f);
			_graphics.FillRectangle(_greydarker, xOffset + 1f, yOffset + 1f, xOffset + (widthBar - 2f), yOffset + (heightBar - 2f));
			_graphics.FillRectangle(PlayerHPColors[0], xOffset + 1f, yOffset + 1f, xOffset + ((widthBar - 2f) * percentage), yOffset + (heightBar - 2f));
			_graphics.DrawText(_currentFont, config.FontSize, PlayerHPColors[1], xOffset + 10f, yOffset, string.Format("{0}{1} / {2}", name.Replace("_", "").ToUpper(), chealth, mhealth));
			_graphics.DrawText(_currentFont, config.FontSize, PlayerHPColors[1], endOfBar, yOffset, perc);
		}

		public void DrawPartnerBar(Graphics _graphics, OverlayWindow _window, PluginConfiguration config, string name, float chealth, float mhealth, float percentage = 1f)
		{
			float widthBar = 250f;
			float heightBar = config.FontSize + 8f;
			var xOffset = ((_window.Width / 2f) - (widthBar / 2f)) * config.ScalingFactor;
			var yOffset = (_window.Height - 70f) * config.ScalingFactor;
			var bar = name.Contains("Leon") || name.Contains("Ashley") && !name.Contains("_") ? HPBarColor : name.Contains("_") || name.Contains("Luis") ? HPBarColor2[0] : HPBarColor2[1];
			var txt = name.Contains("Leon") || name.Contains("Ashley") && !name.Contains("_") ? TextColor : name.Contains("_") || name.Contains("Luis") ? TextColor2[0] : TextColor2[1];
			string perc = float.IsNaN(percentage) ? "0%" : string.Format("{0:P1}", percentage);
			float endOfBar = (_window.Width / 2f) - (widthBar / 2f) + widthBar - GetStringSize(_graphics, perc, config.FontSize) - 8f;
			_graphics.DrawRectangle(_greydark, xOffset, yOffset += 28f, xOffset + widthBar, yOffset + heightBar, 4f);
			_graphics.FillRectangle(_greydarker, xOffset + 1f, yOffset + 1f, xOffset + (widthBar - 2f), yOffset + (heightBar - 2f));
			_graphics.FillRectangle(bar, xOffset + 1f, yOffset + 1f, xOffset + ((widthBar - 2f) * percentage), yOffset + (heightBar - 2f));
			_graphics.DrawText(_currentFont, config.FontSize, txt, xOffset + 10f, yOffset, string.Format("{0}{1} / {2}", name.Replace("_", "").ToUpper(), chealth, mhealth));
			_graphics.DrawText(_currentFont, config.FontSize, txt, endOfBar, yOffset, perc);
		}

		// ENEMY HP METHODS
		private void DrawProgressBar(Graphics _graphics, PluginConfiguration config, ref float xOffset, ref float yOffset, string name, float chealth, float mhealth, float percentage = 1f)
		{
			float widthBar = 200f;
			if (name == "Dog") return;
			if (config.ShowDamagedEnemiesOnly && percentage == 1f) return;
			string perc = float.IsNaN(percentage) ? "0%" : string.Format("{0:P1}", percentage);
			float endOfBar = config.PositionX + widthBar - GetStringSize(_graphics, perc, config.FontSize);
			_graphics.DrawRectangle(_greydark, xOffset, yOffset += 28f, xOffset + widthBar, yOffset + 22f, 4f);
			_graphics.FillRectangle(_greydarker, xOffset + 1f, yOffset + 1f, xOffset + widthBar - 2f, yOffset + 20f);
			_graphics.FillRectangle(_darkred, xOffset + 1f, yOffset + 1f, xOffset + ((widthBar - 2f) * percentage), yOffset + 20f);
			_graphics.DrawText(_currentFont, config.FontSize, _lightred, xOffset + 10f, yOffset, string.Format("{0} / {1}", chealth, mhealth));
			_graphics.DrawText(_currentFont, config.FontSize, _lightred, endOfBar, yOffset, perc);
		}

		private void DrawBossBar(Graphics _graphics, OverlayWindow _window, PluginConfiguration config, string name, float chealth, float mhealth, float percentage = 1f)
		{
			float widthBar = 552f;
			float heightBar = 32f;
			float fSize = 24f;
			var xOffset = ((_window.Width / 2f) - (widthBar / 2f)) * config.ScalingFactor;
			var yOffset = 4f * config.ScalingFactor;
			if (name == "Dog") return;
			if (config.ShowDamagedEnemiesOnly && percentage == 1f) return;
			string perc = float.IsNaN(percentage) ? "0%" : string.Format("{0:P1}", percentage);
			float endOfBar = (_window.Width / 2f) - (widthBar / 2f) + widthBar - GetStringSize(_graphics, perc, fSize) - 8f;
			_graphics.DrawRectangle(_greydark, xOffset, yOffset += 28f, xOffset + widthBar, yOffset + heightBar, 4f);
			_graphics.FillRectangle(_greydarker, xOffset + 1f, yOffset + 1f, xOffset + widthBar - 2f, yOffset + (heightBar - 2f));
			_graphics.FillRectangle(_darkred, xOffset + 1f, yOffset + 1f, xOffset + ((widthBar - 2f) * percentage), yOffset + heightBar - 2f);
			_graphics.DrawText(_currentFont, fSize, _lightred, xOffset + 10f, yOffset, string.Format("{0} {1} / {2}", name.Replace("_", " ").ToUpper(), chealth, mhealth));
			_graphics.DrawText(_currentFont, fSize, _lightred, endOfBar, yOffset, perc);
		}

		public void DrawEnemies(Graphics graphics, OverlayWindow window, PluginConfiguration config, PlayerContext enemy, ref float xOffset, ref float yOffset)
		{
			if (config.ShowBossOnly)
			{
				if (!enemy.IsBoss) return;
				if (enemy.IsBoss)
				{
					if (config.CenterBossHP) DrawBossBar(graphics, window, config, enemy.SurvivorTypeString, enemy.Health.CurrentHP, enemy.Health.MaxHP, enemy.Health.Percentage);
					else DrawProgressBar(graphics, config, ref xOffset, ref yOffset, enemy.SurvivorTypeString, enemy.Health.CurrentHP, enemy.Health.MaxHP, enemy.Health.Percentage);
				}
			}
			else if (config.CenterBossHP && enemy.IsBoss)
				DrawBossBar(graphics, window, config, enemy.SurvivorTypeString, enemy.Health.CurrentHP, enemy.Health.MaxHP, enemy.Health.Percentage);
			else
				DrawProgressBar(graphics, config, ref xOffset, ref yOffset, enemy.SurvivorTypeString, enemy.Health.CurrentHP, enemy.Health.MaxHP, enemy.Health.Percentage);
		}

		public void Dispose()
		{
			_white?.Dispose();
			_grey?.Dispose();
			_darkred?.Dispose();
			_red?.Dispose();
			_lightred?.Dispose();
			_lightyellow?.Dispose();
			_lightgreen?.Dispose();
			_lawngreen?.Dispose();
			_greydark?.Dispose();
			_greydarker?.Dispose();
			_darkgreen?.Dispose();
			_darkyellow?.Dispose();
			HPBarColor?.Dispose();
			TextColor?.Dispose();
			HPBarColor2[0]?.Dispose();
			HPBarColor2[1]?.Dispose();
			TextColor2[0]?.Dispose();
			TextColor2[1]?.Dispose();
			_currentFont?.Dispose();
			return;
		}

		public async ValueTask DisposeAsync()
		{
			Dispose();
			await Task.CompletedTask;
		}
	}
}
