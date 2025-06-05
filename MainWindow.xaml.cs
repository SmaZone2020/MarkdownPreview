using MarkdownPreview.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace MarkdownPreview
{
    public partial class MainWindow : Window
    {
        private readonly MarkdownParser _parser = new MarkdownParser();
        private CancellationTokenSource _debounceCts;

        public MainWindow()
        {
            InitializeComponent();
            Editor.TextChanged += Editor_TextChanged;
            Editor.Text = @"

# 这是一级标题 (h1)
## 这是二级标题 (h2)
### 这是三级标题 (h3)
#### 这是四级标题 (h4)
##### 这是五级标题 (h5)
###### 这是六级标题 (h6)

### 链接
[GitHub](https://github.com/)

### 代码块
```javascript
function greet(name) {
  return `Hello, ${name}!`;
}
```
**加粗文本**  或  __加粗文本__

_斜体文本 _ 或 *斜体文本*

~~删除线文本~~

&下划线文本&

> 引用文本
> 可以多行

||防窥文本||（鼠标悬停显示）


### 图片
![随机图片](https://picsum.photos/200/100)

## 视频
![video](https://sample-videos.com/video321/mp4/720/big_buck_bunny_720p_2mb.mp4) ""肥兔子""
";
        }

        private void Editor_TextChanged(object sender, TextChangedEventArgs e)
        {
            _debounceCts?.Cancel();
            _debounceCts = new CancellationTokenSource();
            var token = _debounceCts.Token;
            string text = Editor.Text;

            Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(250, token);
                    Dispatcher.Invoke(() => ParseAndRender(text));
                }
                catch (TaskCanceledException){}
            }, token);
        }

        private void ParseAndRender(string markdownText)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            Preview.Children.Clear();

            var elements = _parser.Parse(markdownText);
            JsonTextBox.Text = "";

            foreach (var element in elements)
            {
                JsonTextBox.Text += string.Join(Environment.NewLine, element) + Environment.NewLine;
                switch (element)
                {
                    case Models.Header header:
                        RenderHeader(header);
                        break;
                    case Models.Image image:
                        RenderImage(image);
                        break;
                    case Models.CodeBlock codeBlock:
                        RenderCodeBlock(codeBlock);
                        break;
                    case Models.Video video:
                        RenderVideo(video);
                        break;
                    case Models.Link link:
                        RenderLink(link);
                        break;
                    case Models.Paragraph paragraph:
                        RenderParagraph(paragraph);
                        break;
                    case Models.ItalicText italic:
                        RenderItalicText(italic);
                        break;
                    case Models.BoldText bold:
                        RenderBoldText(bold);
                        break;
                    case Models.StrikethroughText strikethrough:
                        RenderStrikethroughText(strikethrough);
                        break;
                    case Models.UnderlineText underline:
                        RenderUnderlineText(underline);
                        break;
                    case Models.Quote quote:
                        RenderQuote(quote);
                        break;
                    case Models.ListItem listItem:
                        RenderListItem(listItem);
                        break;
                    case Models.HorizontalRule _:
                        RenderHorizontalRule();
                        break;
                    case Models.SpoilerText spoiler:
                        RenderSpoilerText(spoiler);
                        break;
                }
            }

            stopwatch.Stop();
            Dispatcher.Invoke(() =>
            {
                this.Title = $"渲染耗时: {stopwatch.ElapsedMilliseconds} ms | 共渲染 {elements.Count} 个元素";
            });
        }

        private void RenderHeader(Models.Header header)
        {
            var textBlock = new Label
            {
                Content = header.Content,
                FontWeight = FontWeights.Bold
            };

            switch (header.Level)
            {
                case 1:
                    textBlock.FontSize = 32; // H1
                    break;
                case 2:
                    textBlock.FontSize = 24; // H2
                    break;
                case 3:
                    textBlock.FontSize = 19; // H3
                    break;
                case 4:
                    textBlock.FontSize = 16; // H4
                    break;
                case 5:
                    textBlock.FontSize = 13; // H5
                    break;
                case 6:
                    textBlock.FontSize = 11; // H6
                    break;
                default:
                    textBlock.FontSize = 14;
                    break;
            }

            Preview.Children.Add(textBlock);
        }

        private void RenderImage(Models.Image image)
        {
            try
            {
                var imageControl = new System.Windows.Controls.Image
                {
                    Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(image.Url)),
                    Stretch = Stretch.Uniform,
                    MaxWidth = 400,
                    MaxHeight = 300,
                    Margin = new Thickness(0, 5, 0, 5),
                    ToolTip = image.AltText
                };

                var border = new Border
                {
                    Child = imageControl,
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(1),
                    Margin = new Thickness(0, 5, 0, 5)
                };

                Preview.Children.Add(border);
            }
            catch (Exception)
            {
                Preview.Children.Add(new TextBlock
                {
                    Text = $"![{image.AltText}](加载图片失败)",
                    Foreground = Brushes.Red,
                    Margin = new Thickness(0, 5, 0, 5)
                });
            }
        }

        private void RenderCodeBlock(Models.CodeBlock codeBlock)
        {
            var textBlock = new TextBlock
            {
                Text = codeBlock.Code,
                FontFamily = new FontFamily("Consolas"),
                Background = Brushes.LightGray,
                Padding = new Thickness(5),
                Margin = new Thickness(0, 5, 0, 5)
            };

            var border = new Border
            {
                Child = textBlock,
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 5, 0, 5),
                CornerRadius = new CornerRadius(3)
            };

            if (!string.IsNullOrEmpty(codeBlock.Language))
            {
                var languageLabel = new TextBlock
                {
                    Text = codeBlock.Language,
                    FontStyle = FontStyles.Italic,
                    Foreground = Brushes.Gray,
                    Margin = new Thickness(5, 5, 0, 0)
                };
                Preview.Children.Add(languageLabel);
            }

            Preview.Children.Add(border);
        }

        private void RenderVideo(Models.Video video)
        {
            var mediaElement = new MediaElement
            {
                ToolTip = video.AltText ?? video.Url,
                Margin = new Thickness(0, 5, 0, 5),
                Source = new Uri(video.Url),
                LoadedBehavior = MediaState.Manual,
                UnloadedBehavior = MediaState.Manual,
                Width = 400,
                Height = 225,
                Stretch = Stretch.Uniform
            };

            mediaElement.Loaded += (s, e) =>
            {
                mediaElement.Volume = 0.5;
                mediaElement.Play();
            };

            var playPauseButton = new Button
            {
                Content = "暂停",
                Margin = new Thickness(5, 0, 0, 0),
                MinWidth = 60
            };
            var replayButton = new Button
            {
                Content = "重播",
                Margin = new Thickness(5, 0, 0, 0),
                MinWidth = 60
            };

            playPauseButton.Click += (s, e) =>
            {
                if (mediaElement.CanPause && mediaElement.IsLoaded)
                {
                    if (mediaElement.Position < mediaElement.NaturalDuration)
                    {
                        if (mediaElement.IsPlaying())
                        {
                            mediaElement.Pause();
                            playPauseButton.Content = "播放";
                        }
                        else
                        {
                            mediaElement.Play();
                            playPauseButton.Content = "暂停";
                        }
                    }
                }
            };

            replayButton.Click += (s, e) =>
            {
                mediaElement.Stop();
                mediaElement.Position = TimeSpan.Zero;
                mediaElement.Play();
                playPauseButton.Content = "暂停";
            };

            mediaElement.MediaEnded += (s, e) =>
            {
                playPauseButton.Content = "播放";
            };
            mediaElement.MediaOpened += (s, e) =>
            {
                playPauseButton.Content = "暂停";
            };

            var panel = new StackPanel
            {
                Orientation = Orientation.Vertical
            };
            panel.Children.Add(mediaElement);
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 2, 0, 0)
            };
            buttonPanel.Children.Add(playPauseButton);
            buttonPanel.Children.Add(replayButton);
            panel.Children.Add(buttonPanel);

            var border = new Border
            {
                Child = panel,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1),
                Margin = new Thickness(0, 5, 0, 5),
                CornerRadius = new CornerRadius(3)
            };

            Preview.Children.Add(border);
        }

        private void RenderLink(Models.Link link)
        {
            var hyperlink = new Hyperlink
            {
                NavigateUri = new Uri(link.Url),
                ToolTip = link.Url
            };
            hyperlink.Inlines.Add(link.Text);
            hyperlink.RequestNavigate += (s, e) =>
            {
                try
                {
                    System.Diagnostics.Process.Start($"explorer", e.Uri.ToString());
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            var textBlock = new TextBlock
            {
                Margin = new Thickness(0, 5, 0, 5),
                TextWrapping = TextWrapping.Wrap
            };
            textBlock.Inlines.Add(hyperlink);

            Preview.Children.Add(textBlock);
        }

        private void RenderParagraph(Models.Paragraph paragraph)
        {
            var textBlock = new TextBlock
            {
                Margin = new Thickness(0, 5, 0, 5),
                TextWrapping = TextWrapping.Wrap
            };

            foreach (var inline in paragraph.InlineElements)
            {
                var run = new Run(inline.Text);

                switch (inline.Type)
                {
                    case InlineElementType.Bold:
                        run.FontWeight = FontWeights.Bold;
                        break;
                    case InlineElementType.Italic:
                        run.FontStyle = FontStyles.Italic;
                        break;
                    case InlineElementType.Strikethrough:
                        run.TextDecorations.Add(TextDecorations.Strikethrough);
                        break;
                    case InlineElementType.Underline:
                        run.TextDecorations.Add(TextDecorations.Underline);
                        break;
                    case InlineElementType.Spoiler:
                        var spoilerBorder = new Border
                        {
                            Background = Brushes.Black,
                            Child = new TextBlock
                            {
                                Text = run.Text,
                                Foreground = Brushes.Black,
                                ToolTip = "鼠标悬停显示内容"
                            }
                        };
                        spoilerBorder.MouseEnter += (s, e) => spoilerBorder.Background = Brushes.Transparent;
                        spoilerBorder.MouseLeave += (s, e) => spoilerBorder.Background = Brushes.Black;
                        textBlock.Inlines.Add(new InlineUIContainer(spoilerBorder));
                        continue;
                }

                textBlock.Inlines.Add(run);
            }

            Preview.Children.Add(textBlock);
        }

        private void RenderBoldText(Models.BoldText bold)
        {
            var textBlock = new TextBlock
            {
                Text = bold.Text,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 5, 0, 5)
            };
            Preview.Children.Add(textBlock);
        }

        private void RenderItalicText(Models.ItalicText italic)
        {
            var textBlock = new TextBlock
            {
                Text = italic.Text,
                FontStyle = FontStyles.Italic,
                Margin = new Thickness(0, 5, 0, 5)
            };
            Preview.Children.Add(textBlock);
        }

        private void RenderStrikethroughText(Models.StrikethroughText strikethrough)
        {
            var textBlock = new TextBlock
            {
                Text = strikethrough.Text,
                Margin = new Thickness(0, 5, 0, 5)
            };
            textBlock.TextDecorations.Add(TextDecorations.Strikethrough);
            Preview.Children.Add(textBlock);
        }

        private void RenderUnderlineText(Models.UnderlineText underline)
        {
            var textBlock = new TextBlock
            {
                Text = underline.Text,
                Margin = new Thickness(0, 5, 0, 5)
            };
            textBlock.TextDecorations.Add(TextDecorations.Underline);
            Preview.Children.Add(textBlock);
        }

        private void RenderQuote(Models.Quote quote)
        {
            var border = new Border
            {
                BorderThickness = new Thickness(1, 0, 0, 0),
                BorderBrush = Brushes.Gray,
                Margin = new Thickness(10, 5, 0, 5),
                Padding = new Thickness(10, 5, 0, 5)
            };

            var textBlock = new TextBlock
            {
                Text = quote.Text,
                FontStyle = FontStyles.Italic,
                Foreground = Brushes.Gray,
                TextWrapping = TextWrapping.Wrap
            };

            border.Child = textBlock;
            Preview.Children.Add(border);
        }

        private void RenderListItem(Models.ListItem listItem)
        {
            var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };

            if (listItem.IsOrdered && listItem.Number.HasValue)
            {
                stackPanel.Children.Add(new TextBlock
                {
                    Text = $"{listItem.Number}.",
                    Margin = new Thickness(0, 0, 5, 0)
                });
            }
            else
            {
                stackPanel.Children.Add(new TextBlock
                {
                    Text = "•",
                    Margin = new Thickness(0, 0, 5, 0)
                });
            }

            stackPanel.Children.Add(new TextBlock
            {
                Text = listItem.Text,
                TextWrapping = TextWrapping.Wrap
            });

            stackPanel.Margin = new Thickness(15, 2, 0, 2);
            Preview.Children.Add(stackPanel);
        }

        private void RenderHorizontalRule()
        {
            var line = new Border
            {
                Height = 1,
                Background = Brushes.LightGray,
                Margin = new Thickness(0, 10, 0, 10)
            };
            Preview.Children.Add(line);
        }

        private void RenderSpoilerText(Models.SpoilerText spoiler)
        {
            var hiddenText = new TextBlock
            {
                Text = "■■■ 点击显示隐藏内容 ■■■",
                Background = Brushes.Black,
                Foreground = Brushes.Black,
                Padding = new Thickness(5),
                Margin = new Thickness(0, 5, 0, 5)
            };

            var actualText = new TextBlock
            {
                Text = spoiler.Text,
                Visibility = Visibility.Collapsed,
                Margin = new Thickness(0, 5, 0, 5)
            };

            var stackPanel = new StackPanel();
            stackPanel.Children.Add(hiddenText);
            stackPanel.Children.Add(actualText);

            hiddenText.MouseEnter += (s, e) =>
            {
                hiddenText.Visibility = Visibility.Collapsed;
                actualText.Visibility = Visibility.Visible;
            };

            actualText.MouseLeave += (s, e) =>
            {
                actualText.Visibility = Visibility.Collapsed;
                hiddenText.Visibility = Visibility.Visible;
            };

            Preview.Children.Add(stackPanel);
        }
    }
}