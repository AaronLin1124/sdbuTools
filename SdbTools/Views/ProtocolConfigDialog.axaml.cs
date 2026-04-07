using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using SdbTools.Models;

namespace SdbTools.Views;

public partial class ProtocolConfigDialog : Window
{
    private const int Initial_fieldCount = 1;
    private int _fieldCount = Initial_fieldCount;
    private List<ComboBox> _typeCombos = new();
    private List<TextBox> _lengthBoxes = new();
    private List<CheckBox> _checkSumChecks = new();
    private List<CheckBox> _inLengthChecks = new();
    private List<CheckBox> _byteOrderChecks = new();

    public SdbuProject? Project { get; set; }

    private TextBox? _headerMagicBox;
    private TextBox? _footerMagicBox;

    public Dictionary<int, (int fieldType, int length, bool checkSum, bool inLength, bool byteOrder)> FieldConfigs { get; private set; } = new();

    public ProtocolConfigDialog()
    {
        InitializeComponent();
        GenerateFieldConfigs();
        _headerMagicBox = this.Find<TextBox>("HeaderMagicBox");
        _footerMagicBox = this.Find<TextBox>("FooterMagicBox");
    }

    public ProtocolConfigDialog(SdbuProject project) : this()
    {
        Project = project;
        LoadFromProject();
    }

    private void LoadFromProject()
    {
        if (Project?.Protocol?.ProtocolConfig == null) return;
        
        var config = Project.Protocol.ProtocolConfig;
        
        for (int i = 0; i < _fieldCount; i++)
        {
            int pos = i * 2;
            if (pos >= config.Length) continue;
            
            byte b = config[pos];
            int fieldType = (b >> 4) & 0x0F;
            int length = (b & 0x0F);
            if (pos + 1 < config.Length)
            {
                length |= (config[pos + 1] << 4);
            }
            
            bool checkSum = (b & 0x20) != 0;
            bool inLength = (b & 0x40) != 0;
            bool byteOrder = (b & 0x80) != 0;

            if (fieldType > 0)
            {
                _typeCombos[i].SelectedIndex = fieldType;
                _lengthBoxes[i].Text = length.ToString();
                _checkSumChecks[i].IsChecked = checkSum;
                _inLengthChecks[i].IsChecked = inLength;
                _byteOrderChecks[i].IsChecked = byteOrder;
            }
        }

        if (Project.Protocol.HeaderMagic != null)
        {
            _headerMagicBox!.Text = BytesToHexString(Project.Protocol.HeaderMagic);
        }
        if (Project.Protocol.FooterMagic != null)
        {
            _footerMagicBox!.Text = BytesToHexString(Project.Protocol.FooterMagic);
        }
    }

    private string BytesToHexString(byte[] bytes)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < bytes.Length; i++)
        {
            if (i > 0) sb.Append(" ");
            sb.Append(bytes[i].ToString("X2"));
        }
        return sb.ToString();
    }

    private void GenerateFieldConfigs()
    {
        var fieldTypes = new[]
        {
            (0, "无"),
            (1, "包头"),
            (2, "命令"),
            (3, "子命令"),
            (4, "版本"),
            (5, "设备地址"),
            (6, "长度"),
            (7, "数据区"),
            (8, "校验"),
            (9, "包尾")
        };

        for (int i = 0; i < Initial_fieldCount; i++)
        {
            var panel = new StackPanel 
            { 
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Margin = new Avalonia.Thickness(0, 2, 0, 2)
            };

            var indexLabel = new TextBlock 
            { 
                Text = $"{i + 1}.", 
                Width = 30,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
            };

            var combo = new ComboBox { Width = 120, Tag = i, Margin = new Avalonia.Thickness(0, 0, 5, 0) };
            foreach (var (tag, name) in fieldTypes)
            {
                combo.Items.Add(new ComboBoxItem { Content = name, Tag = tag });
            }
            combo.SelectedIndex = 0;
            combo.SelectionChanged += OnFieldTypeChanged;
            _typeCombos.Add(combo);

            var lengthBox = new TextBox { Width = 40, Tag = i, Text = "0", Margin = new Avalonia.Thickness(0, 0, 10, 0) };
            _lengthBoxes.Add(lengthBox);

            var checkSumCheck = new CheckBox { Content = "校验", Tag = i, Margin = new Avalonia.Thickness(0, 0, 5, 0) };
            _checkSumChecks.Add(checkSumCheck);

            var inLengthCheck = new CheckBox { Content = "含长", Tag = i, Margin = new Avalonia.Thickness(0, 0, 5, 0) };
            _inLengthChecks.Add(inLengthCheck);

            var byteOrderCheck = new CheckBox { Content = "Intel" };
            _byteOrderChecks.Add(byteOrderCheck);

            panel.Children.Add(indexLabel);
            panel.Children.Add(combo);
            panel.Children.Add(lengthBox);
            panel.Children.Add(checkSumCheck);
            panel.Children.Add(inLengthCheck);
            panel.Children.Add(byteOrderCheck);

            FieldConfigPanel.Children.Add(panel);
        }
    }

    private void OnFieldTypeChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdateCheckBoxes();
    }

    private void UpdateCheckBoxes()
    {
        int checkSumPos = -1;
        int lengthPos = -1;
        int headerPos = -1;
        int footerPos = -1;

        for (int i = 0; i < _fieldCount; i++)
        {
            var combo = _typeCombos[i];
            var selectedItem = (ComboBoxItem?)combo.SelectedItem;
            int fieldType = selectedItem?.Tag != null ? (int)selectedItem.Tag : 0;

            if (fieldType == 8) checkSumPos = i;
            if (fieldType == 6) lengthPos = i;
            if (fieldType == 1) headerPos = i;
            if (fieldType == 9) footerPos = i;
        }

        for (int i = 0; i < _fieldCount; i++)
        {
            _checkSumChecks[i].IsEnabled = (checkSumPos < 0) || (i < checkSumPos);
            _inLengthChecks[i].IsEnabled = (lengthPos < 0) || (i > lengthPos);
            
            int fieldType = 0;
            var combo = _typeCombos[i];
            var selectedItem = (ComboBoxItem?)combo.SelectedItem;
            if (selectedItem?.Tag != null) fieldType = (int)selectedItem.Tag;
            
            _lengthBoxes[i].LostFocus -= OnLengthLostFocus;
            if (fieldType == 1 || fieldType == 9 || fieldType == 8)
            {
                _lengthBoxes[i].Watermark = "最大4";
            }
            else
            {
                _lengthBoxes[i].Watermark = "";
            }
            _lengthBoxes[i].LostFocus += OnLengthLostFocus;
        }
    }

    private void OnLengthLostFocus(object? sender, RoutedEventArgs e)
    {
        var textBox = sender as TextBox;
        if (textBox == null) return;

        int i = (int)textBox.Tag!;
        int fieldType = 0;
        var combo = _typeCombos[i];
        var selectedItem = (ComboBoxItem?)combo.SelectedItem;
        if (selectedItem?.Tag != null) fieldType = (int)selectedItem.Tag;

        if (fieldType == 1 || fieldType == 9 || fieldType == 8)
        {
            if (int.TryParse(textBox.Text, out var val) && val > 4)
            {
                textBox.Text = "4";
            }
        }
    }

    private void OnConfirm(object? sender, RoutedEventArgs e)
    {
        FieldConfigs.Clear();
        var protocolConfig = new byte[32];
        
        for (int i = 0; i < _fieldCount; i++)
        {
            var combo = _typeCombos[i];
            var lengthBox = _lengthBoxes[i];
            var checkSumCheck = _checkSumChecks[i];
            var inLengthCheck = _inLengthChecks[i];
            var byteOrderCheck = _byteOrderChecks[i];
            
            var selectedItem = (ComboBoxItem?)combo.SelectedItem;
            int fieldType = selectedItem?.Tag != null ? (int)selectedItem.Tag : 0;
            int length = int.TryParse(lengthBox.Text, out var l) ? l : 0;
            bool checkSum = checkSumCheck.IsChecked == true;
            bool inLength = inLengthCheck.IsChecked == true;
            bool byteOrder = byteOrderCheck.IsChecked == true;

            int pos = i * 2;
            byte b = (byte)((fieldType & 0x0F) << 4);
            b |= (byte)(length & 0x0F);
            if (checkSum) b |= 0x20;
            if (inLength) b |= 0x40;
            if (byteOrder) b |= 0x80;
            protocolConfig[pos] = b;
            protocolConfig[pos + 1] = (byte)(length >> 4);

            if (fieldType > 0)
            {
                FieldConfigs[i] = (fieldType, length, checkSum, inLength, byteOrder);
            }
        }

        if (Project != null)
        {
            Project.Protocol.ProtocolConfig = protocolConfig;
            Project.Protocol.HeaderMagic = HexStringToBytes(_headerMagicBox?.Text ?? "");
            Project.Protocol.FooterMagic = HexStringToBytes(_footerMagicBox?.Text ?? "");
            Project.IsDirty = true;
        }
        Close();
    }

    private byte[] HexStringToBytes(string hex)
    {
        var bytes = new List<byte>();
        var parts = hex.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in parts)
        {
            if (part.Length == 2 && byte.TryParse(part, System.Globalization.NumberStyles.HexNumber, null, out var b))
            {
                bytes.Add(b);
            }
        }
        while (bytes.Count < 4) bytes.Add(0);
        return bytes.Take(4).ToArray();
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnAddField(object? sender, RoutedEventArgs e)
    {
        if (_fieldCount >= 16) return;
        
        int i = _fieldCount;
        _fieldCount++;

        ComboBox combo;
        TextBox lengthBox;
        CheckBox checkSumCheck;
        CheckBox inLengthCheck;
        CheckBox byteOrderCheck;

        var panel = new StackPanel 
        { 
            Orientation = Avalonia.Layout.Orientation.Horizontal,
            Margin = new Avalonia.Thickness(0, 2, 0, 2)
        };

        var indexLabel = new TextBlock 
        { 
            Text = $"{i + 1}.", 
            Width = 30,
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center
        };

        combo = new ComboBox { Width = 120, Tag = i, Margin = new Avalonia.Thickness(0, 0, 5, 0) };
        combo.Items.Add(new ComboBoxItem { Content = "无", Tag = 0 });
        combo.Items.Add(new ComboBoxItem { Content = "包头", Tag = 1 });
        combo.Items.Add(new ComboBoxItem { Content = "命令", Tag = 2 });
        combo.Items.Add(new ComboBoxItem { Content = "子命令", Tag = 3 });
        combo.Items.Add(new ComboBoxItem { Content = "版本", Tag = 4 });
        combo.Items.Add(new ComboBoxItem { Content = "设备地址", Tag = 5 });
        combo.Items.Add(new ComboBoxItem { Content = "长度", Tag = 6 });
        combo.Items.Add(new ComboBoxItem { Content = "数据区", Tag = 7 });
        combo.Items.Add(new ComboBoxItem { Content = "校验", Tag = 8 });
        combo.Items.Add(new ComboBoxItem { Content = "包尾", Tag = 9 });
        combo.SelectedIndex = 0;
        combo.SelectionChanged += OnFieldTypeChanged;
        _typeCombos.Add(combo);

        lengthBox = new TextBox { Width = 40, Tag = i, Text = "0", Margin = new Avalonia.Thickness(0, 0, 10, 0) };
        _lengthBoxes.Add(lengthBox);

        checkSumCheck = new CheckBox { Content = "校验", Tag = i, Margin = new Avalonia.Thickness(0, 0, 5, 0) };
        _checkSumChecks.Add(checkSumCheck);

        inLengthCheck = new CheckBox { Content = "含长", Tag = i, Margin = new Avalonia.Thickness(0, 0, 5, 0) };
        _inLengthChecks.Add(inLengthCheck);

        byteOrderCheck = new CheckBox { Content = "Intel" };
        _byteOrderChecks.Add(byteOrderCheck);

        panel.Children.Add(indexLabel);
        panel.Children.Add(combo);
        panel.Children.Add(lengthBox);
        panel.Children.Add(checkSumCheck);
        panel.Children.Add(inLengthCheck);
        panel.Children.Add(byteOrderCheck);

        FieldConfigPanel.Children.Add(panel);
        UpdateCheckBoxes();
    }

    private void OnDeleteField(object? sender, RoutedEventArgs e)
    {
        if (_fieldCount <= 1) return;
        
        _fieldCount--;
        int i = _fieldCount;
        
        if (i >= 0 && i < _typeCombos.Count) _typeCombos.RemoveAt(i);
        if (i >= 0 && i < _lengthBoxes.Count) _lengthBoxes.RemoveAt(i);
        if (i >= 0 && i < _checkSumChecks.Count) _checkSumChecks.RemoveAt(i);
        if (i >= 0 && i < _inLengthChecks.Count) _inLengthChecks.RemoveAt(i);
        if (i >= 0 && i < _byteOrderChecks.Count) _byteOrderChecks.RemoveAt(i);
        
        if (i >= 0 && i < FieldConfigPanel.Children.Count) FieldConfigPanel.Children.RemoveAt(i);
        UpdateCheckBoxes();
    }
}