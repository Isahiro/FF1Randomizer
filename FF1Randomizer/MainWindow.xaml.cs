﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using RomUtilities;

namespace FF1Randomizer
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		private string _filename;
		private Blob _seed;

		public const string Version = "0.6.3";

		private class MainWindowViewModel
		{
			public string WindowTitle => $"FF1 Randomizer {Version}";
		}

		public MainWindow()
		{
			InitializeComponent();

			DataContext = new MainWindowViewModel();

			GenerateSeed();

			SetScaleFactorLabel(PriceScaleFactorSlider, PriceScaleFactorLabel);
			SetScaleFactorLabel(EnemyScaleFactorSlider, EnemyScaleFactorLabel);
			SetExpLabel();
			SetFlagsText(null, null);
		}

		private void GenerateSeed()
		{
			_seed = Blob.Random(4);

			SeedTextBox.Text = _seed.ToHex();
		}

		private void RomButton_Click(object sender, RoutedEventArgs e)
		{
			var openFileDialog = new OpenFileDialog
			{
				Filter = "NES ROM files (*.nes)|*.nes"
			};

			var result = openFileDialog.ShowDialog(this);
			if (result == true)
			{
				var randomizer = new FF1Rom(openFileDialog.FileName);
				if (!randomizer.Validate())
				{
					MessageBox.Show("ROM does not appear to be valid.  Proceed at your own risk.", "Validation Error");
				}

				_filename = openFileDialog.FileName;
				RomTextBox.Text = openFileDialog.SafeFileName;
				GenerateButton.IsEnabled = true;
			}
		}

		private void SeedButton_Click(object sender, RoutedEventArgs e)
		{
			GenerateSeed();
		}

		private void SeedTextBox_LostFocus(object sender, RoutedEventArgs e)
		{
			SeedTextBox.Text = SeedTextBox.Text.Trim();

			try
			{
				_seed = Blob.FromHex(SeedTextBox.Text);
			}
			catch (Exception)
			{
				MessageBox.Show("Seeds must be eight hexadecimal characters (0-9, A-F).  Generating new seed.", "Invalid Seed");

				GenerateSeed();
			}
		}

		private void GenerateButton_Click(object sender, RoutedEventArgs e)
		{
			var rom = new FF1Rom(_filename);
			var rng = new MT19337(BitConverter.ToUInt32(_seed, 0));

			if (TreasuresCheckBox.IsChecked == true)
			{
				rom.ShuffleTreasures(rng);
			}

			if (ShopsCheckBox.IsChecked == true)
			{
				rom.ShuffleShops(rng);
			}

			if (MagicShopsCheckBox.IsChecked == true)
			{
				rom.ShuffleMagicShops(rng);
			}

			if (MagicLevelsCheckBox.IsChecked == true)
			{
				rom.ShuffleMagicLevels(rng, MagicPermissionsCheckBox.IsChecked ?? false);
			}

			if (PricesCheckBox.IsChecked == true)
			{
				rom.ScalePrices(PriceScaleFactorSlider.Value, rng);
			}

			if (EnemyStatsCheckBox.IsChecked == true)
			{
				rom.ScaleEnemyStats(EnemyScaleFactorSlider.Value, rng);
			}

			if (ExpGoldBoostCheckBox.IsChecked == true)
			{
				rom.ExpGoldBoost(ExpBonusSlider.Value*10, ExpMultiplierSlider.Value);
			}

			var seedText = _seed.ToHex();
			rom.WriteSeedAndFlags(Version, seedText, FlagsTextBox.Text);

			var fileRoot = _filename.Substring(0, _filename.LastIndexOf("."));
			var outputFilename = $"{fileRoot}_{FlagsTextBox.Text}_{seedText}.nes";
			rom.Save(outputFilename);

			MessageBox.Show($"Finished generating new ROM: {outputFilename}", "Done");
		}

		private void MagicLevelsCheckBox_OnChecked(object sender, RoutedEventArgs e)
		{
			if (MagicPermissionsCheckBox != null)
			{
				MagicPermissionsCheckBox.IsEnabled = true;
			}

			SetFlagsText(sender, e);
		}

		private void MagicLevelsCheckBox_OnUnchecked(object sender, RoutedEventArgs e)
		{
			if (MagicPermissionsCheckBox != null)
			{
				MagicPermissionsCheckBox.IsEnabled = false;
			}

			SetFlagsText(sender, e);
		}

		private void PriceScaleFactorSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			PriceScaleFactorSlider.Value = Math.Round(PriceScaleFactorSlider.Value, 1);

			SetScaleFactorLabel(PriceScaleFactorSlider, PriceScaleFactorLabel);
			SetFlagsText(sender, e);
		}

		private void EnemyScaleFactorSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			EnemyScaleFactorSlider.Value = Math.Round(EnemyScaleFactorSlider.Value, 1);

			SetScaleFactorLabel(EnemyScaleFactorSlider, EnemyScaleFactorLabel);
			SetFlagsText(sender, e);
		}

		private void SetScaleFactorLabel(Slider slider, Label label)
		{
			var lower = Math.Round(100 / slider.Value);
			var upper = Math.Round(100 * slider.Value);

			label.Content = $"{lower}% - {upper}%";
		}

		private void ExpMultiplierSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			ExpMultiplierSlider.Value = Math.Round(ExpMultiplierSlider.Value, 1);

			SetExpLabel();
			SetFlagsText(sender, e);
		}

		private void ExpBonusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			ExpBonusSlider.Value = Math.Round(ExpBonusSlider.Value);

			SetExpLabel();
			SetFlagsText(sender, e);
		}

		private void SetExpLabel()
		{
			if (ExpMultiplierSlider != null && ExpBonusSlider != null && ExpLabel != null)
			{
				ExpLabel.Content = $"{ExpMultiplierSlider.Value}x + {ExpBonusSlider.Value*10}";
			}
		}

		private void SetFlagsText(object sender, RoutedEventArgs e)
		{
			if (FlagsTextBox == null)
			{
				return;
			}

			FlagsTextBox.Text = "";

			FlagsTextBox.Text += TreasuresCheckBox.IsChecked == true ? "T" : "t";
			FlagsTextBox.Text += ShopsCheckBox.IsChecked == true ? "S" : "s";
			FlagsTextBox.Text += MagicShopsCheckBox.IsChecked == true ? "M" : "m";
			FlagsTextBox.Text += MagicLevelsCheckBox.IsChecked == true ? "L" : "l";
			FlagsTextBox.Text += MagicPermissionsCheckBox.IsChecked == true ? "P" : "p";

			if (PricesCheckBox.IsChecked == true)
			{
				FlagsTextBox.Text += "P";
				FlagsTextBox.Text += SliderToBase64((int)(10 * PriceScaleFactorSlider.Value));
			}
			if (EnemyStatsCheckBox.IsChecked == true)
			{
				FlagsTextBox.Text += "S";
				FlagsTextBox.Text += SliderToBase64((int)(10 * EnemyScaleFactorSlider.Value));
			}

			if (ExpGoldBoostCheckBox.IsChecked == true)
			{
				FlagsTextBox.Text += "B";
				FlagsTextBox.Text += SliderToBase64((int)(10*ExpMultiplierSlider.Value));
				FlagsTextBox.Text += SliderToBase64((int)ExpBonusSlider.Value);
			}
		}

		private char SliderToBase64(int value)
		{
			if (value < 0 || value > 63)
			{
				throw new ArgumentOutOfRangeException(nameof(value), value, "Value must be between 0 and 63.");
			}
			else if (value < 10)
			{
				return (char)('0' + value);
			}
			else if (value < 36)
			{
				return (char)('A' + value - 10);
			}
			else if (value < 62)
			{
				return (char)('a' + value - 36);
			}
			else if (value == 62)
			{
				return '!';
			}
			else
			{
				return '?';
			}
		}
	}
}
