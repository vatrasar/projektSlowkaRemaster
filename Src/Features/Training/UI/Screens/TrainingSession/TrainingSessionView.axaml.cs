using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.ReactiveUI;
using Microsoft.Extensions.Options;
using ReactiveUI;
using Splat;
using ProjektSlowkaRemasterd.Src.Core.Config;
using ProjektSlowkaRemasterd.Src.Core.Domain.Models;

namespace ProjektSlowkaRemasterd.Src.Features.Training.UI.Screens.TrainingSession;

/// <summary>
/// Code-behind for the TrainingSessionView.
/// Handles dynamic formatted text parsing (*sc ... *ec) and local media file rendering.
/// </summary>
public partial class TrainingSessionView : ReactiveUserControl<TrainingSessionViewModel>
{
    public TrainingSessionView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            // Bind header texts
            this.OneWayBind(ViewModel, vm => vm.State.Title, v => v.CategoryNameText.Text);
            this.OneWayBind(ViewModel, vm => vm.State.Subtitle, v => v.TopicNameText.Text);

            // Subtitle visibility
            this.WhenAnyValue(x => x.ViewModel!.State.Subtitle)
                .Select(subtitle => !string.IsNullOrEmpty(subtitle))
                .Subscribe(Observer.Create<bool>(visible => TopicNameText.IsVisible = visible))
                .DisposeWith(disposables);

            // Bind loading indicator
            this.OneWayBind(ViewModel, vm => vm.State.IsLoading, v => v.CardLoadingIndicator.IsVisible);

            // State changes subscription to update layout, format texts, and render images
            this.WhenAnyValue(x => x.ViewModel!.State)
                .Subscribe(Observer.Create<TrainingSessionState>(UpdateUI))
                .DisposeWith(disposables);
        });
    }

    private void UpdateUI(TrainingSessionState state)
    {
        if (state == null) return;

        // Session completion visibility toggle
        if (state.IsFinished)
        {
            ActiveCardBorder.IsVisible = false;
            FinishedBorder.IsVisible = true;
            ActionsGrid.IsVisible = false;
            ProgressText.Text = "Completed";
            SessionProgressBar.Value = 100;
            return;
        }

        ActiveCardBorder.IsVisible = true;
        FinishedBorder.IsVisible = false;
        ActionsGrid.IsVisible = true;

        // Progress indicators
        if (state.TotalQuestionsCount > 0)
        {
            ProgressText.Text = $"Card {state.CurrentQuestionIndex} of {state.TotalQuestionsCount}";
            SessionProgressBar.Value = (double)(state.CurrentQuestionIndex - 1) / state.TotalQuestionsCount * 100;
        }
        else
        {
            ProgressText.Text = "Card 0 of 0";
            SessionProgressBar.Value = 0;
        }

        // Bidirectional badge details
        if (state.IsBidirectional)
        {
            DirectionBadge.IsVisible = true;
            DirectionBadgeText.Text = state.CurrentDirection;
        }
        else
        {
            DirectionBadge.IsVisible = false;
        }

        var q = state.CurrentQuestion;
        if (q != null)
        {
            // Note mode vs normal mode Q/A visibility
            if (q.IsNotion)
            {
                QuestionContainer.IsVisible = false;
                CardDivider.IsVisible = false;
                AnswerContainer.IsVisible = true;

                RenderFormattedText(AnswerTextPanel, state.AnswerText);
                RenderImages(AnswerImagesPanel, state.AnswerMedia);
            }
            else
            {
                QuestionContainer.IsVisible = true;
                RenderFormattedText(QuestionTextPanel, state.QuestionText);
                RenderImages(QuestionImagesPanel, state.QuestionMedia);

                if (state.IsAnswerVisible)
                {
                    CardDivider.IsVisible = true;
                    AnswerContainer.IsVisible = true;
                    RenderFormattedText(AnswerTextPanel, state.AnswerText);
                    RenderImages(AnswerImagesPanel, state.AnswerMedia);
                }
                else
                {
                    CardDivider.IsVisible = false;
                    AnswerContainer.IsVisible = false;
                }
            }

            // Evaluation actions panel toggle
            if (state.IsAnswerVisible || q.IsNotion)
            {
                ShowAnswerButton.IsVisible = false;
                EvaluationButtonsGrid.IsVisible = true;
            }
            else
            {
                ShowAnswerButton.IsVisible = true;
                EvaluationButtonsGrid.IsVisible = false;
            }
        }
    }

    private void RenderFormattedText(StackPanel container, string text)
    {
        container.Children.Clear();
        if (string.IsNullOrEmpty(text)) return;

        int index = 0;
        while (index < text.Length)
        {
            int scIndex = text.IndexOf("*sc", index);
            if (scIndex < 0)
            {
                var remaining = text.Substring(index);
                container.Children.Add(new TextBlock
                {
                    Text = remaining,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 8),
                    FontSize = 16
                });
                break;
            }

            if (scIndex > index)
            {
                var normalPart = text.Substring(index, scIndex - index);
                container.Children.Add(new TextBlock
                {
                    Text = normalPart,
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, 0, 0, 8),
                    FontSize = 16
                });
            }

            int ecIndex = text.IndexOf("*ec", scIndex + 3);
            if (ecIndex < 0)
            {
                var codePart = text.Substring(scIndex + 3);
                AddCodeBlock(container, codePart);
                break;
            }
            else
            {
                var codePart = text.Substring(scIndex + 3, ecIndex - (scIndex + 3));
                AddCodeBlock(container, codePart);
                index = ecIndex + 3;
            }
        }
    }

    private void AddCodeBlock(StackPanel container, string code)
    {
        var border = new Border
        {
            Background = Brush.Parse("#ff121212"),
            BorderBrush = Brush.Parse("#ff2d2d2d"),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(12),
            Margin = new Thickness(0, 4, 0, 12),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var textBlock = new TextBlock
        {
            Text = code.Trim('\r', '\n'),
            FontFamily = "Courier New,Consolas,Monospace",
            FontSize = 14,
            Foreground = Brush.Parse("#ff00d0ff"),
            TextWrapping = TextWrapping.NoWrap
        };

        var scroll = new ScrollViewer
        {
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            Content = textBlock
        };

        border.Child = scroll;
        container.Children.Add(border);
    }

    private void RenderImages(StackPanel container, IEnumerable<Media> mediaList)
    {
        container.Children.Clear();
        if (mediaList == null) return;

        var config = Locator.Current.GetService<IOptions<AppConfig>>()?.Value;
        var mediaDir = config?.ResolvedMediaDirectoryPath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "media");

        foreach (var m in mediaList)
        {
            var filePath = Path.Combine(mediaDir, m.Filename);
            if (File.Exists(filePath))
            {
                try
                {
                    var bitmap = new Bitmap(filePath);
                    var img = new Image
                    {
                        Source = bitmap,
                        MaxWidth = 500,
                        MaxHeight = 350,
                        Margin = new Thickness(0, 8, 0, 8),
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    container.Children.Add(img);
                }
                catch
                {
                    // Fail silently on corrupt images
                }
            }
        }
    }
}
