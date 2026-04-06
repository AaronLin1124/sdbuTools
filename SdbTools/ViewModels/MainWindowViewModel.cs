using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SdbTools.Models;
using SdbTools.Parsers;
using SdbTools.Views;
using SdbTools.Writers;

namespace SdbTools.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private SdbuMessage? _selectedMessage;
    
    [ObservableProperty]
    private SdbuSignal? _selectedSignal;

    public SdbuProject Project { get; private set; } = new();

    public ObservableCollection<SdbuMessage> Messages { get; } = new();
    public ObservableCollection<SdbuSignal> Signals { get; } = new();

    partial void OnSelectedMessageChanged(SdbuMessage? value)
    {
        UpdateSignalList();
        AddSignalCommand.NotifyCanExecuteChanged();
        DeleteMessageCommand.NotifyCanExecuteChanged();
        EditMessageCommand.NotifyCanExecuteChanged();
        SaveMessageChangesCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedSignalChanged(SdbuSignal? value)
    {
        DeleteSignalCommand.NotifyCanExecuteChanged();
        EditSignalCommand.NotifyCanExecuteChanged();
        SaveSignalChangesCommand.NotifyCanExecuteChanged();
    }

    public string ProtocolConfigText => "点击配置协议";

    public int ByteOrderIndex
    {
        get => SelectedSignal?.ByteOrder == ByteOrderEnum.Intel ? 1 : 0;
        set { if (SelectedSignal != null) SelectedSignal.ByteOrder = value == 1 ? ByteOrderEnum.Intel : ByteOrderEnum.Motorola; }
    }

    public int ValueTypeIndex
    {
        get => (int)(SelectedSignal?.ValueType ?? ValueTypeEnum.Unsigned);
        set { if (SelectedSignal != null) SelectedSignal.ValueType = (ValueTypeEnum)value; }
    }

    private Window? _mainWindow;
    private FilePickerFileType? _sdbuFileType;

    public MainWindowViewModel()
    {
    }

    public void SetWindow(Window window)
    {
        _mainWindow = window;
        _sdbuFileType = new FilePickerFileType("SDBU Files")
        {
            Patterns = new[] { "*.sdbu" }
        };
    }

    [RelayCommand]
    private void NewProject()
    {
        Project = new SdbuProject();
        Messages.Clear();
        Signals.Clear();
        SelectedMessage = null;
        SelectedSignal = null;
        OnPropertyChanged(nameof(ProtocolConfigText));
    }

    [RelayCommand]
    private async Task OpenProject()
    {
        if (_mainWindow == null) return;

        var files = await _mainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "打开SDBU文件",
            AllowMultiple = false,
            FileTypeFilter = new[] { _sdbuFileType! }
        });

        if (files.Count > 0)
        {
            try
            {
                var filePath = files[0].Path.LocalPath;
                Project = SdbuParser.Parse(filePath);
                Project.IsDirty = false;

                Messages.Clear();
                foreach (var msg in Project.Messages)
                {
                    Messages.Add(msg);
                }

                SelectedMessage = Messages.FirstOrDefault();
                OnPropertyChanged(nameof(ProtocolConfigText));
            }
            catch (Exception ex)
            {
                await ShowMessage("错误", $"打开文件失败: {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private void SaveProject()
    {
        if (string.IsNullOrEmpty(Project.FilePath))
        {
            SaveProjectAs();
            return;
        }
        DoSave(Project.FilePath);
    }

    [RelayCommand]
    private async Task SaveProjectAs()
    {
        if (_mainWindow == null) return;

        var file = await _mainWindow.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "保存SDBU文件",
            SuggestedFileName = "protocol.sdbu",
            FileTypeChoices = new[] { _sdbuFileType! }
        });

        if (file != null)
        {
            DoSave(file.Path.LocalPath);
        }
    }

    private void DoSave(string filePath)
    {
        try
        {
            SdbuWriter.Write(filePath, Project);
            Project.FilePath = filePath;
            Project.IsDirty = false;
        }
        catch (Exception ex)
        {
            _ = ShowMessage("错误", $"保存文件失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private void AddMessage()
    {
        var message = new SdbuMessage
        {
            MessageId = (uint)(Messages.Count + 1),
            MessageDlc = 8,
            Name = $"Message_{Messages.Count + 1}"
        };
        Project.Messages.Add(message);
        Messages.Add(message);
        SelectedMessage = message;
        Project.IsDirty = true;
    }

    [RelayCommand(CanExecute = nameof(CanDeleteMessage))]
    private void DeleteMessage()
    {
        if (SelectedMessage == null) return;

        Project.Messages.Remove(SelectedMessage);
        Messages.Remove(SelectedMessage);
        SelectedMessage = Messages.FirstOrDefault();
        Project.IsDirty = true;
    }

    private bool CanDeleteMessage() => SelectedMessage != null;

    [RelayCommand(CanExecute = nameof(CanEditMessage))]
    private void EditMessage()
    {
    }

    private bool CanEditMessage() => SelectedMessage != null;

    [RelayCommand(CanExecute = nameof(CanSaveMessage))]
    private void SaveMessageChanges()
    {
        Project.IsDirty = true;
    }

    private bool CanSaveMessage() => SelectedMessage != null;

    [RelayCommand(CanExecute = nameof(CanAddSignal))]
    private void AddSignal()
    {
        if (SelectedMessage == null) return;

        var signal = new SdbuSignal
        {
            MessageId = SelectedMessage.MessageId,
            MessageDlc = SelectedMessage.MessageDlc,
            Name = $"Signal_{SelectedMessage.Signals.Count + 1}",
            StartBit = 0,
            Length = 8,
            Factor = 1.0,
            Offset = 0
        };
        SelectedMessage.Signals.Add(signal);
        UpdateSignalList();
        SelectedSignal = signal;
        Project.IsDirty = true;
    }

    private bool CanAddSignal() => SelectedMessage != null;

    [RelayCommand(CanExecute = nameof(CanDeleteSignal))]
    private void DeleteSignal()
    {
        if (SelectedSignal == null || SelectedMessage == null) return;

        SelectedMessage.Signals.Remove(SelectedSignal);
        UpdateSignalList();
        SelectedSignal = Signals.FirstOrDefault();
        Project.IsDirty = true;
    }

    private bool CanDeleteSignal() => SelectedSignal != null;

    [RelayCommand(CanExecute = nameof(CanEditSignal))]
    private void EditSignal()
    {
    }

    private bool CanEditSignal() => SelectedSignal != null;

    [RelayCommand(CanExecute = nameof(CanSaveSignal))]
    private void SaveSignalChanges()
    {
        Project.IsDirty = true;
    }

    private bool CanSaveSignal() => SelectedSignal != null;

    [RelayCommand]
    private async Task EditProtocol()
    {
        if (_mainWindow == null) return;
        
        var dialog = new ProtocolConfigDialog(Project);
        await dialog.ShowDialog(_mainWindow);
        OnPropertyChanged(nameof(ProtocolConfigText));
    }

    [RelayCommand]
    private async Task ShowAbout()
    {
        await ShowMessage("关于", "SDBU 串口协议工具 v1.0\n基于Avalonia UI");
    }

    private void UpdateSignalList()
    {
        Signals.Clear();
        if (SelectedMessage == null) return;

        foreach (var sig in SelectedMessage.Signals)
        {
            Signals.Add(sig);
        }
        SelectedSignal = Signals.FirstOrDefault();
    }

    private async Task ShowMessage(string title, string message)
    {
        if (_mainWindow == null) return;
        var dialog = new Window
        {
            Title = title,
            Width = 400,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(20),
                Children =
                {
                    new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                    new Button { Content = "确定", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center, Margin = new Avalonia.Thickness(0, 20, 0, 0) }
                }
            }
        };
        var btn = ((StackPanel)dialog.Content).Children.OfType<Button>().First();
        btn.Click += (s, e) => dialog.Close();
        await dialog.ShowDialog(_mainWindow);
    }
}
