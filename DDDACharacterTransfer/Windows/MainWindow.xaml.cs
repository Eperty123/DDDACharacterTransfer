/*
/* MIT License
/* 
/* Copyright (c) 2018-2022 Eperty123
/* 
/* Permission is hereby granted, free of charge, to any person obtaining a copy
/* of this software and associated documentation files (the "Software"), to deal
/* in the Software without restriction, including without limitation the rights
/* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
/* copies of the Software, and to permit persons to whom the Software is
/* furnished to do so, subject to the following conditions:
/* 
/* The above copyright notice and this permission notice shall be included in all
/* copies or substantial portions of the Software.
/* 
/* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
/* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
/* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
/* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
/* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
/* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
/* SOFTWARE.
*/

using DDDSaveToolSharp.Core.Models;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;

namespace DDDACharacterTransfer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Sav _Sav;
        Preset _Preset;
        PresetManager _PresetManager;

        PlayerType _SelectedInputPlayer;
        PlayerType _SelectedTargetPlayer;
        ReplacementType _SelectedReplacementType;

        public MainWindow()
        {
            InitializeComponent();
            Initialize();
        }

        void Initialize()
        {
            Title = "DDDA Character Transfer";

            _Sav = new Sav();
            _Preset = new Preset();
            _PresetManager = new PresetManager();

            AddPlayerTypes();
            AddReplacementTypes();

            SetTextBlockValue(YearTextBlock, $"2018 - {DateTime.Now.Year}");
        }

        void AddPlayerTypes()
        {
            var playerTypes = Enum.GetValues(typeof(PlayerType));
            foreach (PlayerType playerType in playerTypes)
            {
                AddComboBoxItem(InputPlayerComboBox, playerType);
                AddComboBoxItem(TargetPlayerComboBox, playerType);
            }
            SetComboBoxItemIndex(InputPlayerComboBox, 0);
            SetComboBoxItemIndex(TargetPlayerComboBox, 0);
        }

        void AddReplacementTypes()
        {
            var replacementTypes = Enum.GetValues(typeof(ReplacementType));
            foreach (ReplacementType replacementType in replacementTypes)
                AddComboBoxItem(ReplacementTypeCombBox, replacementType);

            SetComboBoxItemIndex(ReplacementTypeCombBox, 0);
        }

        bool HasSaveFileLoaded()
        {
            return _Sav != null && _Sav.IsLoaded();
        }

        void NoSaveFileLoadedMessage()
        {
            MessageBox.Show($"Please load up a save file first.", "No save file selected!", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        void LoadSavFile()
        {
            var opf = CreateOpenFileDialog("Select a save file", "Sav|*.sav");
            if (opf.ShowDialog() == true)
            {
                bool ok = _Sav.Load(opf.FileName);
                if (ok)
                {
                    _PresetManager.SetSav(_Sav);
                    SetTextBoxValue(InputSavFileTxtBox, opf.FileName);

                    SetControlEnabledState(ProfileSettingsTab, true);
                    SetControlEnabledState(CharacterTransferTab, true);

                    ReadSavProfile();
                }
                else NoSaveFileLoadedMessage();
            }
        }


        void ExportSavAsXml()
        {
            var sfd = CreateSaveFileDialog("Export save file as xml", "Xml|*.xml");
            if (HasSaveFileLoaded())
            {
                if (sfd.ShowDialog() == true)
                {
                    bool ok = _Sav.ExportXml(sfd.FileName);
                    if (!ok) MessageBox.Show($"Failed to export save file: {sfd.FileName} as xml! Try again.", "Failed to export save file as xml!", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else NoSaveFileLoadedMessage();
        }

        void SaveSavFile()
        {
            var sfd = CreateSaveFileDialog("Save save file", "Sav|*.sav");
            if (HasSaveFileLoaded())
            {
                if (sfd.ShowDialog() == true)
                {
                    bool ok = _Sav.Save(sfd.FileName);
                    if (!ok) MessageBox.Show($"Failed to save save file: {sfd.FileName}! Try again.", "Failed to save save file!", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else NoSaveFileLoadedMessage();
        }

        void ReadSavProfile()
        {
            if (HasSaveFileLoaded())
            {
                SetTextBoxValue(SteamIdTxtBox, _Sav.GetSteamId());
                SetTextBoxValue(PlayerNameTxtBox, _Sav.GetPlayerName());
            }
            else NoSaveFileLoadedMessage();
        }

        void ApplyProfileChanges()
        {
            if (HasSaveFileLoaded())
            {
                long.TryParse(SteamIdTxtBox.Text, out var steamId);
                if (!_Sav.SetSteamId(steamId)) MessageBox.Show("Failed to set Steam Id!", "Failed to set Steam Id!", MessageBoxButton.OK, MessageBoxImage.Error);
                if (!_Sav.SetPlayerName(PlayerNameTxtBox.Text)) MessageBox.Show("Failed to set player name!", "Failed to set player name!", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else NoSaveFileLoadedMessage();
        }

        void LoadPreset()
        {
            var opf = CreateOpenFileDialog("Load a character preset", "Xml|*.xml");
            if (HasSaveFileLoaded())
            {
                if (opf.ShowDialog() == true)
                {
                    _Preset.Load(opf.FileName);
                    _PresetManager.SetPreset(_Preset);
                }
            }
            else NoSaveFileLoadedMessage();
        }

        void SavePreset(PlayerType playerType)
        {
            var sfd = CreateSaveFileDialog("Save character preset", "Xml|*.xml");
            if (HasSaveFileLoaded())
            {
                if (sfd.ShowDialog() == true)
                {
                    var generatedPreset = Preset.GenerateFromSavFile(_Sav, playerType);
                    generatedPreset.Save(sfd.FileName);

                    generatedPreset = null;
                }
            }
            else NoSaveFileLoadedMessage();
        }

        void TransferCharacter()
        {
            if (HasSaveFileLoaded())
            {
                if (!_Preset.IsLoaded())
                {
                    // Use the selected character's data if no preset data is present.
                    var generatedPreset = Preset.GenerateFromSavFile(_Sav, _SelectedInputPlayer);
                    _Preset.LoadXml(generatedPreset.OuterXml);

                    generatedPreset = null;
                }

                _PresetManager.ApplyPreset(_SelectedTargetPlayer, _SelectedReplacementType);
            }
            else NoSaveFileLoadedMessage();
        }

        void SetControlEnabledState(Control control, bool state)
        {
            Dispatcher.BeginInvoke(new Action(() => control.IsEnabled = state));
        }

        void SetTextBoxValue(TextBox txtBox, object value)
        {
            Dispatcher.BeginInvoke(new Action(() => txtBox.Text = value.ToString()));
        }

        void SetTextBlockValue(TextBlock textBlock, object value)
        {
            Dispatcher.BeginInvoke(new Action(() => textBlock.Text = value.ToString()));
        }

        void AddComboBoxItem(ComboBox comboBox, object item)
        {
            Dispatcher.BeginInvoke(new Action(() => comboBox.Items.Add(item)));
        }

        void SetComboBoxItem(ComboBox comboBox, object item)
        {
            Dispatcher.BeginInvoke(new Action(() => comboBox.SelectedItem = item));
        }

        void SetComboBoxItemIndex(ComboBox comboBox, int index)
        {
            Dispatcher.BeginInvoke(new Action(() => comboBox.SelectedIndex = index));
        }


        OpenFileDialog CreateOpenFileDialog(string title, string filter)
        {
            var opf = new OpenFileDialog();
            opf.Title = title;
            opf.Filter = filter;

            return opf;
        }

        SaveFileDialog CreateSaveFileDialog(string title, string filter)
        {
            var sfd = new SaveFileDialog();
            sfd.Title = title;
            sfd.Filter = filter;

            return sfd;
        }

        private void LoadSavBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadSavFile();
        }

        private void ExportSavXmlBtn_Click(object sender, RoutedEventArgs e)
        {
            ExportSavAsXml();
        }

        private void SaveSavBtn_Click(object sender, RoutedEventArgs e)
        {
            SaveSavFile();
        }

        private void ApplyProfileChangesBtn_Click(object sender, RoutedEventArgs e)
        {
            ApplyProfileChanges();
        }

        private void InputPlayerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _SelectedInputPlayer = (PlayerType)InputPlayerComboBox.SelectedItem;
        }

        private void TargetPlayerComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _SelectedTargetPlayer = (PlayerType)TargetPlayerComboBox.SelectedItem;
        }

        private void InputPlayerLoadPresetBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadPreset();
        }

        private void InputPlayerSavePresetBtn_Click(object sender, RoutedEventArgs e)
        {
            SavePreset(_SelectedInputPlayer);
        }

        private void TargetPlayerLoadPresetBtn_Click(object sender, RoutedEventArgs e)
        {
            LoadPreset();
        }

        private void TargetPlayerSavePresetBtn_Click(object sender, RoutedEventArgs e)
        {
            SavePreset(_SelectedTargetPlayer);
        }

        private void ApplyTransferChangesBtn_Click(object sender, RoutedEventArgs e)
        {
            TransferCharacter();
        }

        private void ReplacementTypeCombBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _SelectedReplacementType = (ReplacementType)ReplacementTypeCombBox.SelectedItem;
        }

        private void LibraryLink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start("explorer.exe", e.Uri.ToString());
        }
    }
}
