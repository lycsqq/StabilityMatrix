﻿using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using StabilityMatrix.Avalonia.Controls;
using StabilityMatrix.Core.Helper;
using TextMateSharp.Grammars;

namespace StabilityMatrix.Avalonia.Views;

public partial class LaunchPageView : UserControlBase
{
    private const int LineOffset = 5;
    
    public LaunchPageView()
    {
        InitializeComponent();
        var editor = this.FindControl<TextEditor>("Console");
        var options = new RegistryOptions(ThemeName.HighContrastLight);
        
        var textMate = editor.InstallTextMate(options);
        var scope = options.GetScopeByLanguageId("log");
        
        if (scope is null) throw new InvalidOperationException("Scope is null");
        
        textMate.SetGrammar(scope);
        textMate.SetTheme(options.LoadTheme(ThemeName.DarkPlus));
        
        EventManager.Instance.ScrollToBottomRequested += OnScrollToBottomRequested;
    }

    private void OnScrollToBottomRequested(object? sender, EventArgs e)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            var editor = this.FindControl<TextEditor>("Console");
            if (editor?.Document == null) return;
            var line = Math.Max(editor.Document.LineCount - LineOffset, 1);
            editor.ScrollToLine(line);
        });
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
